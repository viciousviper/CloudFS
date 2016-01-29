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
using Box.V2;
using Box.V2.Exceptions;
using Box.V2.Models;
using IgorSoft.CloudFS.Gateways.Box.OAuth;
using IgorSoft.CloudFS.Interface;
using IgorSoft.CloudFS.Interface.Composition;
using IgorSoft.CloudFS.Interface.IO;

namespace IgorSoft.CloudFS.Gateways.Box
{
    [ExportAsAsyncCloudGateway("Box")]
    [ExportMetadata(nameof(CloudGatewayMetadata.CloudService), BoxGateway.SCHEMA)]
    [ExportMetadata(nameof(CloudGatewayMetadata.ServiceUri), BoxGateway.URL)]
    [ExportMetadata(nameof(CloudGatewayMetadata.ApiAssembly), BoxGateway.API)]
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public sealed class BoxGateway : IAsyncCloudGateway
    {
        private const string SCHEMA = "box";

        private const string URL = "https://app.box.com";

        private const string API = "Box.V2";

        private const int RETRIES = 3;

        private static readonly List<string> boxFileFields = new List<string>(new[] { BoxItem.FieldName, BoxItem.FieldCreatedAt, BoxItem.FieldModifiedAt, BoxItem.FieldSize, BoxFile.FieldSha1 });

        private static readonly List<string> boxFolderFields = new List<string>(new[] { BoxItem.FieldName, BoxItem.FieldCreatedAt, BoxItem.FieldModifiedAt });

        private class BoxContext
        {
            public BoxClient Client { get; }

            public BoxContext(BoxClient client)
            {
                Client = client;
            }
        }

        private IDictionary<RootName, BoxContext> contextCache = new Dictionary<RootName, BoxContext>();

        public bool PreservesId => true;

        private async Task<BoxContext> RequireContext(RootName root, string apiKey = null)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            var result = default(BoxContext);
            if (!contextCache.TryGetValue(root, out result)) {
                var client = await OAuthAuthenticator.Login(root.UserName, apiKey);
                contextCache.Add(root, result = new BoxContext(client));
            }
            return result;
        }

        public async Task<DriveInfoContract> GetDriveAsync(RootName root, string apiKey, IDictionary<string, string> parameters)
        {
            var context = await RequireContext(root, apiKey);

            var item = await AsyncFunc.Retry<BoxUser, BoxException>(async () => await context.Client.UsersManager.GetCurrentUserInformationAsync(), RETRIES);

            return new DriveInfoContract(item.Id, item.SpaceAmount.Value - item.SpaceUsed.Value, item.SpaceUsed.Value);
        }

        public async Task<RootDirectoryInfoContract> GetRootAsync(RootName root, string apiKey)
        {
            var context = await RequireContext(root, apiKey);

            var item = await AsyncFunc.Retry<BoxFolder, BoxException>(async () => await context.Client.FoldersManager.GetInformationAsync("0", boxFolderFields), RETRIES);

            return new RootDirectoryInfoContract(item.Id, DateTimeOffset.MinValue, DateTimeOffset.MinValue);
        }

        public async Task<IEnumerable<FileSystemInfoContract>> GetChildItemAsync(RootName root, DirectoryId parent)
        {
            var context = await RequireContext(root);

            var items = await AsyncFunc.Retry<BoxCollection<BoxItem>, BoxException>(async () => await context.Client.FoldersManager.GetFolderItemsAsync(parent.Value, 1000, fields:boxFileFields), RETRIES);

            return items.Entries.Select(i => i.ToFileSystemInfoContract());
        }

        public async Task<bool> ClearContentAsync(RootName root, FileId target, Func<FileSystemInfoLocator> locatorResolver)
        {
            var context = await RequireContext(root);

            var locator = locatorResolver();
            await AsyncFunc.Retry<BoxFile, BoxException>(async () => await context.Client.FilesManager.UploadNewVersionAsync(locator.Name, target.Value, new MemoryStream()), RETRIES);

            return true;
        }

        public async Task<Stream> GetContentAsync(RootName root, FileId source)
        {
            var context = await RequireContext(root);

            var stream = await AsyncFunc.Retry<Stream, BoxException>(async () => await context.Client.FilesManager.DownloadStreamAsync(source.Value), RETRIES);

            return stream;
        }

        public async Task<bool> SetContentAsync(RootName root, FileId target, Stream content, IProgress<ProgressValue> progress, Func<FileSystemInfoLocator> locatorResolver)
        {
            var context = await RequireContext(root);

            var locator = locatorResolver();
            var item = await AsyncFunc.Retry<BoxFile, BoxException>(async () => await context.Client.FilesManager.UploadNewVersionAsync(locator.Name, target.Value, new ProgressStream(content, progress)), RETRIES);

            return true;
        }

