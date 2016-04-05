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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using pCloud.NET;
using IgorSoft.CloudFS.Interface;
using IgorSoft.CloudFS.Interface.Composition;
using IgorSoft.CloudFS.Interface.IO;
using IgorSoft.CloudFS.Gateways.pCloud.OAuth;

using pCloudFile = pCloud.NET.File;

namespace IgorSoft.CloudFS.Gateways.pCloud
{
    [ExportAsAsyncCloudGateway("pCloud")]
    [ExportMetadata(nameof(CloudGatewayMetadata.CloudService), pCloudGateway.SCHEMA)]
    [ExportMetadata(nameof(CloudGatewayMetadata.Capabilities), pCloudGateway.CAPABILITIES)]
    [ExportMetadata(nameof(CloudGatewayMetadata.ServiceUri), pCloudGateway.URL)]
    [ExportMetadata(nameof(CloudGatewayMetadata.ApiAssembly), pCloudGateway.API)]
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public class pCloudGateway : IAsyncCloudGateway
    {
        private const string SCHEMA = "pcloud";

        private const GatewayCapabilities CAPABILITIES = GatewayCapabilities.All ^ GatewayCapabilities.CopyDirectoryItem;

        private const string URL = "https://www.pcloud.com";

        private const string API = "pCloud.NET SDK";

        private const int RETRIES = 3;

        private class pCloudContext
        {
            public pCloudClient Client { get; }

            public pCloudContext(pCloudClient client)
            {
                Client = client;
            }
        }

        private IDictionary<RootName, pCloudContext> contextCache = new Dictionary<RootName, pCloudContext>();

        private async Task<pCloudContext> RequireContext(RootName root, string apiKey = null)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            var result = default(pCloudContext);
            if (!contextCache.TryGetValue(root, out result)) {
                var client = await Authenticator.Login(root.UserName, apiKey);
                contextCache.Add(root, result = new pCloudContext(client));
            }
            return result;
        }

        private static long ToId(DirectoryId folderId)
        {
            if (!folderId.Value.StartsWith("d", StringComparison.Ordinal))
                throw new FormatException(string.Format(CultureInfo.InvariantCulture, Resources.InvalidFolderId, folderId));

            return long.Parse(folderId.Value.Substring(1), NumberStyles.Number);
        }

        private static long ToId(FileId fileId)
        {
            if (!fileId.Value.StartsWith("f", StringComparison.Ordinal))
                throw new FormatException(string.Format(CultureInfo.InvariantCulture, Resources.InvalidFileId, fileId));

            return long.Parse(fileId.Value.Substring(1), NumberStyles.Number);
        }

        public async Task<DriveInfoContract> GetDriveAsync(RootName root, string apiKey, IDictionary<string, string> parameters)
        {
            var context = await RequireContext(root, apiKey);

            var item = await AsyncFunc.Retry<UserInfo, pCloudException>(async () => await context.Client.GetUserInfoAsync(), RETRIES);

            return new DriveInfoContract(item.UserId, item.Quota - item.UsedQuota, item.UsedQuota);
        }

        public async Task<RootDirectoryInfoContract> GetRootAsync(RootName root, string apiKey)
        {
            var context = await RequireContext(root, apiKey);

            var item = await AsyncFunc.Retry<ListedFolder, pCloudException>(async () => await context.Client.ListFolderAsync(0), RETRIES);

            return new RootDirectoryInfoContract(item.Id, item.Created, item.Modified);
        }

        public async Task<IEnumerable<FileSystemInfoContract>> GetChildItemAsync(RootName root, DirectoryId parent)
        {
            var context = await RequireContext(root);

            var item = await AsyncFunc.Retry<ListedFolder, pCloudException>(async () => await context.Client.ListFolderAsync(ToId(parent)), RETRIES);
            var items = item.Contents;

            return items.Select(i => i.ToFileSystemInfoContract());
        }

        public async Task<bool> ClearContentAsync(RootName root, FileId target, Func<FileSystemInfoLocator> locatorResolver)
        {
            if (locatorResolver == null)
                throw new ArgumentNullException(nameof(locatorResolver));

            var context = await RequireContext(root);

            var locator = locatorResolver();
            await context.Client.UploadFileAsync(Stream.Null, ToId(locator.ParentId), locator.Name, CancellationToken.None);

            return true;
        }

        public async Task<Stream> GetContentAsync(RootName root, FileId source)
        {
            var context = await RequireContext(root);

            var stream = new MemoryStream();
            //await AsyncFunc.Retry<Stream, pCloudException>(async () => await context.Client.DownloadFileAsync(ToId(source), stream, tokenSource.Token), RETRIES);
            await context.Client.DownloadFileAsync(ToId(source), stream, CancellationToken.None);
            stream.Seek(0, SeekOrigin.Begin);

            return stream;
        }

