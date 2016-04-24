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
using OneDrive;
using IgorSoft.CloudFS.Interface;
using IgorSoft.CloudFS.Interface.Composition;
using IgorSoft.CloudFS.Interface.IO;
using IgorSoft.CloudFS.Gateways.OneDrive.OAuth;

namespace IgorSoft.CloudFS.Gateways.OneDrive
{
    [ExportAsAsyncCloudGateway("OneDrive")]
    [ExportMetadata(nameof(CloudGatewayMetadata.CloudService), OneDriveGateway.SCHEMA)]
    [ExportMetadata(nameof(CloudGatewayMetadata.Capabilities), OneDriveGateway.CAPABILITIES)]
    [ExportMetadata(nameof(CloudGatewayMetadata.ServiceUri), OneDriveGateway.URL)]
    [ExportMetadata(nameof(CloudGatewayMetadata.ApiAssembly), OneDriveGateway.API)]
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public sealed class OneDriveGateway : IAsyncCloudGateway
    {
        private const string SCHEMA = "onedrive";

        private const GatewayCapabilities CAPABILITIES = GatewayCapabilities.All ^ GatewayCapabilities.ClearContent;

        private const string URL = "https://onedrive.live.com";

        private const string API = "OneDriveSDK";

        private const int RETRIES = 3;

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

        private async Task<OneDriveContext> RequireContext(RootName root, string apiKey = null)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            var result = default(OneDriveContext);
            if (!contextCache.TryGetValue(root, out result)) {
                var connection = await OAuthAuthenticator.Login(root.UserName, apiKey);
                var drive = await connection.GetDrive();
                contextCache.Add(root, result = new OneDriveContext(connection, drive));
            }
            return result;
        }

        public async Task<DriveInfoContract> GetDriveAsync(RootName root, string apiKey, IDictionary<string, string> parameters)
        {
            var context = await RequireContext(root, apiKey);

            var item = await AsyncFunc.Retry<ODDrive, ODException>(async () => await context.Connection.GetDrive(), RETRIES);

            return new DriveInfoContract(item.Id, item.Quota.Remaining, item.Quota.Used);
        }

        public async Task<RootDirectoryInfoContract> GetRootAsync(RootName root, string apiKey)
        {
            var context = await RequireContext(root, apiKey);

            var item = await AsyncFunc.Retry<ODItem, ODException>(async () => await context.Connection.GetRootItemAsync(ItemRetrievalOptions.Default), RETRIES);

            return new RootDirectoryInfoContract(item.Id, item.CreatedDateTime, item.LastModifiedDateTime);
        }

        public async Task<IEnumerable<FileSystemInfoContract>> GetChildItemAsync(RootName root, DirectoryId parent)
        {
            var context = await RequireContext(root);

            var itemReference = ODConnection.ItemReferenceForItemId(parent.Value, context.Drive.Id);
            var items = await AsyncFunc.Retry<ODItemCollection, ODException>(async () => await context.Connection.GetChildrenOfItemAsync(itemReference, ChildrenRetrievalOptions.Default), RETRIES);

            return items.Collection.Select(i => i.ToFileSystemInfoContract());
        }

        public Task<bool> ClearContentAsync(RootName root, FileId target, Func<FileSystemInfoLocator> locatorResolver)
        {
            return Task.FromException<bool>(new NotSupportedException(Resources.EmptyFilesNotSupported));
        }

        public async Task<Stream> GetContentAsync(RootName root, FileId source)
        {
            var context = await RequireContext(root);

            var itemReference = ODConnection.ItemReferenceForItemId(source.Value, context.Drive.Id);
            var stream = await AsyncFunc.Retry<Stream, ODException>(async () => await context.Connection.DownloadStreamForItemAsync(itemReference, StreamDownloadOptions.Default), RETRIES);

            return stream;
        }

        public async Task<bool> SetContentAsync(RootName root, FileId target, Stream content, IProgress<ProgressValue> progress, Func<FileSystemInfoLocator> locatorResolver)
        {
            var context = await RequireContext(root);

            var itemReference = ODConnection.ItemReferenceForItemId(target.Value, context.Drive.Id);
            var uploadOptions = progress != null ? new ItemUploadOptions() { ProgressReporter = (complete, transfered, total) => progress.Report(new ProgressValue(complete, (int)transfered, (int)total)) } : ItemUploadOptions.Default;
            var item = await AsyncFunc.Retry<ODItem, ODException>(async () => await context.Connection.PutContentsAsync(itemReference, content, uploadOptions), RETRIES);

            return true;
        }

