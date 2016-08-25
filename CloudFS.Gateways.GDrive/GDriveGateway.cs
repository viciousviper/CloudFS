/*
The MIT License(MIT)

Copyright(c) 2015 IgorSoft

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using IgorSoft.CloudFS.Interface;
using IgorSoft.CloudFS.Interface.Composition;
using IgorSoft.CloudFS.Interface.IO;

using GoogleFile = Google.Apis.Drive.v3.Data.File;

namespace IgorSoft.CloudFS.Gateways.GDrive
{
    [ExportAsAsyncCloudGateway("GDrive")]
    [ExportMetadata(nameof(CloudGatewayMetadata.CloudService), GDriveGateway.SCHEMA)]
    [ExportMetadata(nameof(CloudGatewayMetadata.Capabilities), GDriveGateway.CAPABILITIES)]
    [ExportMetadata(nameof(CloudGatewayMetadata.ServiceUri), GDriveGateway.URL)]
    [ExportMetadata(nameof(CloudGatewayMetadata.ApiAssembly), GDriveGateway.API)]
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public sealed class GDriveGateway : IAsyncCloudGateway, IPersistGatewaySettings
    {
        private const string SCHEMA = "gdrive";

        private const GatewayCapabilities CAPABILITIES = GatewayCapabilities.All ^ GatewayCapabilities.CopyDirectoryItem;

        private const string URL = "https://drive.google.com";

        private const string API = "Google.Apis.Drive.v3";

        private const string MIME_TYPE_DIRECTORY = "application/vnd.google-apps.folder";

        private const string MIME_TYPE_FILE = "application/octet-stream";

        private const int RETRIES = 3;

        private class GDriveContext
        {
            public DriveService Service { get; }

            public GDriveContext(DriveService service)
            {
                Service = service;
            }
        }

        private readonly IDictionary<RootName, GDriveContext> contextCache = new Dictionary<RootName, GDriveContext>();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "apiKey")]
        private async Task<GDriveContext> RequireContextAsync(RootName root, string apiKey = null)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            var result = default(GDriveContext);
            if (!contextCache.TryGetValue(root, out result)) {
                var clientSecret = new ClientSecrets() { ClientId = Secrets.CLIENT_ID, ClientSecret = Secrets.CLIENT_SECRET };
                var credentials = await GoogleWebAuthorizationBroker.AuthorizeAsync(clientSecret, new[] { DriveService.Scope.Drive }, root.UserName, System.Threading.CancellationToken.None);
                var service = new DriveService(new BaseClientService.Initializer() { HttpClientInitializer = credentials, ApplicationName = "CloudFS" });
                contextCache.Add(root, result = new GDriveContext(service));
            }
            return result;
        }

        public async Task<bool> TryAuthenticateAsync(RootName root, string apiKey, IDictionary<string, string> parameters)
        {
            try {
                await RequireContextAsync(root, apiKey);
                return true;
            } catch (Exception) {
                return false;
            }
        }

        public async Task<DriveInfoContract> GetDriveAsync(RootName root, string apiKey, IDictionary<string, string> parameters)
        {
            var context = await RequireContextAsync(root, apiKey);

            var request = context.Service.About.Get().AsDrive();
            var item = await AsyncFunc.RetryAsync<About, GoogleApiException>(async () => await request.ExecuteAsync(), RETRIES);

            var storageQuota = item.StorageQuota;
            var usedSpace = storageQuota.UsageInDrive.HasValue || storageQuota.UsageInDriveTrash.HasValue
                ? (storageQuota.UsageInDrive ?? 0) + (storageQuota.UsageInDriveTrash ?? 0)
                : (long?)null;
            var freeSpace = storageQuota.Limit.HasValue ? storageQuota.Limit.Value - usedSpace : (long?)null;
            return new DriveInfoContract(item.User.DisplayName, freeSpace, usedSpace);
        }

        public async Task<RootDirectoryInfoContract> GetRootAsync(RootName root, string apiKey, IDictionary<string, string> parameters)
        {
            var context = await RequireContextAsync(root, apiKey);

            var request = context.Service.Files.Get("root").AsRootDirectory();
            var item = await AsyncFunc.RetryAsync<GoogleFile, GoogleApiException>(async () => await request.ExecuteAsync(), RETRIES);

            return new RootDirectoryInfoContract(item.Id, new DateTimeOffset(item.CreatedTime.Value), new DateTimeOffset(item.ModifiedTime.Value));
        }

        public async Task<IEnumerable<FileSystemInfoContract>> GetChildItemAsync(RootName root, DirectoryId parent)
        {
            var context = await RequireContextAsync(root);

            var request = context.Service.Files.List().WithFiles(parent);
            var childReferences = await AsyncFunc.RetryAsync<FileList, GoogleApiException>(async () => await request.ExecuteAsync(), RETRIES);
            var items = childReferences.Files.Select(async c => await AsyncFunc.RetryAsync<GoogleFile, GoogleApiException>(async () =>
            {
                var childRequest = context.Service.Files.Get(c.Id).AsFileSystem();
                return await childRequest.ExecuteAsync();
            }, RETRIES)).ToArray();

            Task.WaitAll(items);

            return items.Select(i => i.Result.ToFileSystemInfoContract());
        }

        public async Task<bool> ClearContentAsync(RootName root, FileId target, Func<FileSystemInfoLocator> locatorResolver)
        {
            var context = await RequireContextAsync(root);

            await AsyncFunc.RetryAsync<IUploadProgress, GoogleApiException>(async () => await context.Service.Files.Update(null, target.Value, Stream.Null, MIME_TYPE_FILE).UploadAsync(), RETRIES);

            return true;
        }

        public async Task<Stream> GetContentAsync(RootName root, FileId source)
        {
            var context = await RequireContextAsync(root);

            var stream = new MemoryStream();
            await AsyncFunc.RetryAsync<GoogleApiException>(async () => await context.Service.Files.Get(source.Value).DownloadAsync(stream), RETRIES);

            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        public async Task<bool> SetContentAsync(RootName root, FileId target, Stream content, IProgress<ProgressValue> progress, Func<FileSystemInfoLocator> locatorResolver)
        {
            var context = await RequireContextAsync(root);

            var update = context.Service.Files.Update(new GoogleFile(), target.Value, content, MIME_TYPE_FILE);
            if (progress != null)
                update.ProgressChanged += p => progress.Report(new ProgressValue((int)p.BytesSent, (int)content.Length));
            await AsyncFunc.RetryAsync<IUploadProgress, GoogleApiException>(async () => await update.UploadAsync(), RETRIES);

            return true;
        }

        public async Task<FileSystemInfoContract> CopyItemAsync(RootName root, FileSystemId source, string copyName, DirectoryId destination, bool recurse)
        {
            if (source is DirectoryId)
                throw new NotSupportedException(Properties.Resources.CopyingOfDirectoriesNotSupported);

            var context = await RequireContextAsync(root);

            var copy = new GoogleFile() { Name = copyName };
            if (destination != null)
                copy.Parents = new[] { destination.Value };
            var request = context.Service.Files.Copy(copy, source.Value).AsFileSystem();
            var item = await AsyncFunc.RetryAsync<GoogleFile, GoogleApiException>(async () => await request.ExecuteAsync(), RETRIES);

            return item.ToFileSystemInfoContract();
        }

        public async Task<FileSystemInfoContract> MoveItemAsync(RootName root, FileSystemId source, string moveName, DirectoryId destination, Func<FileSystemInfoLocator> locatorResolver)
        {
            var context = await RequireContextAsync(root);

            var parent = new DirectoryId(context.Service.Files.Get(source.Value).WithParents().ExecuteAsync().Result.Parents.Single());

            var move = new GoogleFile() { Name = moveName };
            var request = context.Service.Files.Update(move, source.Value).AsFileSystem(parent, destination);
            var item = await AsyncFunc.RetryAsync<GoogleFile, GoogleApiException>(async () => await request.ExecuteAsync(), RETRIES);

            return item.ToFileSystemInfoContract();
        }

        public async Task<DirectoryInfoContract> NewDirectoryItemAsync(RootName root, DirectoryId parent, string name)
        {
            var context = await RequireContextAsync(root);

            var file = new GoogleFile() { Name = name, MimeType = MIME_TYPE_DIRECTORY, Parents = new[] { parent.Value } };
            var item = await AsyncFunc.RetryAsync<GoogleFile, GoogleApiException>(async () => await context.Service.Files.Create(file).AsDirectory().ExecuteAsync(), RETRIES);

            return new DirectoryInfoContract(item.Id, item.Name, new DateTimeOffset(item.CreatedTime.Value), new DateTimeOffset(item.ModifiedTime.Value));
        }

        public async Task<FileInfoContract> NewFileItemAsync(RootName root, DirectoryId parent, string name, Stream content, IProgress<ProgressValue> progress)
        {
            if (content.Length == 0)
                return new ProxyFileInfoContract(name);

            var context = await RequireContextAsync(root);

            var file = new GoogleFile() { Name = name, MimeType = MIME_TYPE_FILE, Parents = new[] { parent.Value } };
            var insert = context.Service.Files.Create(file, content, MIME_TYPE_FILE).AsFile();
            if (progress != null)
                insert.ProgressChanged += p => progress.Report(new ProgressValue((int)p.BytesSent, (int)content.Length));
            var upload = await AsyncFunc.RetryAsync<IUploadProgress, GoogleApiException>(async () => await insert.UploadAsync(), RETRIES);
            var item = insert.ResponseBody;

            return new FileInfoContract(item.Id, item.Name, new DateTimeOffset(item.CreatedTime.Value), new DateTimeOffset(item.ModifiedTime.Value), item.Size.Value, item.Md5Checksum);
        }

        public async Task<bool> RemoveItemAsync(RootName root, FileSystemId target, bool recurse)
        {
            var context = await RequireContextAsync(root);

            var request = context.Service.Files.Delete(target.Value);
            var item = await AsyncFunc.RetryAsync<string, GoogleApiException>(async () => await request.ExecuteAsync(), RETRIES);

            return true;
        }

        public async Task<FileSystemInfoContract> RenameItemAsync(RootName root, FileSystemId target, string newName, Func<FileSystemInfoLocator> locatorResolver)
        {
            var context = await RequireContextAsync(root);

            var request = context.Service.Files.Update(new GoogleFile() { Name = newName }, target.Value).AsFileSystem();
            var item = await AsyncFunc.RetryAsync<GoogleFile, GoogleApiException>(async () => await request.ExecuteAsync(), RETRIES);

            return item.ToFileSystemInfoContract();
        }

        public void PurgeSettings(RootName root)
        {
            var dataStore = new FileDataStore(GoogleWebAuthorizationBroker.Folder, false);
            if (root != null)
                dataStore.DeleteAsync<TokenResponse>(root.UserName).Wait();
            else
                dataStore.ClearAsync().Wait();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private static string DebuggerDisplay() => nameof(GDriveGateway);
    }
}
