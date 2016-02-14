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
using YandexDisk.Client;
using YandexDisk.Client.Clients;
using YandexDisk.Client.Protocol;
using IgorSoft.CloudFS.Gateways.Yandex.OAuth;
using IgorSoft.CloudFS.Interface;
using IgorSoft.CloudFS.Interface.Composition;
using IgorSoft.CloudFS.Interface.IO;

namespace IgorSoft.CloudFS.Gateways.Yandex
{
    [ExportAsAsyncCloudGateway("Yandex")]
    [ExportMetadata(nameof(CloudGatewayMetadata.CloudService), YandexGateway.SCHEMA)]
    [ExportMetadata(nameof(CloudGatewayMetadata.Capabilities), YandexGateway.CAPABILITIES)]
    [ExportMetadata(nameof(CloudGatewayMetadata.ServiceUri), YandexGateway.URL)]
    [ExportMetadata(nameof(CloudGatewayMetadata.ApiAssembly), nameof(YandexDisk))]
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public sealed class YandexGateway : IAsyncCloudGateway
    {
        private const string SCHEMA = "yandex";

        private const GatewayCapabilities CAPABILITIES = GatewayCapabilities.All ^ GatewayCapabilities.ItemId;

        private const string URL = "https://www.yandex.com";

        private const int RETRIES = 3;

        private class YandexContext
        {
            public IDiskApi Client { get; }

            public YandexContext(IDiskApi client)
            {
                Client = client;
            }
        }

        private IDictionary<RootName, YandexContext> contextCache = new Dictionary<RootName, YandexContext>();

        private async Task<YandexContext> RequireContext(RootName root, string apiKey = null)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            var result = default(YandexContext);
            if (!contextCache.TryGetValue(root, out result)) {
                var client = await OAuthAuthenticator.Login(root.UserName, apiKey);
                contextCache.Add(root, result = new YandexContext(client));
            }
            return result;
        }

        private async Task<bool> OperationProgress(YandexContext context, Link link)
        {
            if (link.HttpStatusCode == System.Net.HttpStatusCode.Accepted) {
                var operation = default(Operation);
                do {
                    Thread.Sleep(100);
                    operation = await context.Client.Commands.GetOperationStatus(link, CancellationToken.None);
                    if (operation.Status == OperationStatus.Failure)
                        return false;
                } while (operation.Status == OperationStatus.InProgress);
            }
            return true;
        }

        public async Task<DriveInfoContract> GetDriveAsync(RootName root, string apiKey, IDictionary<string, string> parameters)
        {
            var context = await RequireContext(root, apiKey);

            //var item = await AsyncFunc.Retry<Disk, YandexApiException>(async () => await context.Client.MetaInfo.GetDiskInfoAsync(CancellationToken.None), RETRIES);
            var item = await context.Client.MetaInfo.GetDiskInfoAsync(CancellationToken.None);

            return new DriveInfoContract(root.Value, item.TotalSpace - item.UsedSpace - item.TrashSize, item.UsedSpace);
        }

        public async Task<RootDirectoryInfoContract> GetRootAsync(RootName root, string apiKey)
        {
            var context = await RequireContext(root, apiKey);

            var request = new ResourceRequest() { Path = "/" };
            //var item = await AsyncFunc.Retry<Resource, YandexApiException>(async () => await context.Client.MetaInfo.GetInfoAsync(request, CancellationToken.None), RETRIES);
            var item = await context.Client.MetaInfo.GetInfoAsync(request, CancellationToken.None);

            return new RootDirectoryInfoContract(item.Path, item.Created, item.Modified);
        }

        public async Task<IEnumerable<FileSystemInfoContract>> GetChildItemAsync(RootName root, DirectoryId parent)
        {
            var context = await RequireContext(root);

            var request = new ResourceRequest() { Path = parent.Value };
            //var item = await AsyncFunc.Retry<Resource, YandexApiException>(async () => await context.Client.MetaInfo.GetInfoAsync(request, CancellationToken.None), RETRIES);
            var item = await context.Client.MetaInfo.GetInfoAsync(request, CancellationToken.None);

            return item.Embedded.Items.Select(i => i.ToFileSystemInfoContract());
        }

        public async Task<bool> ClearContentAsync(RootName root, FileId target, Func<FileSystemInfoLocator> locatorResolver)
        {
            var context = await RequireContext(root);

            var link = await context.Client.Files.GetUploadLinkAsync(target.Value, true, CancellationToken.None);
            await context.Client.Files.UploadAsync(link, new MemoryStream(), CancellationToken.None);

            return true;
        }

        public async Task<Stream> GetContentAsync(RootName root, FileId source)
        {
            var context = await RequireContext(root);

            var link = await context.Client.Files.GetDownloadLinkAsync(source.Value, CancellationToken.None);
            var stream = await context.Client.Files.DownloadAsync(link, CancellationToken.None);

            return stream;
        }

