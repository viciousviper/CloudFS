/*
The MIT License(MIT)

Copyright(c) 2017 IgorSoft

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
using System.Composition.Hosting;
using System.IO;
using System.Threading.Tasks;
using IgorSoft.CloudFS.Interface;
using IgorSoft.CloudFS.Interface.Composition;
using IgorSoft.CloudFS.Interface.IO;

namespace IgorSoft.CloudFS.InterfaceTests.Composition
{
    public partial class AttributeBasedCompositionTests
    {
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public abstract class AsyncCloudGatewayBase : IAsyncCloudGateway
        {
            public Task<bool> ClearContentAsync(RootName root, FileId target, Func<FileSystemInfoLocator> locatorResolver)
            {
                throw new NotImplementedException();
            }

            public Task<FileSystemInfoContract> CopyItemAsync(RootName root, FileSystemId source, string copyName, DirectoryId destination, bool recurse)
            {
                throw new NotImplementedException();
            }

            public Task<IEnumerable<FileSystemInfoContract>> GetChildItemAsync(RootName root, DirectoryId parent)
            {
                throw new NotImplementedException();
            }

            public Task<Stream> GetContentAsync(RootName root, FileId source)
            {
                throw new NotImplementedException();
            }

            public Task<DriveInfoContract> GetDriveAsync(RootName root, string apiKey, IDictionary<string, string> parameters)
            {
                throw new NotImplementedException();
            }

            public Task<RootDirectoryInfoContract> GetRootAsync(RootName root, string apiKey, IDictionary<string, string> parameters)
            {
                throw new NotImplementedException();
            }

            public Task<FileSystemInfoContract> MoveItemAsync(RootName root, FileSystemId source, string moveName, DirectoryId destination, Func<FileSystemInfoLocator> locatorResolver)
            {
                throw new NotImplementedException();
            }

            public Task<DirectoryInfoContract> NewDirectoryItemAsync(RootName root, DirectoryId parent, string name)
            {
                throw new NotImplementedException();
            }

            public Task<FileInfoContract> NewFileItemAsync(RootName root, DirectoryId parent, string name, Stream content, IProgress<ProgressValue> progress)
            {
                throw new NotImplementedException();
            }

            public Task<bool> RemoveItemAsync(RootName root, FileSystemId target, bool recurse)
            {
                throw new NotImplementedException();
            }

            public Task<FileSystemInfoContract> RenameItemAsync(RootName root, FileSystemId target, string newName, Func<FileSystemInfoLocator> locatorResolver)
            {
                throw new NotImplementedException();
            }

            public Task<bool> SetContentAsync(RootName root, FileId target, Stream content, IProgress<ProgressValue> progress, Func<FileSystemInfoLocator> locatorResolver)
            {
                throw new NotImplementedException();
            }

            public Task<bool> TryAuthenticateAsync(RootName root, string apiKey, IDictionary<string, string> parameters)
            {
                throw new NotImplementedException();
            }
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public abstract class CloudGatewayBase : ICloudGateway
        {
            public void ClearContent(RootName root, FileId target)
            {
                throw new NotImplementedException();
            }

            public FileSystemInfoContract CopyItem(RootName root, FileSystemId source, string copyName, DirectoryId destination, bool recurse)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<FileSystemInfoContract> GetChildItem(RootName root, DirectoryId parent)
            {
                throw new NotImplementedException();
            }

            public Stream GetContent(RootName root, FileId source)
            {
                throw new NotImplementedException();
            }

            public DriveInfoContract GetDrive(RootName root, string apiKey, IDictionary<string, string> parameters)
            {
                throw new NotImplementedException();
            }

            public RootDirectoryInfoContract GetRoot(RootName root, string apiKey, IDictionary<string, string> parameters)
            {
                throw new NotImplementedException();
            }

            public FileSystemInfoContract MoveItem(RootName root, FileSystemId source, string moveName, DirectoryId destination)
            {
                throw new NotImplementedException();
            }

            public DirectoryInfoContract NewDirectoryItem(RootName root, DirectoryId parent, string name)
            {
                throw new NotImplementedException();
            }

            public FileInfoContract NewFileItem(RootName root, DirectoryId parent, string name, Stream content, IProgress<ProgressValue> progress)
            {
                throw new NotImplementedException();
            }

            public void RemoveItem(RootName root, FileSystemId target, bool recurse)
            {
                throw new NotImplementedException();
            }

            public FileSystemInfoContract RenameItem(RootName root, FileSystemId target, string newName)
            {
                throw new NotImplementedException();
            }

            public void SetContent(RootName root, FileId target, Stream content, IProgress<ProgressValue> progress)
            {
                throw new NotImplementedException();
            }

            public bool TryAuthenticate(RootName root, string apiKey, IDictionary<string, string> parameters)
            {
                throw new NotImplementedException();
            }
        }

        [ExportAsAsyncCloudGateway(nameof(TestAsyncCloudGateway))]
        [ExportMetadata(nameof(CloudGatewayMetadata.CloudService), SCHEMA)]
        [ExportMetadata(nameof(CloudGatewayMetadata.Capabilities), CAPABILITIES)]
        [ExportMetadata(nameof(CloudGatewayMetadata.ServiceUri), URL)]
        [ExportMetadata(nameof(CloudGatewayMetadata.ApiAssembly), API)]
        public sealed class TestAsyncCloudGateway : AsyncCloudGatewayBase
        {
            public const string SCHEMA = "AsyncSchema";
            public const GatewayCapabilities CAPABILITIES = GatewayCapabilities.GetContent;
            public const string URL = "https://async.org/";
            public const string API = "IgorSoft.CloudFS.Interface";
        }

        [ExportAsCloudGateway(nameof(TestCloudGateway))]
        [ExportMetadata(nameof(CloudGatewayMetadata.CloudService), SCHEMA)]
        [ExportMetadata(nameof(CloudGatewayMetadata.Capabilities), CAPABILITIES)]
        [ExportMetadata(nameof(CloudGatewayMetadata.ServiceUri), URL)]
        [ExportMetadata(nameof(CloudGatewayMetadata.ApiAssembly), API)]
        public sealed class TestCloudGateway : CloudGatewayBase
        {
            public const string SCHEMA = "Schema";
            public const GatewayCapabilities CAPABILITIES = GatewayCapabilities.SetContent;
            public const string URL = "https://sync.org/";
            public const string API = "IgorSoft.CloudFS.Interface";
        }

        public sealed class NonExportingAsyncCloudGateway : AsyncCloudGatewayBase
        {
        }

        public sealed class NonExportingCloudGateway : CloudGatewayBase
        {
        }

        public sealed class ImportConsumer
        {
            [ImportMany]
            public IList<ExportFactory<IAsyncCloudGateway, CloudGatewayMetadata>> AsyncGateways { get; set; }

            [ImportMany]
            public IList<ExportFactory<ICloudGateway, CloudGatewayMetadata>> Gateways { get; set; }
        }

        private sealed class Fixture
        {
            private CompositionHost host;

            public void RegisterExportsInTypes(params Type[] types)
            {
                var configuration = new ContainerConfiguration().WithParts(types);
                host = configuration.CreateContainer();
            }

            public void SatisfyImports(object importConsumer)
            {
                host.SatisfyImports(importConsumer);
            }
        }
    }
}
