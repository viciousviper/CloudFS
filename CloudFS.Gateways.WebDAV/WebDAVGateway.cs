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
using System.IO;
using System.Collections.Generic;
using System.Composition;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using WebDav;
using IgorSoft.CloudFS.Interface;
using IgorSoft.CloudFS.Interface.Composition;
using IgorSoft.CloudFS.Interface.IO;
using IgorSoft.CloudFS.Gateways.WebDAV.Auth;
using System.Net;
using System.Xml.Linq;

namespace IgorSoft.CloudFS.Gateways.WebDAV
{
    [ExportAsAsyncCloudGateway("WebDAV")]
    [ExportMetadata(nameof(CloudGatewayMetadata.CloudService), WebDAVGateway.SCHEMA)]
    [ExportMetadata(nameof(CloudGatewayMetadata.Capabilities), WebDAVGateway.CAPABILITIES)]
    [ExportMetadata(nameof(CloudGatewayMetadata.ApiAssembly), WebDAVGateway.API)]
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public class WebDAVGateway : IAsyncCloudGateway
    {
        private const string SCHEMA = "webdav";

        private const GatewayCapabilities CAPABILITIES = GatewayCapabilities.All ^ GatewayCapabilities.ItemId;

        private const string API = "WebDAVClient";

        private const string davNamespace = "DAV:";

        private static readonly XName availableSpaceProperty = XName.Get("quota-available-bytes", davNamespace);

        private static readonly XName usedSpaceProperty = XName.Get("quota-used-bytes", davNamespace);

        public const string PARAMETER_BASEADDRESS = "baseAddress";

        private class WebDAVContext
        {
            private string pathPrefix;

            public WebDavClient Client { get; }

            public WebDAVContext(WebDavClient client, string pathPrefix = "")
            {
                Client = client;
                this.pathPrefix = pathPrefix;
            }

            public string AppendPrefix(string uri)
            {
                return pathPrefix + uri;
            }

            public string RemovePrefix(string uri)
            {
                if (!uri.StartsWith(pathPrefix))
                    throw new InvalidOperationException();

                return uri.Substring(pathPrefix.Length);
            }
        }

        private readonly IDictionary<RootName, WebDAVContext> contextCache = new Dictionary<RootName, WebDAVContext>();

        private async Task<WebDAVContext> RequireContextAsync(RootName root, string apiKey = null, Uri baseAddress = null)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            var result = default(WebDAVContext);
            if (!contextCache.TryGetValue(root, out result)) {
                var client = await Authenticator.LoginAsync(root.UserName, apiKey, baseAddress);
                contextCache.Add(root, result = new WebDAVContext(client, baseAddress.AbsolutePath.TrimEnd('/')));
            }
            return result;
        }

        public async Task<bool> TryAuthenticateAsync(RootName root, string apiKey, IDictionary<string, string> parameters)
        {
            var baseAddress = default(string);
            if (parameters?.TryGetValue(PARAMETER_BASEADDRESS, out baseAddress) != true)
                throw new ArgumentException($"Required {PARAMETER_BASEADDRESS} missing in {nameof(parameters)}".ToString(CultureInfo.CurrentCulture));

            try {
                var context = await RequireContextAsync(root, apiKey, new Uri(baseAddress));
                return true;
            } catch (Exception) {
                return false;
            }
        }

        private void CheckSuccess(WebDavResponse response, string operation, params string[] parameters)
        {
            if (!response.IsSuccessful)
                throw new WebException($"WebDAV operation {operation}({string.Join(", ", parameters)}) failed with status {response.StatusCode}");
        }