        public async Task<bool> SetContentAsync(RootName root, FileId target, Stream content, IProgress<ProgressValue> progress, Func<FileSystemInfoLocator> locatorResolver)
        {
            if (locatorResolver == null)
                throw new ArgumentNullException(nameof(locatorResolver));

            var context = await RequireContext(root);

            var locator = locatorResolver();
            var stream = progress != null ? new ProgressStream(content, progress) : content;
            await context.Client.UploadFileAsync(stream, ToId(locator.ParentId), locator.Name, CancellationToken.None);

            return true;
        }

        public async Task<FileSystemInfoContract> CopyItemAsync(RootName root, FileSystemId source, string copyName, DirectoryId destination, bool recurse)
        {
            var fileSource = source as FileId;
            if (fileSource == null)
                 throw new NotSupportedException(Resources.CopyingOfDirectoriesNotSupported);

            var context = await RequireContext(root);

            var item = await AsyncFunc.Retry<pCloudFile, pCloudException>(async () => await context.Client.CopyFileAsync(ToId(fileSource), ToId(destination), copyName), RETRIES);

            return new FileInfoContract(item.Id, item.Name, item.Created, item.Modified, item.Size, null);
        }

        public async Task<FileSystemInfoContract> MoveItemAsync(RootName root, FileSystemId source, string moveName, DirectoryId destination, Func<FileSystemInfoLocator> locatorResolver)
        {
            var context = await RequireContext(root);

            var directorySource = source as DirectoryId;
            if (directorySource != null) {
                var item = await context.Client.RenameFolderAsync(ToId(directorySource), ToId(destination), moveName);

                return new DirectoryInfoContract(item.Id, item.Name, item.Created, item.Modified);
            }

            var fileSource = source as FileId;
            if (fileSource != null) {
                var item = await context.Client.RenameFileAsync(ToId(fileSource), ToId(destination), moveName);

                return new FileInfoContract(item.Id, item.Name, item.Created, item.Modified, item.Size, null);
            }

            throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, Resources.ItemTypeNotSupported, source.GetType().Name));
        }

        public async Task<DirectoryInfoContract> NewDirectoryItemAsync(RootName root, DirectoryId parent, string name)
        {
            var context = await RequireContext(root);

            var item = await AsyncFunc.Retry<Folder, pCloudException>(async () => await context.Client.CreateFolderAsync(ToId(parent), name), RETRIES);

            return new DirectoryInfoContract(item.Id, item.Name, item.Created, item.Modified);
        }

        public async Task<FileInfoContract> NewFileItemAsync(RootName root, DirectoryId parent, string name, Stream content, IProgress<ProgressValue> progress)
        {
            var context = await RequireContext(root);

            var stream = progress != null ? new ProgressStream(content, progress) : content;
            var item = await AsyncFunc.Retry<pCloudFile, pCloudException>(async () => await context.Client.UploadFileAsync(stream, ToId(parent), name, CancellationToken.None), RETRIES);

            return new FileInfoContract(item.Id, item.Name, item.Created, item.Modified, item.Size, null);
        }

        public async Task<bool> RemoveItemAsync(RootName root, FileSystemId target, bool recurse)
        {
            var context = await RequireContext(root);

            var directoryTarget = target as DirectoryId;
            if (directoryTarget != null) {
                await context.Client.DeleteFolderAsync(ToId(directoryTarget), recurse);
                return true;
            }

            var fileTarget = target as FileId;
            if (fileTarget != null) {
                await context.Client.DeleteFileAsync(ToId(fileTarget));
                return true;
            }

            throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, Resources.ItemTypeNotSupported, target.GetType().Name));
        }

        public async Task<FileSystemInfoContract> RenameItemAsync(RootName root, FileSystemId target, string newName, Func<FileSystemInfoLocator> locatorResolver)
        {
            var context = await RequireContext(root);

            var directoryTarget = target as DirectoryId;
            if (directoryTarget != null) {
                var locator = locatorResolver();
                var item = await context.Client.RenameFolderAsync(ToId(directoryTarget), ToId(locator.ParentId), newName);

                return new DirectoryInfoContract(item.Id, item.Name, item.Created, item.Modified);
            }

            var fileTarget = target as FileId;
            if (fileTarget != null) {
                var locator = locatorResolver();
                var item = await context.Client.RenameFileAsync(ToId(fileTarget), ToId(locator.ParentId), newName);

                return new FileInfoContract(item.Id, item.Name, item.Created, item.Modified, item.Size, null);
            }

            throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, Resources.ItemTypeNotSupported, target.GetType().Name));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private static string DebuggerDisplay() => nameof(pCloudGateway);
    }
}
