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
using System.Threading.Tasks;
using SwiftClient;
using IgorSoft.CloudFS.Gateways.hubiC.OAuth;
using IgorSoft.CloudFS.Interface;
using IgorSoft.CloudFS.Interface.Composition;
using IgorSoft.CloudFS.Interface.IO;

namespace IgorSoft.CloudFS.Gateways.hubiC
{
    [ExportAsAsyncCloudGateway("Yandex")]
    [ExportMetadata(nameof(CloudGatewayMetadata.CloudService), hubiCGateway.SCHEMA)]
    [ExportMetadata(nameof(CloudGatewayMetadata.Capabilities), hubiCGateway.CAPABILITIES)]
    [ExportMetadata(nameof(CloudGatewayMetadata.ServiceUri), hubiCGateway.URL)]
    [ExportMetadata(nameof(CloudGatewayMetadata.ApiAssembly), nameof(hubiCGateway))]
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public sealed class hubiCGateway : IAsyncCloudGateway
    {
        private const string SCHEMA = "hubic";

        private const GatewayCapabilities CAPABILITIES = GatewayCapabilities.All ^ GatewayCapabilities.CopyDirectoryItem ^ GatewayCapabilities.MoveItems ^ GatewayCapabilities.RenameItems ^ GatewayCapabilities.ItemId;

        private const string URL = "https://hubic.com";

        private const int RETRIES = 3;

        private const string PARAMETER_CONTAINER = "container";

        private const string DEFAULT_CONTAINER = "default";

        private static readonly Dictionary<string, string> directoryContentHeaders = new Dictionary<string, string>() { { "Content-Type", "application/directory" } };

        private class hubiCContext
        {
            public Client Client { get; }

            public string Container { get; }

            public hubiCContext(Client client, string container)
            {
                Client = client;
                Container = container;
            }
        }

        private readonly IDictionary<RootName, hubiCContext> contextCache = new Dictionary<RootName, hubiCContext>();

        private async Task<hubiCContext> RequireContextAsync(RootName root, string apiKey = null, string container = DEFAULT_CONTAINER)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            var result = default(hubiCContext);
            if (!contextCache.TryGetValue(root, out result)) {
                var client = await OAuthAuthenticator.LoginAsync(root.UserName, apiKey);
                contextCache.Add(root, result = new hubiCContext(client, container));
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
            var container = default(string);
            parameters?.TryGetValue(PARAMETER_CONTAINER, out container);

            var context = await RequireContextAsync(root, apiKey, container);

            var item = await context.Client.GetAccount();

            var totalSpace = long.Parse(item.Headers[string.Format(CultureInfo.InvariantCulture, SwiftHeaderKeys.AccountMetaFormat, "Quota")]);
            var usedSpace = long.Parse(item.Headers[SwiftHeaderKeys.AccountBytesUsed]);
            return new DriveInfoContract(root.Value, totalSpace - usedSpace, usedSpace);
        }

        public async Task<RootDirectoryInfoContract> GetRootAsync(RootName root, string apiKey, IDictionary<string, string> parameters)
        {
            var context = await RequireContextAsync(root, apiKey);

            var accessToken = context.Client.RetryManager.AuthManager.Credentials.Password;
            var item = hubiCInfo.QueryData<hubiCAccount>(hubiCInfo.AccountUri, accessToken);

            return new RootDirectoryInfoContract("/", item.CreationDate, item.CreationDate);
        }

        public async Task<IEnumerable<FileSystemInfoContract>> GetChildItemAsync(RootName root, DirectoryId parent)
        {
            var context = await RequireContextAsync(root);

            var queryParameters = new Dictionary<string, string>();
            if (parent.Value != "/")
                queryParameters.Add("path", parent.Value);
            else
                queryParameters.Add("delimiter", "/");

            var item = await context.Client.GetContainer(context.Container, queryParams: queryParameters);

            return item.Objects.Where(i => !string.IsNullOrEmpty(i.ContentType)).Select(i => i.ToFileSystemInfoContract(parent));
        }

        public async Task<bool> ClearContentAsync(RootName root, FileId target, Func<FileSystemInfoLocator> locatorResolver)
        {
            var context = await RequireContextAsync(root);

            var response = await context.Client.PutObject(context.Container, target.Value, Array.Empty<byte>());

            return response.IsSuccess;
        }

        public async Task<Stream> GetContentAsync(RootName root, FileId source)
        {
            var context = await RequireContextAsync(root);

            var item = await context.Client.GetObject(context.Container, source.Value);

            return item.Stream;
        }

        public async Task<bool> SetContentAsync(RootName root, FileId target, Stream content, IProgress<ProgressValue> progress, Func<FileSystemInfoLocator> locatorResolver)
        {
            var context = await RequireContextAsync(root);

            var stream = progress != null ? new ProgressStream(content, progress) : content;
            var item = await context.Client.PutObject(context.Container, target.Value, stream);

            return item.IsSuccess;
        }

        public async Task<FileSystemInfoContract> CopyItemAsync(RootName root, FileSystemId source, string copyName, DirectoryId destination, bool recurse)
        {
            var context = await RequireContextAsync(root);

            if (source is DirectoryId)
                throw new NotSupportedException(Resources.CopyingOfDirectoriesNotSupported);

            var targetName = !string.IsNullOrEmpty(copyName) ? copyName : source.GetName();
            var targetId = destination.GetObjectId(targetName);

            await context.Client.CopyObject(context.Container, source.Value, context.Container, targetId);
            var item = await context.Client.HeadObject(context.Container, targetId);

            var creationTime = DateTime.Parse(item.Headers["Date"]);
            return new FileInfoContract(targetId, targetName, creationTime, creationTime, item.ContentLength, item.Headers["ETag"]);
        }

        public Task<FileSystemInfoContract> MoveItemAsync(RootName root, FileSystemId source, string moveName, DirectoryId destination, Func<FileSystemInfoLocator> locatorResolver)
        {
            return Task.FromException<FileSystemInfoContract>(new NotSupportedException(Resources.MovingOfFilesNotSupported));
        }

        public async Task<DirectoryInfoContract> NewDirectoryItemAsync(RootName root, DirectoryId parent, string name)
        {
            var context = await RequireContextAsync(root);

            var objectId = parent.GetObjectId(name);

            var item = await context.Client.PutPseudoDirectory(context.Container, objectId);
            if (!item.IsSuccess)
                throw new ApplicationException(item.Reason);

            var creationTime = DateTime.Parse(item.Headers["Date"]);
            return new DirectoryInfoContract(objectId, name, creationTime, creationTime);
        }

        public async Task<FileInfoContract> NewFileItemAsync(RootName root, DirectoryId parent, string name, Stream content, IProgress<ProgressValue> progress)
        {
            if (content.Length == 0)
                return new ProxyFileInfoContract(name);

            var context = await RequireContextAsync(root);

            var objectId = parent.GetObjectId(name);
            var length = content.Length;

            var stream = progress != null ? new ProgressStream(content, progress) : content;
            var item = await context.Client.PutObject(context.Container, objectId, stream);
            if (!item.IsSuccess)
                throw new ApplicationException(item.Reason);

            var creationTime = DateTime.Parse(item.Headers["Date"]);
            return new FileInfoContract(objectId, name, creationTime, creationTime, length, item.Headers["ETag"]);
        }

        public async Task<bool> RemoveItemAsync(RootName root, FileSystemId target, bool recurse)
        {
            var context = await RequireContextAsync(root);

            var response = default(SwiftResponse);
            if (target is DirectoryId && recurse) {
                var directoryId = target.Value.TrimEnd('/');
                var queryParameters = new Dictionary<string, string>() {
                    { "marker", directoryId },
                    { "end_marker", directoryId + '0' }
                };

                var container = await context.Client.GetContainer(context.Container, queryParams: queryParameters);

                response = await context.Client.DeleteObjects(context.Container, container.Objects.Select(i => i.Object).Concat(new[] { directoryId }));
            } else {
                response = await context.Client.DeleteObject(context.Container, target.Value.TrimEnd('/'));
            }

            return response.IsSuccess;
        }

        public Task<FileSystemInfoContract> RenameItemAsync(RootName root, FileSystemId target, string newName, Func<FileSystemInfoLocator> locatorResolver)
        {
            return Task.FromException<FileSystemInfoContract>(new NotSupportedException(Resources.RenamingOfFilesNotSupported));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private static string DebuggerDisplay() => nameof(hubiCGateway);
    }
}
