/*
The MIT License(MIT)

Copyright(c) 2016 IgorSoft

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
using System.Security.Authentication;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Upload;
using Google.Cloud.Storage.V1;
using IgorSoft.CloudFS.Interface;
using IgorSoft.CloudFS.Interface.Composition;
using IgorSoft.CloudFS.Interface.IO;

namespace IgorSoft.CloudFS.Gateways.GoogleCloudStorage
{
    [ExportAsAsyncCloudGateway("GDrive")]
    [ExportMetadata(nameof(CloudGatewayMetadata.CloudService), GoogleCloudStorageGateway.SCHEMA)]
    [ExportMetadata(nameof(CloudGatewayMetadata.Capabilities), GoogleCloudStorageGateway.CAPABILITIES)]
    [ExportMetadata(nameof(CloudGatewayMetadata.ServiceUri), GoogleCloudStorageGateway.URL)]
    [ExportMetadata(nameof(CloudGatewayMetadata.ApiAssembly), GoogleCloudStorageGateway.API)]
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay(),nq}")]
    internal class GoogleCloudStorageGateway : IAsyncCloudGateway, IPersistGatewaySettings
    {
        private const string SCHEMA = "gcs";

        private const GatewayCapabilities CAPABILITIES = GatewayCapabilities.All ^ GatewayCapabilities.CopyDirectoryItem ^ GatewayCapabilities.RenameDirectoryItem ^ GatewayCapabilities.RenameFileItem ^ GatewayCapabilities.ItemId;

        private const string URL = "https://drive.google.com";

        private const string API = "Google.Apis.Storage.v1";

        private const string PARAMETER_BUCKET = "bucket";

        private const string MIME_TYPE_FILE = "application/octet-stream";

        private static readonly long FREE_SPACE = new FileSize("5GB");

        private class GoogleCloudStorageContext
        {
            public StorageClient Client { get; }

            public string Bucket { get; }

            public GoogleCloudStorageContext(StorageClient client, string bucket)
            {
                Client = client;
                Bucket = bucket;
            }
        }

        private class UploadProgress : IProgress<IUploadProgress>
        {
            private IProgress<ProgressValue> progress;

            private int bytesTotal;

            public UploadProgress(IProgress<ProgressValue> progress, int bytesTotal)
            {
                this.progress = progress;
                this.bytesTotal = bytesTotal;
            }

            public void Report(IUploadProgress value)
            {
                progress.Report(new ProgressValue((int)value.BytesSent, bytesTotal));
            }
        }

        private readonly IDictionary<RootName, GoogleCloudStorageContext> contextCache = new Dictionary<RootName, GoogleCloudStorageContext>();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "apiKey")]
        private async Task<GoogleCloudStorageContext> RequireContextAsync(RootName root, string apiKey = null, string bucket = null)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            var result = default(GoogleCloudStorageContext);
            if (!contextCache.TryGetValue(root, out result)) {
                if (string.IsNullOrEmpty(apiKey))
                    throw new InvalidOperationException("Credentials missing");

                var credentials = GoogleCredential.FromStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(apiKey)));
                var client = await StorageClient.CreateAsync(credentials);
                contextCache.Add(root, result = new GoogleCloudStorageContext(client, bucket));
            }
            return result;
        }

        public async Task<bool> TryAuthenticateAsync(RootName root, string apiKey, IDictionary<string, string> parameters)
        {
            try {
                await RequireContextAsync(root, apiKey, parameters[PARAMETER_BUCKET]);
                return true;
            } catch (AuthenticationException) {
                return false;
            }
        }

        public async Task<DriveInfoContract> GetDriveAsync(RootName root, string apiKey, IDictionary<string, string> parameters)
        {
            var context = await RequireContextAsync(root, apiKey, parameters[PARAMETER_BUCKET]);

            var item = await context.Client.GetBucketAsync(context.Bucket);
            var usedSpace = await context.Client.ListObjectsAsync(context.Bucket, string.Empty).Aggregate(0UL, (u, o) => u + (o.Size ?? 0));

            // HACK: Assume freeSpace to be the maximum supported file size because Google Cloud Storage does not expose free space info.
            return new DriveInfoContract(item.Id, FREE_SPACE, (long)usedSpace);
        }

        public async Task<RootDirectoryInfoContract> GetRootAsync(RootName root, string apiKey, IDictionary<string, string> parameters)
        {
            var context = await RequireContextAsync(root, apiKey, parameters[PARAMETER_BUCKET]);

            var item = await context.Client.GetBucketAsync(context.Bucket);

            return new RootDirectoryInfoContract($"{item.Id}//{item.Metageneration.Value}", new DateTimeOffset(item.TimeCreated.Value), new DateTimeOffset(item.Updated.Value));
        }

        public async Task<IEnumerable<FileSystemInfoContract>> GetChildItemAsync(RootName root, DirectoryId parent)
        {
            var context = await RequireContextAsync(root);

            var parentObjectId = new StorageObjectId(parent.Value);
            var items = await context.Client.ListObjectsAsync(parentObjectId.Bucket, parentObjectId.Path).Where(i =>
            {
                var childName = i.Name.Substring(parentObjectId.Path.Length).TrimEnd(Path.AltDirectorySeparatorChar);
                return !string.IsNullOrEmpty(childName) && !childName.Contains(Path.AltDirectorySeparatorChar);
            }).ToList();

            return items.Select(i => i.ToFileSystemInfoContract());
        }

        public async Task<bool> ClearContentAsync(RootName root, FileId target, Func<FileSystemInfoLocator> locatorResolver)
        {
            var context = await RequireContextAsync(root);

            var targetObjectId = new StorageObjectId(target.Value);
            await context.Client.UploadObjectAsync(targetObjectId.Bucket, targetObjectId.Path, MIME_TYPE_FILE, new MemoryStream());

            return true;
        }

        public async Task<Stream> GetContentAsync(RootName root, FileId source)
        {
            var context = await RequireContextAsync(root);

            var sourceObjectId = new StorageObjectId(source.Value);
            var stream = new MemoryStream();
            await context.Client.DownloadObjectAsync(sourceObjectId.Bucket, sourceObjectId.Path, stream);

            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        public async Task<bool> SetContentAsync(RootName root, FileId target, Stream content, IProgress<ProgressValue> progress, Func<FileSystemInfoLocator> locatorResolver)
        {
            var context = await RequireContextAsync(root);

            var targetObjectId = new StorageObjectId(target.Value);

            var uploadProgress = progress != null ? new UploadProgress(progress, (int)content.Length) : null;

            await context.Client.UploadObjectAsync(targetObjectId.Bucket, targetObjectId.Path, MIME_TYPE_FILE, content, progress: uploadProgress);

            return true;
        }

        public async Task<FileSystemInfoContract> CopyItemAsync(RootName root, FileSystemId source, string copyName, DirectoryId destination, bool recurse)
        {
            if (source is DirectoryId)
                throw new NotSupportedException(Properties.Resources.CopyingOfDirectoriesNotSupported);

            var context = await RequireContextAsync(root);

            var sourceObjectId = new StorageObjectId(source.Value);
            var destinationObjectId = new StorageObjectId(destination.Value);
            var item = await context.Client.CopyObjectAsync(sourceObjectId.Bucket, sourceObjectId.Path, destinationObjectId.Bucket, $"{destinationObjectId.Path}{copyName}");

            return item.ToFileSystemInfoContract();
        }

        public async Task<FileSystemInfoContract> MoveItemAsync(RootName root, FileSystemId source, string moveName, DirectoryId destination, Func<FileSystemInfoLocator> locatorResolver)
        {
            // Emulate MoveItem through CopyItem + RemoveItem

            var context = await RequireContextAsync(root);

            var sourceObjectId = new StorageObjectId(source.Value);
            var destinationObjectId = new StorageObjectId(destination.Value);
            var directorySource = source as DirectoryId;
            if (directorySource != null)
                moveName += Path.AltDirectorySeparatorChar;

            var item = await context.Client.CopyObjectAsync(sourceObjectId.Bucket, sourceObjectId.Path, destinationObjectId.Bucket, $"{destinationObjectId.Path}{moveName}");

            if (directorySource != null) {
                var subDestination = new DirectoryId(item.Id);
                foreach (var subItem in await GetChildItemAsync(root, directorySource))
                    await MoveItemAsync(root, subItem.Id, subItem.Name, subDestination, locatorResolver);
            }

            await context.Client.DeleteObjectAsync(sourceObjectId.Bucket, sourceObjectId.Path);

            return item.ToFileSystemInfoContract();
        }

        public async Task<DirectoryInfoContract> NewDirectoryItemAsync(RootName root, DirectoryId parent, string name)
        {
            var context = await RequireContextAsync(root);

            var parentObjectId = new StorageObjectId(parent.Value);
            var item = await context.Client.UploadObjectAsync(parentObjectId.Bucket, $"{parentObjectId.Path}{name}/", null, new MemoryStream());

            return new DirectoryInfoContract(item.Id, item.Name.Substring(parentObjectId.Path.Length), new DateTimeOffset(item.TimeCreated.Value), new DateTimeOffset(item.Updated.Value));
        }

        public async Task<FileInfoContract> NewFileItemAsync(RootName root, DirectoryId parent, string name, Stream content, IProgress<ProgressValue> progress)
        {
            if (content.Length == 0)
                return new ProxyFileInfoContract(name);

            var context = await RequireContextAsync(root);

            var parentObjectId = new StorageObjectId(parent.Value);

            var uploadProgress = progress != null ? new UploadProgress(progress, (int)content.Length) : null;

            var item = await context.Client.UploadObjectAsync(parentObjectId.Bucket, $"{parentObjectId.Path}{name}", MIME_TYPE_FILE, content, progress: uploadProgress);

            return new FileInfoContract(item.Id, item.Name.Substring(parentObjectId.Path.Length), new DateTimeOffset(item.TimeCreated.Value), new DateTimeOffset(item.Updated.Value), (FileSize)(long)item.Size.Value, item.Md5Hash);
        }

        public async Task<bool> RemoveItemAsync(RootName root, FileSystemId target, bool recurse)
        {
            var context = await RequireContextAsync(root);

            var targetObjectId = new StorageObjectId(target.Value);

            if (recurse)
                foreach (var childItem in await GetChildItemAsync(root, (DirectoryId)target))
                    await RemoveItemAsync(root, childItem.Id, childItem is DirectoryInfoContract);

            await context.Client.DeleteObjectAsync(targetObjectId.Bucket, targetObjectId.Path);

            return true;
        }

        public Task<FileSystemInfoContract> RenameItemAsync(RootName root, FileSystemId target, string newName, Func<FileSystemInfoLocator> locatorResolver)
        {
            throw new NotSupportedException();
        }

        public void PurgeSettings(RootName root)
        {
            throw new NotImplementedException();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private static string DebuggerDisplay() => nameof(GoogleCloudStorageGateway);
    }
}