        public async Task<FileSystemInfoContract> CopyItemAsync(RootName root, FileSystemId source, string copyName, DirectoryId destination, bool recurse)
        {
            var context = await RequireContext(root);

            var itemReference = ODConnection.ItemReferenceForItemId(source.Value, context.Drive.Id);
            var destinationPathReference = ODConnection.ItemReferenceForItemId(destination.Value, context.Drive.Id);
            var task = await context.Connection.CopyItemAsync(itemReference, destinationPathReference, copyName);

            while (task.Status.Status != AsyncJobStatus.Complete) {
                await Task.Delay(20);
                await task.Refresh(context.Connection);
            }

            return task.FinishedItem.ToFileSystemInfoContract();
        }

        public async Task<FileSystemInfoContract> MoveItemAsync(RootName root, FileSystemId source, string moveName, DirectoryId destination, Func<FileSystemInfoLocator> locatorResolver)
        {
            var context = await RequireContext(root);

            var itemReference = ODConnection.ItemReferenceForItemId(source.Value, context.Drive.Id);
            var destinationPathReference = ODConnection.ItemReferenceForItemId(destination.Value, context.Drive.Id);
            var item = await AsyncFunc.Retry<ODItem, ODException>(async () => await context.Connection.PatchItemAsync(itemReference, new ODItem() { ParentReference = destinationPathReference , Name = moveName }), RETRIES);

            return item.ToFileSystemInfoContract();
        }

        public async Task<DirectoryInfoContract> NewDirectoryItemAsync(RootName root, DirectoryId parent, string name)
        {
            var context = await RequireContext(root);

            var itemReference = ODConnection.ItemReferenceForItemId(parent.Value, context.Drive.Id);
            var item = await AsyncFunc.Retry<ODItem, ODException>(async () => await context.Connection.CreateFolderAsync(itemReference, name), RETRIES);

            return new DirectoryInfoContract(item.Id, item.Name, item.CreatedDateTime, item.LastModifiedDateTime);
        }

        public async Task<FileInfoContract> NewFileItemAsync(RootName root, DirectoryId parent, string name, Stream content, IProgress<ProgressValue> progress)
        {
            if (content.Length == 0)
                return new ProxyFileInfoContract(name);

            var context = await RequireContext(root);

            var itemReference = ODConnection.ItemReferenceForItemId(parent.Value, context.Drive.Id);
            var uploadOptions = progress != null ? new ItemUploadOptions() { ProgressReporter = (complete, transfered, total) => progress.Report(new ProgressValue(complete, (int)transfered, (int)total)) } : ItemUploadOptions.Default;
            var item = await AsyncFunc.Retry<ODItem, ODException>(async () => await context.Connection.PutNewFileToParentItemAsync(itemReference, name, content, uploadOptions), RETRIES);

            return new FileInfoContract(item.Id, item.Name, item.CreatedDateTime, item.LastModifiedDateTime, item.Size, item.File.Hashes.Sha1.ToLowerInvariant());
        }

        public async Task<bool> RemoveItemAsync(RootName root, FileSystemId target, bool recurse)
        {
            var context = await RequireContext(root);

            var itemReference = ODConnection.ItemReferenceForItemId(target.Value, context.Drive.Id);
            var success = await AsyncFunc.Retry<bool, ODException>(async () => await context.Connection.DeleteItemAsync(itemReference, ItemDeleteOptions.Default), RETRIES);

            return success;
        }

        public async Task<FileSystemInfoContract> RenameItemAsync(RootName root, FileSystemId target, string newName, Func<FileSystemInfoLocator> locatorResolver)
        {
            var context = await RequireContext(root);

            var itemReference = ODConnection.ItemReferenceForItemId(target.Value, context.Drive.Id);
            var item = await AsyncFunc.Retry<ODItem, ODException>(async () => await context.Connection.PatchItemAsync(itemReference, new ODItem() { Name = newName }), RETRIES);

            return item.ToFileSystemInfoContract();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private static string DebuggerDisplay() => nameof(OneDriveGateway);
    }
}
