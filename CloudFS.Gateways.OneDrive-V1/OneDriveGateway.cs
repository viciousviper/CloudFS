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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OneDrive.Sdk;
using IgorSoft.CloudFS.Interface;
using IgorSoft.CloudFS.Interface.Composition;
using IgorSoft.CloudFS.Interface.IO;
using IgorSoft.CloudFS.Gateways.OneDrive_V1.OAuth;

namespace IgorSoft.CloudFS.Gateways.OneDrive_V1
{
    [ExportAsAsyncCloudGateway("OneDrive-V1")]
    [ExportMetadata(nameof(CloudGatewayMetadata.CloudService), OneDriveGateway.SCHEMA)]
    [ExportMetadata(nameof(CloudGatewayMetadata.Capabilities), OneDriveGateway.CAPABILITIES)]
    [ExportMetadata(nameof(CloudGatewayMetadata.ServiceUri), OneDriveGateway.URL)]
    [ExportMetadata(nameof(CloudGatewayMetadata.ApiAssembly), OneDriveGateway.API)]
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public sealed class OneDriveGateway : IAsyncCloudGateway, IPersistGatewaySettings
    {
        private const string SCHEMA = "onedrive_v1";

        private const GatewayCapabilities CAPABILITIES = GatewayCapabilities.All;

        private const string URL = "https://onedrive.live.com";

        private const string API = "OneDriveSDK";

        private const int RETRIES = 3;

        private class OneDriveContext
        {
            public IOneDriveClient Client { get; }

            public OneDriveContext(IOneDriveClient client)
            {
                Client = client;
            }
        }

        private readonly IDictionary<RootName, OneDriveContext> contextCache = new Dictionary<RootName, OneDriveContext>();

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
                var client = await OAuthAuthenticator.LoginAsync(root.UserName, apiKey, settingsPassPhrase);
                contextCache.Add(root, result = new OneDriveContext(client));
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

            var item = await AsyncFunc.RetryAsync<Drive, OneDriveException>(async () => await context.Client.Drive.Request().GetAsync(), RETRIES);

            return new DriveInfoContract(item.Id, item.Quota.Remaining, item.Quota.Used);
        }

        public async Task<RootDirectoryInfoContract> GetRootAsync(RootName root, string apiKey, IDictionary<string, string> parameters)
        {
            var context = await RequireContextAsync(root, apiKey);

            var item = await AsyncFunc.RetryAsync<Item, OneDriveException>(async () => await context.Client.Drive.Root.Request().GetAsync(), RETRIES);

            return new RootDirectoryInfoContract(item.Id, item.CreatedDateTime ?? DateTimeOffset.FromFileTime(0), item.LastModifiedDateTime ?? DateTimeOffset.FromFileTime(0));
        }

        public async Task<IEnumerable<FileSystemInfoContract>> GetChildItemAsync(RootName root, DirectoryId parent)
        {
            var context = await RequireContextAsync(root);

            var pagedCollection = await AsyncFunc.RetryAsync<IChildrenCollectionPage, OneDriveException>(async () => await context.Client.Drive.Items[parent.Value].Children.Request().GetAsync(), RETRIES);

            var items = pagedCollection.CurrentPage.ToList();

            while (pagedCollection.NextPageRequest != null) {
                pagedCollection = await pagedCollection.NextPageRequest.GetAsync();
                items.AddRange(pagedCollection.CurrentPage);
            }

            return items.Select(i => i.ToFileSystemInfoContract());
        }

        public async Task<bool> ClearContentAsync(RootName root, FileId target, Func<FileSystemInfoLocator> locatorResolver)
        {
            var context = await RequireContextAsync(root);

            var item = await AsyncFunc.RetryAsync<Item, OneDriveException>(async () => await context.Client.Drive.Items[target.Value].Content.Request().PutAsync<Item>(Stream.Null), RETRIES);

            return true;
        }

