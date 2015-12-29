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
using CG.Web.MegaApiClient;
using IgorSoft.CloudFS.Interface;
using IgorSoft.CloudFS.Interface.Composition;
using IgorSoft.CloudFS.Interface.IO;
using IgorSoft.CloudFS.Gateways.Mega.Auth;

namespace IgorSoft.CloudFS.Gateways.Mega
{
    [ExportAsCloudGateway("Mega")]
    [ExportMetadata(nameof(CloudGatewayMetadata.CloudService), MegaGateway.SCHEMA)]
    [ExportMetadata(nameof(CloudGatewayMetadata.ServiceUri), MegaGateway.URL)]
    [ExportMetadata(nameof(CloudGatewayMetadata.ApiAssembly), MegaGateway.API)]
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    public sealed class MegaGateway : ICloudGateway
    {
        private const string SCHEMA = "mega";

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

        private IDictionary<RootName, MegaContext> contextCache = new Dictionary<RootName, MegaContext>();

        private MegaContext RequireContext(RootName root, string apiKey = null)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            var result = default(MegaContext);
            if (!contextCache.TryGetValue(root, out result)) {
                var client = Authenticator.Login(root.UserName, apiKey);
                contextCache.Add(root, result = new MegaContext(client));
            }
            return result;
        }

        public DriveInfoContract GetDrive(RootName root, string apiKey)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            return new DriveInfoContract(root.Value, null, null);
        }

        public RootDirectoryInfoContract GetRoot(RootName root, string apiKey)
        {
            var context = RequireContext(root, apiKey);

            var nodes = context.Client.GetNodes();
            var item = nodes.Single(n => n.Type == NodeType.Root);

            return new RootDirectoryInfoContract(item.Id, DateTimeOffset.MinValue, item.LastModificationDate);
        }

        public IEnumerable<FileSystemInfoContract> GetChildItem(RootName root, DirectoryId parent)
        {
            var context = RequireContext(root);

            var nodes = context.Client.GetNodes();
            var parentItem = nodes.Single(n => n.Id == parent.Value);
            var items = context.Client.GetNodes(parentItem);

            return items.Select(i => i.ToFileSystemInfoContract());
        }

        public void ClearContent(RootName root, FileId target)
        {
            throw new NotSupportedException(Resources.SettingOfFileContentNotSupported);
        }

        public Stream GetContent(RootName root, FileId source)
        {
            var context = RequireContext(root);

            var nodes = context.Client.GetNodes();
            var item = nodes.Single(n => n.Id == source.Value);
            var stream = context.Client.Download(item);

            return stream;
        }

        public void SetContent(RootName root, FileId target, Stream content, IProgress<ProgressValue> progress)
        {
            throw new NotSupportedException(Resources.SettingOfFileContentNotSupported);
        }

        public FileSystemInfoContract CopyItem(RootName root, FileSystemId source, string copyName, DirectoryId destination, bool recurse)
        {
            throw new NotSupportedException(Resources.CopyingOfFilesNotSupported);
        }

        public FileSystemInfoContract MoveItem(RootName root, FileSystemId source, string moveName, DirectoryId destination)
        {

            var context = RequireContext(root);

            var nodes = context.Client.GetNodes();
            var sourceItem = nodes.Single(n => n.Id == source.Value);
            if (!string.IsNullOrEmpty(moveName) && moveName != sourceItem.Name)
                throw new NotSupportedException(Resources.RenamingOfFilesNotSupported);
            var destinationParentItem = nodes.Single(n => n.Id == destination.Value);
            var item = context.Client.Move(sourceItem, destinationParentItem);

            return item.ToFileSystemInfoContract();
        }

        public DirectoryInfoContract NewDirectoryItem(RootName root, DirectoryId parent, string name)
        {
            var context = RequireContext(root);

            var nodes = context.Client.GetNodes();
            var parentItem = nodes.Single(n => n.Id == parent.Value);
            var item = context.Client.CreateFolder(name, parentItem);

            return new DirectoryInfoContract(item.Id, item.Name, DateTimeOffset.MinValue, item.LastModificationDate);
        }

        public FileInfoContract NewFileItem(RootName root, DirectoryId parent, string name, Stream content, IProgress<ProgressValue> progress)
        {
            var context = RequireContext(root);

            var nodes = context.Client.GetNodes();
            var parentItem = nodes.Single(n => n.Id == parent.Value);
            var item = context.Client.Upload(new ProgressStream(content, progress), name, parentItem);

            return new FileInfoContract(item.Id, item.Name, DateTimeOffset.MinValue, item.LastModificationDate, item.Size, null);
        }

        public void RemoveItem(RootName root, FileSystemId target, bool recurse)
        {
            var context = RequireContext(root);

            var nodes = context.Client.GetNodes();
            var item = nodes.Single(n => n.Id == target.Value);
            context.Client.Delete(item);
        }

        public FileSystemInfoContract RenameItem(RootName root, FileSystemId target, string newName)
        {
            throw new NotSupportedException(Resources.RenamingOfFilesNotSupported);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private static string DebuggerDisplay => nameof(MegaGateway);
    }
}
