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
using System.Security.Authentication;
using System.Threading.Tasks;
using Polly;
using OneDrive;
using IgorSoft.CloudFS.Gateways.OneDrive_Legacy.OAuth;
using IgorSoft.CloudFS.Interface;
using IgorSoft.CloudFS.Interface.Composition;
using IgorSoft.CloudFS.Interface.IO;

namespace IgorSoft.CloudFS.Gateways.OneDrive_Legacy
{
    [ExportAsAsyncCloudGateway("OneDrive-Legacy")]
    [ExportMetadata(nameof(CloudGatewayMetadata.CloudService), OneDriveGateway.SCHEMA)]
    [ExportMetadata(nameof(CloudGatewayMetadata.Capabilities), OneDriveGateway.CAPABILITIES)]
    [ExportMetadata(nameof(CloudGatewayMetadata.ServiceUri), OneDriveGateway.URL)]
    [ExportMetadata(nameof(CloudGatewayMetadata.ApiAssembly), OneDriveGateway.API)]
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public sealed class OneDriveGateway : IAsyncCloudGateway, IPersistGatewaySettings
    {
        private const string SCHEMA = "onedrive_legacy";

        private const GatewayCapabilities CAPABILITIES = GatewayCapabilities.All ^ GatewayCapabilities.ClearContent;

        private const string URL = "https://onedrive.live.com";

        private const string API = "OneDriveSDK";

        private class OneDriveContext
        {
            public ODConnection Connection { get; }

            public ODDrive Drive { get; }

            public OneDriveContext(ODConnection connection, ODDrive drive)
            {
                Connection = connection;
                Drive = drive;
            }
        }

        private readonly IDictionary<RootName, OneDriveContext> contextCache = new Dictionary<RootName, OneDriveContext>();

        private readonly Policy retryPolicy = Policy.Handle<ODException>().WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        private string settingsPassPhrase;

        [ImportingConstructor]
        public OneDriveGateway([Import(ExportContracts.SettingsPassPhrase)] string settingsPassPhrase)
        {
            this.settingsPassPhrase = settingsPassPhrase;
        }

        private async Task<OneDriveContext> RequireContextAsync(RootName root, string apiKey = null)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            var result = default(OneDriveContext);
            if (!contextCache.TryGetValue(root, out result)) {
                var connection = await OAuthAuthenticator.LoginAsync(root.UserName, apiKey, settingsPassPhrase);
                var drive = await connection.GetDrive();
                contextCache.Add(root, result = new OneDriveContext(connection, drive));
            }
            return result;
        }

        public async Task<bool> TryAuthenticateAsync(RootName root, string apiKey, IDictionary<string, string> parameters)
        {
            try {
                await RequireContextAsync(root, apiKey);
                return true;
            } catch (AuthenticationException) {
                return false;
            }
        }

        public async Task<DriveInfoContract> GetDriveAsync(RootName root, string apiKey, IDictionary<string, string> parameters)
        {
            var context = await RequireContextAsync(root, apiKey);

            var item = await retryPolicy.ExecuteAsync(() => context.Connection.GetDrive());

            return new DriveInfoContract(item.Id, item.Quota.Remaining, item.Quota.Used);
        }

        public async Task<RootDirectoryInfoContract> GetRootAsync(RootName root, string apiKey, IDictionary<string, string> parameters)
        {
            var context = await RequireContextAsync(root, apiKey);

            var item = await retryPolicy.ExecuteAsync(() => context.Connection.GetRootItemAsync(ItemRetrievalOptions.Default));

            return new RootDirectoryInfoContract(item.Id, item.CreatedDateTime, item.LastModifiedDateTime);
        }

        public async Task<IEnumerable<FileSystemInfoContract>> GetChildItemAsync(RootName root, DirectoryId parent)
        {
            var context = await RequireContextAsync(root);

            var itemReference = ODConnection.ItemReferenceForItemId(parent.Value, context.Drive.Id);
            var items = await retryPolicy.ExecuteAsync(() => context.Connection.GetChildrenOfItemAsync(itemReference, ChildrenRetrievalOptions.Default));

            return items.Collection.Select(i => i.ToFileSystemInfoContract());
        }

        public Task<bool> ClearContentAsync(RootName root, FileId target, Func<FileSystemInfoLocator> locatorResolver)
        {
            return Task.FromException<bool>(new NotSupportedException(Properties.Resources.EmptyFilesNotSupported));
        }

        public async Task<Stream> GetContentAsync(RootName root, FileId source)
        {
            var context = await RequireContextAsync(root);

            var itemReference = ODConnection.ItemReferenceForItemId(source.Value, context.Drive.Id);
            var stream = await retryPolicy.ExecuteAsync(() => context.Connection.DownloadStreamForItemAsync(itemReference, StreamDownloadOptions.Default));

            return stream;
        }