        public async Task<Stream> GetContentAsync(RootName root, FileId source)
        {
            var context = await RequireContextAsync(root);

            var stream = await AsyncFunc.RetryAsync<Stream, OneDriveException>(async () => await context.Client.Drive.Items[source.Value].Content.Request().GetAsync(), RETRIES);

            return stream;
        }

        public async Task<bool> SetContentAsync(RootName root, FileId target, Stream content, IProgress<ProgressValue> progress, Func<FileSystemInfoLocator> locatorResolver)
        {
            var context = await RequireContextAsync(root);

            var item = await AsyncFunc.RetryAsync<Item, OneDriveException>(async () => await context.Client.Drive.Items[target.Value].Content.Request().PutAsync<Item>(content), RETRIES);

            return true;
        }

        public async Task<FileSystemInfoContract> CopyItemAsync(RootName root, FileSystemId source, string copyName, DirectoryId destination, bool recurse)
        {
            var context = await RequireContextAsync(root);

            var asyncStatus = await context.Client.Drive.Items[source.Value].Copy(copyName, new ItemReference { Id = destination.Value }).Request().PostAsync();

            var item = await asyncStatus.CompleteOperationAsync(null, CancellationToken.None);

            return item.ToFileSystemInfoContract();
        }

        public async Task<FileSystemInfoContract> MoveItemAsync(RootName root, FileSystemId source, string moveName, DirectoryId destination, Func<FileSystemInfoLocator> locatorResolver)
        {
            var context = await RequireContextAsync(root);

            var destinationPathReference = new ItemReference { Id = destination.Value };
            var item = await AsyncFunc.RetryAsync<Item, OneDriveException>(async () => await context.Client.Drive.Items[source.Value].Request().UpdateAsync(new Item { ParentReference = destinationPathReference, Name = moveName }), RETRIES);

            return item.ToFileSystemInfoContract();
        }

        public async Task<DirectoryInfoContract> NewDirectoryItemAsync(RootName root, DirectoryId parent, string name)
        {
            var context = await RequireContextAsync(root);

            var folder = new Item() { Folder = new Folder() };
            var item = await AsyncFunc.RetryAsync<Item, OneDriveException>(async () => await context.Client.Drive.Items[parent.Value].ItemWithPath(name).Request().CreateAsync(folder), RETRIES);

            return new DirectoryInfoContract(item.Id, item.Name, item.CreatedDateTime ?? DateTimeOffset.FromFileTime(0), item.LastModifiedDateTime ?? DateTimeOffset.FromFileTime(0));
        }

        public async Task<FileInfoContract> NewFileItemAsync(RootName root, DirectoryId parent, string name, Stream content, IProgress<ProgressValue> progress)
        {
            if (content.Length == 0)
                return new ProxyFileInfoContract(name);

            var context = await RequireContextAsync(root);

            var item = await AsyncFunc.RetryAsync<Item, OneDriveException>(async () => await context.Client.Drive.Items[parent.Value].ItemWithPath(name).Content.Request().PutAsync<Item>(content), RETRIES);

            return new FileInfoContract(item.Id, item.Name, item.CreatedDateTime ?? DateTimeOffset.FromFileTime(0), item.LastModifiedDateTime ?? DateTimeOffset.FromFileTime(0), item.Size ?? -1, item.File.Hashes.Sha1Hash.ToLowerInvariant());
        }

        public async Task<bool> RemoveItemAsync(RootName root, FileSystemId target, bool recurse)
        {
            var context = await RequireContextAsync(root);

            await AsyncFunc.RetryAsync<OneDriveException>(async () => await context.Client.Drive.Items[target.Value].Request().DeleteAsync(), RETRIES);

            return true;
        }

        public async Task<FileSystemInfoContract> RenameItemAsync(RootName root, FileSystemId target, string newName, Func<FileSystemInfoLocator> locatorResolver)
        {
            var context = await RequireContextAsync(root);

            var item = await AsyncFunc.RetryAsync<Item, OneDriveException>(async () => await context.Client.Drive.Items[target.Value].Request().UpdateAsync(new Item() { Name = newName }), RETRIES);

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
