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
using CG.Web.MegaApiClient;
using IgorSoft.CloudFS.Interface;
using IgorSoft.CloudFS.Interface.Composition;
using IgorSoft.CloudFS.Interface.IO;
using IgorSoft.CloudFS.Gateways.Mega.Auth;

namespace IgorSoft.CloudFS.Gateways.Mega
{
    [ExportAsAsyncCloudGateway("Mega")]
    [ExportMetadata(nameof(CloudGatewayMetadata.CloudService), MegaGateway.SCHEMA)]
    [ExportMetadata(nameof(CloudGatewayMetadata.Capabilities), MegaGateway.CAPABILITIES)]
    [ExportMetadata(nameof(CloudGatewayMetadata.ServiceUri), MegaGateway.URL)]
    [ExportMetadata(nameof(CloudGatewayMetadata.ApiAssembly), MegaGateway.API)]
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public sealed class MegaGateway : IAsyncCloudGateway
    {
        private const string SCHEMA = "mega";

        private const GatewayCapabilities CAPABILITIES = GatewayCapabilities.All ^ GatewayCapabilities.ClearContent ^ GatewayCapabilities.SetContent ^ GatewayCapabilities.CopyItems ^ GatewayCapabilities.RenameItems;

        private const string URL = "https://mega.co.nz";

        private const string API = "MegaApiClient";

        private const int RETRIES = 3;

        private class MegaContext
        {
            public MegaApiClient Client { get; }

            public MegaContext(MegaApiClient client)
            {
                Client = client;
            }
        }

        private readonly IDictionary<RootName, MegaContext> contextCache = new Dictionary<RootName, MegaContext>();

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
            } catch (Exception) {
                return false;
            }
        }

        public async Task<DriveInfoContract> GetDriveAsync(RootName root, string apiKey, IDictionary<string, string> parameters)
        {
            var context = await RequireContextAsync(root, apiKey);

            var accountInformation = await context.Client.GetAccountInformationAsync();

            return new DriveInfoContract(root.Value, accountInformation.TotalQuota - accountInformation.UsedQuota, accountInformation.UsedQuota);
        }

        public async Task<RootDirectoryInfoContract> GetRootAsync(RootName root, string apiKey, IDictionary<string, string> parameters)
        {
            var context = await RequireContextAsync(root, apiKey);

            var nodes = await context.Client.GetNodesAsync();
            var item = nodes.Single(n => n.Type == NodeType.Root);

            return new RootDirectoryInfoContract(item.Id, DateTimeOffset.FromFileTime(0), item.LastModificationDate);
        }

        public async Task<IEnumerable<FileSystemInfoContract>> GetChildItemAsync(RootName root, DirectoryId parent)
        {
            var context = await RequireContextAsync(root);

            var nodes = await context.Client.GetNodesAsync();
            var parentItem = nodes.Single(n => n.Id == parent.Value);
            var items = await context.Client.GetNodesAsync(parentItem);

            return items.Select(i => i.ToFileSystemInfoContract());
        }

        public Task<bool> ClearContentAsync(RootName root, FileId target, Func<FileSystemInfoLocator> locatorResolver)
        {
            return Task.FromException<bool>(new NotSupportedException(Properties.Resources.SettingOfFileContentNotSupported));
        }

        public async Task<Stream> GetContentAsync(RootName root, FileId source)
        {
            var context = await RequireContextAsync(root);

            var nodes = await context.Client.GetNodesAsync();
            var item = nodes.Single(n => n.Id == source.Value);
            var stream = await context.Client.DownloadAsync(item, new Progress<double>());

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

            var nodes = await context.Client.GetNodesAsync();
            var sourceItem = nodes.Single(n => n.Id == source.Value);
            if (!string.IsNullOrEmpty(moveName) && moveName != sourceItem.Name)
                throw new NotSupportedException(Properties.Resources.RenamingOfFilesNotSupported);
            var destinationParentItem = nodes.Single(n => n.Id == destination.Value);
            var item = await context.Client.MoveAsync(sourceItem, destinationParentItem);

            return item.ToFileSystemInfoContract();
        }

        public async Task<DirectoryInfoContract> NewDirectoryItemAsync(RootName root, DirectoryId parent, string name)
        {
            var context = await RequireContextAsync(root);

            var nodes = await context.Client.GetNodesAsync();
            var parentItem = nodes.Single(n => n.Id == parent.Value);
            var item = await context.Client.CreateFolderAsync(name, parentItem);

            return new DirectoryInfoContract(item.Id, item.Name, item.LastModificationDate, item.LastModificationDate);
        }

        public async Task<FileInfoContract> NewFileItemAsync(RootName root, DirectoryId parent, string name, Stream content, IProgress<ProgressValue> progress)
        {
            if (content.Length == 0)
                return new ProxyFileInfoContract(name);

            var context = await RequireContextAsync(root);

            var nodes = await context.Client.GetNodesAsync();
            var parentItem = nodes.Single(n => n.Id == parent.Value);
            var contentLength = content.Length;
            var item = await context.Client.UploadAsync(content, name, parentItem, new Progress<double>(d => progress?.Report(new ProgressValue((int)(contentLength * d), (int)contentLength))));

            return new FileInfoContract(item.Id, item.Name, item.LastModificationDate, item.LastModificationDate, item.Size, null);
        }

        public async Task<bool> RemoveItemAsync(RootName root, FileSystemId target, bool recurse)
        {
            var context = await RequireContextAsync(root);

            var nodes = await context.Client.GetNodesAsync();
            var item = nodes.Single(n => n.Id == target.Value);
            await context.Client.DeleteAsync(item);

            return true;
        }

        public Task<FileSystemInfoContract> RenameItemAsync(RootName root, FileSystemId target, string newName, Func<FileSystemInfoLocator> locatorResolver)
        {
            return Task.FromException<FileSystemInfoContract>(new NotSupportedException(Properties.Resources.RenamingOfFilesNotSupported));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private static string DebuggerDisplay() => nameof(MegaGateway);
    }
}