        public async Task<bool> SetContentAsync(RootName root, FileId target, Stream content, IProgress<ProgressValue> progress, Func<FileSystemInfoLocator> locatorResolver)
        {
            var context = await RequireContextAsync(root);

            var itemReference = ODConnection.ItemReferenceForItemId(target.Value, context.Drive.Id);
            var uploadOptions = progress != null ? new ItemUploadOptions() { ProgressReporter = (complete, transfered, total) => progress.Report(new ProgressValue(complete, (int)transfered, (int)total)) } : ItemUploadOptions.Default;
            var retryPolicyWithAction = Policy.Handle<ODException>().WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (ex, ts) => content.Seek(0, SeekOrigin.Begin));
            var item = await retryPolicyWithAction.ExecuteAsync(() => context.Connection.PutContentsAsync(itemReference, content, uploadOptions));

            return true;
        }

        public async Task<FileSystemInfoContract> CopyItemAsync(RootName root, FileSystemId source, string copyName, DirectoryId destination, bool recurse)
        {
            var context = await RequireContextAsync(root);

            var itemReference = ODConnection.ItemReferenceForItemId(source.Value, context.Drive.Id);
            var destinationPathReference = ODConnection.ItemReferenceForItemId(destination.Value, context.Drive.Id);
            var task = await retryPolicy.ExecuteAsync(() => context.Connection.CopyItemAsync(itemReference, destinationPathReference, copyName));

            while (task.Status.Status != AsyncJobStatus.Completed) {
                await Task.Delay(20);
                await task.Refresh(context.Connection);
            }

            var destinationPathItems = await retryPolicy.ExecuteAsync(() => context.Connection.GetChildrenOfItemAsync(destinationPathReference, ChildrenRetrievalOptions.Default));
            return destinationPathItems.Collection.Single(item => item.Name == copyName).ToFileSystemInfoContract();
        }

        public async Task<FileSystemInfoContract> MoveItemAsync(RootName root, FileSystemId source, string moveName, DirectoryId destination, Func<FileSystemInfoLocator> locatorResolver)
        {
            var context = await RequireContextAsync(root);

            var itemReference = ODConnection.ItemReferenceForItemId(source.Value, context.Drive.Id);
            var destinationPathReference = ODConnection.ItemReferenceForItemId(destination.Value, context.Drive.Id);
            var item = await retryPolicy.ExecuteAsync(() => context.Connection.PatchItemAsync(itemReference, new ODItem() { ParentReference = destinationPathReference , Name = moveName }));

            return item.ToFileSystemInfoContract();
        }

        public async Task<DirectoryInfoContract> NewDirectoryItemAsync(RootName root, DirectoryId parent, string name)
        {
            var context = await RequireContextAsync(root);

            var itemReference = ODConnection.ItemReferenceForItemId(parent.Value, context.Drive.Id);
            var item = await retryPolicy.ExecuteAsync(() => context.Connection.CreateFolderAsync(itemReference, name));

            return new DirectoryInfoContract(item.Id, item.Name, item.CreatedDateTime, item.LastModifiedDateTime);
        }

        public async Task<FileInfoContract> NewFileItemAsync(RootName root, DirectoryId parent, string name, Stream content, IProgress<ProgressValue> progress)
        {
            if (content.Length == 0)
                return new ProxyFileInfoContract(name);

            var context = await RequireContextAsync(root);

            var itemReference = ODConnection.ItemReferenceForItemId(parent.Value, context.Drive.Id);
            var uploadOptions = progress != null ? new ItemUploadOptions() { ProgressReporter = (complete, transfered, total) => progress.Report(new ProgressValue(complete, (int)transfered, (int)total)) } : ItemUploadOptions.Default;
            var retryPolicyWithAction = Policy.Handle<ODException>().WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (ex, ts) => content.Seek(0, SeekOrigin.Begin));
            var item = await retryPolicyWithAction.ExecuteAsync(() => context.Connection.PutNewFileToParentItemAsync(itemReference, name, content, uploadOptions));

            return new FileInfoContract(item.Id, item.Name, item.CreatedDateTime, item.LastModifiedDateTime, (FileSize)item.Size, item.File.Hashes.Sha1.ToLowerInvariant());
        }

        public async Task<bool> RemoveItemAsync(RootName root, FileSystemId target, bool recurse)
        {
            var context = await RequireContextAsync(root);

            var itemReference = ODConnection.ItemReferenceForItemId(target.Value, context.Drive.Id);
            var success = await retryPolicy.ExecuteAsync(() => context.Connection.DeleteItemAsync(itemReference, ItemDeleteOptions.Default));

            return success;
        }

        public async Task<FileSystemInfoContract> RenameItemAsync(RootName root, FileSystemId target, string newName, Func<FileSystemInfoLocator> locatorResolver)
        {
            var context = await RequireContextAsync(root);

            var itemReference = ODConnection.ItemReferenceForItemId(target.Value, context.Drive.Id);
            var item = await retryPolicy.ExecuteAsync(() => context.Connection.PatchItemAsync(itemReference, new ODItem() { Name = newName }));

            return item.ToFileSystemInfoContract();
        }

        public void PurgeSettings(RootName root)
        {
            OAuthAuthenticator.PurgeRefreshToken(root?.UserName);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private static string DebuggerDisplay() => nameof(OneDriveGateway);
    }
}