        public async Task<FileSystemInfoContract> CopyItemAsync(RootName root, FileSystemId source, string copyName, DirectoryId destination, bool recurse)
        {
            var context = await RequireContext(root);

            if (source is DirectoryId) {
                var request = new BoxFolderRequest() { Id = source.Value, Name = copyName, Parent = new BoxRequestEntity() { Id = destination.Value } };
                var item = await AsyncFunc.Retry<BoxFolder, BoxException>(async () => await context.Client.FoldersManager.CopyAsync(request, boxFolderFields), RETRIES);

                return new DirectoryInfoContract(item.Id, item.Name, item.CreatedAt.Value, item.ModifiedAt.Value);
            }
            else {
                var request = new BoxFileRequest() { Id = source.Value, Name = copyName, Parent = new BoxRequestEntity() { Id = destination.Value } };
                var item = await AsyncFunc.Retry<BoxFile, BoxException>(async () => await context.Client.FilesManager.CopyAsync(request, boxFileFields), RETRIES);

                return new FileInfoContract(item.Id, item.Name, item.CreatedAt.Value, item.ModifiedAt.Value, item.Size.Value, item.Sha1.ToLowerInvariant());
            }
        }

        public async Task<FileSystemInfoContract> MoveItemAsync(RootName root, FileSystemId source, string moveName, DirectoryId destination, Func<FileSystemInfoLocator> locatorResolver)
        {
            var context = await RequireContext(root);

            if (source is DirectoryId) {
                var request = new BoxFolderRequest() { Id = source.Value, Parent = new BoxRequestEntity() { Id = destination.Value, Type = BoxType.folder }, Name = moveName };
                var item = await AsyncFunc.Retry<BoxFolder, BoxException>(async () => await context.Client.FoldersManager.UpdateInformationAsync(request, fields: boxFolderFields), RETRIES);

                return new DirectoryInfoContract(item.Id, item.Name, item.CreatedAt.Value, item.ModifiedAt.Value);
            }
            else {
                var request = new BoxFileRequest() { Id = source.Value, Parent = new BoxRequestEntity() { Id = destination.Value, Type = BoxType.file }, Name = moveName };
                var item = await AsyncFunc.Retry<BoxFile, BoxException>(async () => await context.Client.FilesManager.UpdateInformationAsync(request, fields: boxFileFields), RETRIES);

                return new FileInfoContract(item.Id, item.Name, item.CreatedAt.Value, item.ModifiedAt.Value, item.Size.Value, item.Sha1.ToLowerInvariant());
            }
        }

        public async Task<DirectoryInfoContract> NewDirectoryItemAsync(RootName root, DirectoryId parent, string name)
        {
            var context = await RequireContext(root);

            var request = new BoxFolderRequest() { Name = name, Parent = new BoxRequestEntity() { Id = parent.Value } };
            var item = await AsyncFunc.Retry<BoxFolder, BoxException>(async () => await context.Client.FoldersManager.CreateAsync(request, boxFolderFields), RETRIES);

            return new DirectoryInfoContract(item.Id, item.Name, item.CreatedAt.Value, item.ModifiedAt.Value);
        }

        public async Task<FileInfoContract> NewFileItemAsync(RootName root, DirectoryId parent, string name, Stream content, IProgress<ProgressValue> progress)
        {
            var context = await RequireContext(root);

            var request = new BoxFileRequest() { Name = name, Parent = new BoxRequestEntity() { Id = parent.Value } };
            var item = await AsyncFunc.Retry<BoxFile, BoxException>(async () => await context.Client.FilesManager.UploadAsync(request, new ProgressStream(content, progress), boxFileFields), RETRIES);

            return new FileInfoContract(item.Id, item.Name, item.CreatedAt.Value, item.ModifiedAt.Value, item.Size.Value, item.Sha1.ToLowerInvariant());
        }

        public async Task<bool> RemoveItemAsync(RootName root, FileSystemId target, bool recurse)
        {
            var context = await RequireContext(root);

            var success = target is DirectoryId
                ? await AsyncFunc.Retry<bool, BoxException>(async () => await context.Client.FoldersManager.DeleteAsync(target.Value, recurse), RETRIES)
                : await AsyncFunc.Retry<bool, BoxException>(async () => await context.Client.FilesManager.DeleteAsync(target.Value), RETRIES);

            return success;
        }

        public async Task<FileSystemInfoContract> RenameItemAsync(RootName root, FileSystemId target, string newName, Func<FileSystemInfoLocator> locatorResolver)
        {
            var context = await RequireContext(root);

            if (target is DirectoryId) {
                var request = new BoxFolderRequest() { Id = target.Value, Name = newName };
                var item = await AsyncFunc.Retry<BoxFolder, BoxException>(async () => await context.Client.FoldersManager.UpdateInformationAsync(request, fields: boxFolderFields), RETRIES);

                return new DirectoryInfoContract(item.Id, item.Name, item.CreatedAt.Value, item.ModifiedAt.Value);
            }
            else {
                var request = new BoxFileRequest() { Id = target.Value, Name = newName };
                var item = await AsyncFunc.Retry<BoxFile, BoxException>(async () => await context.Client.FilesManager.UpdateInformationAsync(request, fields: boxFileFields), RETRIES);

                return new FileInfoContract(item.Id, item.Name, item.CreatedAt.Value, item.ModifiedAt.Value, item.Size.Value, item.Sha1.ToLowerInvariant());
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private static string DebuggerDisplay() => nameof(BoxGateway);
    }
}