        public async Task<bool> SetContentAsync(RootName root, FileId target, Stream content, IProgress<ProgressValue> progress, Func<FileSystemInfoLocator> locatorResolver)
        {
            var context = await RequireContext(root);

            var link = await context.Client.Files.GetUploadLinkAsync(target.Value, true, CancellationToken.None);
            await context.Client.Files.UploadAsync(link, content, CancellationToken.None);

            return true;
        }

        public async Task<FileSystemInfoContract> CopyItemAsync(RootName root, FileSystemId source, string copyName, DirectoryId destination, bool recurse)
        {
            var context = await RequireContext(root);

            var path = !string.IsNullOrEmpty(copyName) ? destination.Value.TrimEnd('/') + '/' + copyName : destination.Value;
            var copyRequest = new CopyFileRequest() { From = source.Value, Path = path };
            var link = await context.Client.Commands.CopyAsync(copyRequest, CancellationToken.None);
            if (!await OperationProgress(context, link))
                throw new ApplicationException(string.Format(CultureInfo.CurrentCulture, Resources.OperationFailed, nameof(YandexDisk.Client.Clients.ICommandsClient.CopyAsync)));
            var request = new ResourceRequest() { Path = path };
            var item = await context.Client.MetaInfo.GetInfoAsync(request, CancellationToken.None);

            return item.ToFileSystemInfoContract();
        }

        public async Task<FileSystemInfoContract> MoveItemAsync(RootName root, FileSystemId source, string moveName, DirectoryId destination, Func<FileSystemInfoLocator> locatorResolver)
        {
            var context = await RequireContext(root);

            var path = !string.IsNullOrEmpty(moveName) ? destination.Value.TrimEnd('/') + '/' + moveName : destination.Value;
            var moveRequest = new MoveFileRequest() { From = source.Value, Path = path };
            var link = await context.Client.Commands.MoveAsync(moveRequest, CancellationToken.None);
            if (!await OperationProgress(context, link))
                throw new ApplicationException(string.Format(CultureInfo.CurrentCulture, Resources.OperationFailed, nameof(YandexDisk.Client.Clients.ICommandsClient.MoveAsync)));
            var request = new ResourceRequest() { Path = path };
            var item = await context.Client.MetaInfo.GetInfoAsync(request, CancellationToken.None);

            return item.ToFileSystemInfoContract();
        }

        public async Task<DirectoryInfoContract> NewDirectoryItemAsync(RootName root, DirectoryId parent, string name)
        {
            var context = await RequireContext(root);

            var request = new ResourceRequest() { Path = parent.Value.TrimEnd('/') + '/' + name };
            var link = await context.Client.Commands.CreateDictionaryAsync(request.Path, CancellationToken.None);
            if (!await OperationProgress(context, link))
                throw new ApplicationException(string.Format(CultureInfo.CurrentCulture, Resources.OperationFailed, nameof(ICommandsClient.CreateDictionaryAsync)));
            var item = await context.Client.MetaInfo.GetInfoAsync(request, CancellationToken.None);

            return new DirectoryInfoContract(item.Path, item.Name, item.Created, item.Modified);
        }

        public async Task<FileInfoContract> NewFileItemAsync(RootName root, DirectoryId parent, string name, Stream content, IProgress<ProgressValue> progress)
        {
            var context = await RequireContext(root);

            var request = new ResourceRequest() { Path = parent.Value.TrimEnd('/') + '/' + name };
            var link = await context.Client.Files.GetUploadLinkAsync(request.Path, false, CancellationToken.None);
            await context.Client.Files.UploadAsync(link, content, CancellationToken.None);
            var item = await context.Client.MetaInfo.GetInfoAsync(request, CancellationToken.None);

            return new FileInfoContract(item.Path, item.Name, item.Created, item.Modified, item.Size, item.Md5);
        }

        public async Task<bool> RemoveItemAsync(RootName root, FileSystemId target, bool recurse)
        {
            var context = await RequireContext(root);

            var request = new DeleteFileRequest() { Path = target.Value };
            var link = await context.Client.Commands.DeleteAsync(request, CancellationToken.None);
            if (!await OperationProgress(context, link))
                throw new ApplicationException(string.Format(CultureInfo.CurrentCulture, Resources.OperationFailed, nameof(ICommandsClient.DeleteAsync)));

            return true;
        }

        public async Task<FileSystemInfoContract> RenameItemAsync(RootName root, FileSystemId target, string newName, Func<FileSystemInfoLocator> locatorResolver)
        {
            var context = await RequireContext(root);

            var indexOfName = target.Value.LastIndexOf('/');
            var path = target.Value.Substring(0, indexOfName + 1) + newName;
            var moveRequest = new MoveFileRequest() { From = target.Value, Path = path };
            var link = await context.Client.Commands.MoveAsync(moveRequest, CancellationToken.None);
            if (!await OperationProgress(context, link))
                throw new ApplicationException(string.Format(CultureInfo.CurrentCulture, Resources.OperationFailed, nameof(ICommandsClient.MoveAsync)));
            var request = new ResourceRequest() { Path = path };
            var item = await context.Client.MetaInfo.GetInfoAsync(request, CancellationToken.None);

            return item.ToFileSystemInfoContract();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private static string DebuggerDisplay() => nameof(YandexGateway);
    }
}