        public async Task<DriveInfoContract> GetDriveAsync(RootName root, string apiKey, IDictionary<string, string> parameters)
        {
            var baseAddress = default(string);
            if (parameters?.TryGetValue(PARAMETER_BASEADDRESS, out baseAddress) != true)
                throw new ArgumentException($"Required {PARAMETER_BASEADDRESS} missing in {nameof(parameters)}".ToString(CultureInfo.CurrentCulture));

            var context = await RequireContextAsync(root, apiKey, new Uri(baseAddress));

            var propfindResponse = await context.Client.Propfind(context.AppendPrefix("/"));
            CheckSuccess(propfindResponse, nameof(WebDavClient.Propfind), "/");

            var item = propfindResponse.Resources.Single(r => context.RemovePrefix(r.Uri) == "/");

            var availableSpaceValue = item.Properties.SingleOrDefault(p => p.Name == availableSpaceProperty)?.Value;
            var usedSpaceValue = item.Properties.SingleOrDefault(p => p.Name == usedSpaceProperty)?.Value;

            return new DriveInfoContract(root.Value, !string.IsNullOrEmpty(availableSpaceValue) ? long.Parse(availableSpaceValue) : default(long?), !string.IsNullOrEmpty(usedSpaceValue) ? long.Parse(usedSpaceValue) : default(long?));
        }

        public async Task<RootDirectoryInfoContract> GetRootAsync(RootName root, string apiKey, IDictionary<string, string> parameters)
        {
            var baseAddress = default(string);
            if (parameters?.TryGetValue(PARAMETER_BASEADDRESS, out baseAddress) != true)
                throw new ArgumentException($"Required {PARAMETER_BASEADDRESS} missing in {nameof(parameters)}".ToString(CultureInfo.CurrentCulture));

            var context = await RequireContextAsync(root, apiKey, new Uri(baseAddress));

            var propfindResponse = await context.Client.Propfind(context.AppendPrefix("/"));
            CheckSuccess(propfindResponse, nameof(WebDavClient.Propfind), "/");

            var item = propfindResponse.Resources.Single(r => context.RemovePrefix(r.Uri) == "/");

            return new RootDirectoryInfoContract(item.Uri, item.CreationDate ?? DateTimeOffset.FromFileTime(0), item.LastModifiedDate ?? DateTimeOffset.FromFileTime(0));
        }

        public async Task<IEnumerable<FileSystemInfoContract>> GetChildItemAsync(RootName root, DirectoryId parent)
        {
            var context = await RequireContextAsync(root);

            var propfindResponse = await context.Client.Propfind(parent.Value);
            CheckSuccess(propfindResponse, nameof(WebDavClient.Propfind), parent.Value);

            var items = propfindResponse.Resources.Where(r => r.Uri != parent.Value);

            return items.Select(i => i.ToFileSystemInfoContract());
        }

        public async Task<bool> ClearContentAsync(RootName root, FileId target, Func<FileSystemInfoLocator> locatorResolver)
        {
            var context = await RequireContextAsync(root);

            var putFileResponse = await context.Client.PutFile(target.Value, Stream.Null);

            return putFileResponse.IsSuccessful;
        }

        public async Task<Stream> GetContentAsync(RootName root, FileId source)
        {
            var context = await RequireContextAsync(root);

            var getRawFileResponse = await context.Client.GetRawFile(source.Value);
            CheckSuccess(getRawFileResponse, nameof(WebDavClient.GetRawFile), source.Value);

            return getRawFileResponse.Stream;
        }

        public async Task<bool> SetContentAsync(RootName root, FileId target, Stream content, IProgress<ProgressValue> progress, Func<FileSystemInfoLocator> locatorResolver)
        {
            var context = await RequireContextAsync(root);

            var putFileResponse = await context.Client.PutFile(target.Value, content);

            return putFileResponse.IsSuccessful;
        }

        public async Task<FileSystemInfoContract> CopyItemAsync(RootName root, FileSystemId source, string copyName, DirectoryId destination, bool recurse)
        {
            var context = await RequireContextAsync(root);

            if (string.IsNullOrEmpty(copyName)) {
                var lastSlashIndex = source.Value.TrimEnd('/').LastIndexOf('/');
                copyName = source.Value.Substring(lastSlashIndex + 1);
            } else if (source.Value.EndsWith("/") && !copyName.EndsWith("/")) {
                copyName += "/";
            }

            var targetName = destination.Value + copyName;

            var copyResponse = await context.Client.Copy(source.Value, targetName);
            CheckSuccess(copyResponse, nameof(WebDavClient.Copy), source.Value, targetName);

            var propfindResponse = await context.Client.Propfind(targetName);
            CheckSuccess(propfindResponse, nameof(WebDavClient.Propfind), targetName);

            var item = propfindResponse.Resources.Single(r => r.Uri == targetName);

            return item.ToFileSystemInfoContract();
        }

