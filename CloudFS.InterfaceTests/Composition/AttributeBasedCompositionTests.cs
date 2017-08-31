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
using IgorSoft.CloudFS.Interface.Composition;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IgorSoft.CloudFS.InterfaceTests.Composition
{
    [TestClass]
    public partial class AttributeBasedCompositionTests
    {
        private Fixture fixture;

        [TestInitialize]
        public void Initialize()
        {
            fixture = new Fixture();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void New_ExportAsAsyncCloudGatewayAttribute_WhereNameIsEmpty_Throws()
        {
            var sut = new ExportAsAsyncCloudGatewayAttribute(string.Empty);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void New_ExportAsCloudGatewayAttribute_WhereNameIsEmpty_Throws()
        {
            var sut = new ExportAsCloudGatewayAttribute(string.Empty);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void New_CloudGatewayMetadata_WhereValuesAreNull_Throws()
        {
            var sut = new CloudGatewayMetadata(default(IDictionary<string, object>));
        }

        [TestMethod]
        public void SatisfyImports_WithNoExportsProvided_ConsumesNoImports()
        {
            fixture.RegisterExportsInTypes(typeof(NonExportingAsyncCloudGateway), typeof(NonExportingAsyncCloudGateway));

            var sut = new ImportConsumer();

            fixture.SatisfyImports(sut);

            Assert.AreEqual(0, sut.AsyncGateways.Count);
            Assert.AreEqual(0, sut.Gateways.Count);
        }

        [TestMethod]
        public void SatisfyImports_WithAsyncExportsProvided_ConsumesAsyncImport()
        {
            fixture.RegisterExportsInTypes(typeof(TestAsyncCloudGateway), typeof(NonExportingCloudGateway));

            var sut = new ImportConsumer();

            fixture.SatisfyImports(sut);

            Assert.AreEqual(1, sut.AsyncGateways.Count);
            Assert.AreEqual(0, sut.Gateways.Count);
        }

        [TestMethod]
        public void SatisfyImports_WithSyncExportsProvided_ConsumesSyncImport()
        {
            fixture.RegisterExportsInTypes(typeof(NonExportingAsyncCloudGateway), typeof(TestCloudGateway));

            var sut = new ImportConsumer();

            fixture.SatisfyImports(sut);

            Assert.AreEqual(0, sut.AsyncGateways.Count);
            Assert.AreEqual(1, sut.Gateways.Count);
        }

        [TestMethod]
        public void SatisfyImports_WithBothExportsProvided_ConsumesBothImports()
        {
            fixture.RegisterExportsInTypes(typeof(TestAsyncCloudGateway), typeof(TestCloudGateway));

            var sut = new ImportConsumer();

            fixture.SatisfyImports(sut);

            Assert.AreEqual(1, sut.AsyncGateways.Count);
            Assert.AreEqual(1, sut.Gateways.Count);
        }

        [TestMethod]
        public void Import_ForAsyncGatewayExport_ReturnsMetadata()
        {
            fixture.RegisterExportsInTypes(typeof(TestAsyncCloudGateway), typeof(NonExportingCloudGateway));

            var sut = new ImportConsumer();

            fixture.SatisfyImports(sut);

            var metadata = sut.AsyncGateways[0].Metadata;
            Assert.AreEqual(TestAsyncCloudGateway.SCHEMA, metadata.CloudService);
            Assert.AreEqual(TestAsyncCloudGateway.CAPABILITIES, metadata.Capabilities);
            Assert.AreEqual(TestAsyncCloudGateway.URL, metadata.ServiceUri.AbsoluteUri);
            Assert.AreEqual(TestAsyncCloudGateway.API, metadata.ApiAssembly.Name);
        }

        [TestMethod]
        public void Import_ForSyncGatewayExport_ReturnsMetadata()
        {
            fixture.RegisterExportsInTypes(typeof(NonExportingAsyncCloudGateway), typeof(TestCloudGateway));

            var sut = new ImportConsumer();

            fixture.SatisfyImports(sut);

            var metadata = sut.Gateways[0].Metadata;
            Assert.AreEqual(TestCloudGateway.SCHEMA, metadata.CloudService);
            Assert.AreEqual(TestCloudGateway.CAPABILITIES, metadata.Capabilities);
            Assert.AreEqual(TestCloudGateway.URL, metadata.ServiceUri.AbsoluteUri);
            Assert.AreEqual(TestCloudGateway.API, metadata.ApiAssembly.Name);
        }
    }
}
