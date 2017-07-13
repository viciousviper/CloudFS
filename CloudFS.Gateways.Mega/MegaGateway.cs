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
using CG.Web.MegaApiClient;
using IgorSoft.CloudFS.Gateways.Mega.Auth;
using IgorSoft.CloudFS.Interface;
using IgorSoft.CloudFS.Interface.Composition;
using IgorSoft.CloudFS.Interface.IO;

namespace IgorSoft.CloudFS.Gateways.Mega
{
    [ExportAsAsyncCloudGateway("Mega")]
    [ExportMetadata(nameof(CloudGatewayMetadata.CloudService), MegaGateway.SCHEMA)]
    [ExportMetadata(nameof(CloudGatewayMetadata.Capabilities), MegaGateway.CAPABILITIES)]
    [ExportMetadata(nameof(CloudGatewayMetadata.ServiceUri), MegaGateway.URL)]
    [ExportMetadata(nameof(CloudGatewayMetadata.ApiAssembly), MegaGateway.API)]
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public sealed class MegaGateway : IAsyncCloudGateway, IPersistGatewaySettings
    {
        private const string SCHEMA = "mega";

        private const GatewayCapabilities CAPABILITIES = GatewayCapabilities.All ^ GatewayCapabilities.ClearContent ^ GatewayCapabilities.SetContent ^ GatewayCapabilities.CopyItems;

        private const string URL = "https://mega.co.nz";

        private const string API = "MegaApiClient";

        private class MegaContext
        {
            public MegaApiClient Client { get; }

            public MegaContext(MegaApiClient client)
            {
                Client = client;
            }
        }

        private readonly IDictionary<RootName, MegaContext> contextCache = new Dictionary<RootName, MegaContext>();

        private readonly Policy retryPolicy = Policy.Handle<ApiException>().WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        private string settingsPassPhrase;

        [ImportingConstructor]
        public MegaGateway([Import(ExportContracts.SettingsPassPhrase)] string settingsPassPhrase)
        {
            this.settingsPassPhrase = settingsPassPhrase;
        }

        private async Task<MegaContext> RequireContextAsync(RootName root, string apiKey = null)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            var result = default(MegaContext);
            if (!contextCache.TryGetValue(root, out result)) {
                var client = await Authenticator.LoginAsync(root.UserName, apiKey, settingsPassPhrase);
                contextCache.Add(root, result = new MegaContext(client));
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

            var accountInformation = await retryPolicy.ExecuteAsync(() => context.Client.GetAccountInformationAsync());

            return new DriveInfoContract(root.Value, accountInformation.TotalQuota - accountInformation.UsedQuota, accountInformation.UsedQuota);
        }

        public async Task<RootDirectoryInfoContract> GetRootAsync(RootName root, string apiKey, IDictionary<string, string> parameters)
        {
            var context = await RequireContextAsync(root, apiKey);

            var nodes = await retryPolicy.ExecuteAsync(() => context.Client.GetNodesAsync());
            var item = nodes.Single(n => n.Type == NodeType.Root);

            return new RootDirectoryInfoContract(item.Id, item.CreationDate, item.ModificationDate ?? DateTimeOffset.FromFileTime(0));
        }

        public async Task<IEnumerable<FileSystemInfoContract>> GetChildItemAsync(RootName root, DirectoryId parent)
        {
            var context = await RequireContextAsync(root);

            var nodes = await retryPolicy.ExecuteAsync(() => context.Client.GetNodesAsync());
            var parentItem = nodes.Single(n => n.Id == parent.Value);
            var items = await retryPolicy.ExecuteAsync(() => context.Client.GetNodesAsync(parentItem));

            return items.Select(i => i.ToFileSystemInfoContract());
        }

        public Task<bool> ClearContentAsync(RootName root, FileId target, Func<FileSystemInfoLocator> locatorResolver)
        {
            return Task.FromException<bool>(new NotSupportedException(Properties.Resources.SettingOfFileContentNotSupported));
        }

        public async Task<Stream> GetContentAsync(RootName root, FileId source)
        {
            var context = await RequireContextAsync(root);

            var nodes = await retryPolicy.ExecuteAsync(() => context.Client.GetNodesAsync());
            var item = nodes.Single(n => n.Id == source.Value);
            var stream = await retryPolicy.ExecuteAsync(() => context.Client.DownloadAsync(item, new Progress<double>()));

            return stream;
        }

        public Task<bool> SetContentAsync(RootName root, FileId target, Stream content, IProgress<ProgressValue> progress, Func<FileSystemInfoLocator> locatorResolver)
        {
            return Task.FromException<bool>(new NotSupportedException(Properties.Resources.SettingOfFileContentNotSupported));
        }

        public Task<FileSystemInfoContract> CopyItemAsync(RootName root, FileSystemId source, string copyName, DirectoryId destination, bool recurse)
        {
            return Task.FromException<FileSystemInfoContract>(new NotSupportedException(Properties.Resources.CopyingOfFilesNotSupported));
        }

        public async Task<FileSystemInfoContract> MoveItemAsync(RootName root, FileSystemId source, string moveName, DirectoryId destination, Func<FileSystemInfoLocator> locatorResolver)
        {
            var context = await RequireContextAsync(root);

            var nodes = await retryPolicy.ExecuteAsync(() => context.Client.GetNodesAsync());
            var sourceItem = nodes.Single(n => n.Id == source.Value);
            var destinationParentItem = nodes.Single(n => n.Id == destination.Value);
            var item = await retryPolicy.ExecuteAsync(() => context.Client.MoveAsync(sourceItem, destinationParentItem));

            return item.ToFileSystemInfoContract();
        }

        public async Task<DirectoryInfoContract> NewDirectoryItemAsync(RootName root, DirectoryId parent, string name)
        {
            var context = await RequireContextAsync(root);

            var nodes = await retryPolicy.ExecuteAsync(() => context.Client.GetNodesAsync());
            var parentItem = nodes.Single(n => n.Id == parent.Value);
            var item = await retryPolicy.ExecuteAsync(() => context.Client.CreateFolderAsync(name, parentItem));

            return new DirectoryInfoContract(item.Id, item.Name, item.CreationDate, item.ModificationDate ?? DateTimeOffset.FromFileTime(0));
        }

        public async Task<FileInfoContract> NewFileItemAsync(RootName root, DirectoryId parent, string name, Stream content, IProgress<ProgressValue> progress)
        {
            if (content.Length == 0)
                return new ProxyFileInfoContract(name);

            var context = await RequireContextAsync(root);

            var nodes = await retryPolicy.ExecuteAsync(() => context.Client.GetNodesAsync());
            var parentItem = nodes.Single(n => n.Id == parent.Value);
            var contentLength = content.Length;
            var retryPolicyWithAction = Policy.Handle<ApiException>().WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (ex, ts) => content.Seek(0, SeekOrigin.Begin));
            var item = await retryPolicyWithAction.ExecuteAsync(() => context.Client.UploadAsync(content, name, parentItem, new Progress<double>(d => progress?.Report(new ProgressValue((int)(contentLength * d), (int)contentLength)))));

            return new FileInfoContract(item.Id, item.Name, item.CreationDate, item.ModificationDate ?? DateTimeOffset.FromFileTime(0), (FileSize)item.Size, null);
        }

        public async Task<bool> RemoveItemAsync(RootName root, FileSystemId target, bool recurse)
        {
            var context = await RequireContextAsync(root);

            var nodes = await context.Client.GetNodesAsync();
            var item = nodes.Single(n => n.Id == target.Value);
            await retryPolicy.ExecuteAsync(() => context.Client.DeleteAsync(item));

            return true;
        }

        public async Task<FileSystemInfoContract> RenameItemAsync(RootName root, FileSystemId target, string newName, Func<FileSystemInfoLocator> locatorResolver)
        {
            var context = await RequireContextAsync(root);

            var nodes = await retryPolicy.ExecuteAsync(() => context.Client.GetNodesAsync());
            var targetItem = nodes.Single(n => n.Id == target.Value);
            var item = await retryPolicy.ExecuteAsync(() => context.Client.RenameAsync(targetItem, newName));

            return item.ToFileSystemInfoContract();
        }

        public void PurgeSettings(RootName root)
        {
            Authenticator.PurgeRefreshToken(root?.UserName);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private static string DebuggerDisplay() => nameof(MegaGateway);
    }
}