        public async Task<FileSystemInfoContract> MoveItemAsync(RootName root, FileSystemId source, string moveName, DirectoryId destination, Func<FileSystemInfoLocator> locatorResolver)
        {
            var context = await RequireContextAsync(root);

            if (string.IsNullOrEmpty(moveName)) {
                var lastSlashIndex = source.Value.TrimEnd('/').LastIndexOf('/');
                moveName = source.Value.Substring(lastSlashIndex + 1);
            } else if (source.Value.EndsWith("/") && !moveName.EndsWith("/")) {
                moveName += "/";
            }

            var targetName = destination.Value + moveName;

            var moveResponse = await context.Client.Move(source.Value, targetName);
            CheckSuccess(moveResponse, nameof(WebDavClient.Move), source.Value, targetName);

            var propfindResponse = await context.Client.Propfind(targetName);
            CheckSuccess(propfindResponse, nameof(WebDavClient.Propfind), targetName);

            var item = propfindResponse.Resources.Single(r => r.Uri == targetName);

            return item.ToFileSystemInfoContract();
        }

        public async Task<DirectoryInfoContract> NewDirectoryItemAsync(RootName root, DirectoryId parent, string name)
        {
            var context = await RequireContextAsync(root);

            var path = parent.Value + name + "/";

            var mkColResponse = await context.Client.Mkcol(path);
            CheckSuccess(mkColResponse, nameof(WebDavClient.Mkcol), path);

            var propFindResponse = await context.Client.Propfind(path);
            CheckSuccess(propFindResponse, nameof(WebDavClient.Propfind), path);

            var item = propFindResponse.Resources.Single(r => r.Uri == path);

            return new DirectoryInfoContract(path, item.GetName(), item.CreationDate ?? DateTimeOffset.FromFileTime(0), item.LastModifiedDate ?? DateTimeOffset.FromFileTime(0));
        }

        public async Task<FileInfoContract> NewFileItemAsync(RootName root, DirectoryId parent, string name, Stream content, IProgress<ProgressValue> progress)
        {
            var context = await RequireContextAsync(root);

            var path = parent.Value + name;

            var putFileResponse = await context.Client.PutFile(path, content);
            CheckSuccess(putFileResponse, nameof(WebDavClient.PutFile), path);

            var propFindResponse = await context.Client.Propfind(path);
            CheckSuccess(propFindResponse, nameof(WebDavClient.Propfind), path);

            var item = propFindResponse.Resources.Single(r => r.Uri == path);

            return new FileInfoContract(path, item.GetName(), item.CreationDate ?? DateTimeOffset.FromFileTime(0), item.LastModifiedDate ?? DateTimeOffset.FromFileTime(0), item.ContentLength ?? -1, item.ETag);
        }

        public async Task<bool> RemoveItemAsync(RootName root, FileSystemId target, bool recurse)
        {
            var context = await RequireContextAsync(root);

            var deleteResponse = await context.Client.Delete(target.Value);

            return deleteResponse.IsSuccessful;
        }

        public async Task<FileSystemInfoContract> RenameItemAsync(RootName root, FileSystemId target, string newName, Func<FileSystemInfoLocator> locatorResolver)
        {
            if (string.IsNullOrEmpty(newName))
                throw new ArgumentNullException(nameof(newName));

            var context = await RequireContextAsync(root);

            var lastSlashIndex = target.Value.TrimEnd('/').LastIndexOf('/');
            newName = target.Value.Substring(0, lastSlashIndex + 1) + newName + (target.Value.EndsWith("/") ? "/" : string.Empty);

            var moveResponse = await context.Client.Move(target.Value, newName);
            CheckSuccess(moveResponse, nameof(WebDavClient.Move), target.Value, newName);

            var propfindResponse = await context.Client.Propfind(newName);
            CheckSuccess(propfindResponse, nameof(WebDavClient.Propfind), newName);

            var item = propfindResponse.Resources.Single(r => r.Uri == newName);

            return item.ToFileSystemInfoContract();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private string DebuggerDisplay() => $"{nameof(WebDAVGateway)} rootPath=''".ToString(CultureInfo.CurrentCulture);
    }
}
