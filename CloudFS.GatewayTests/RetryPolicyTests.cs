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
using System.IO;
using System.Threading.Tasks;
using IgorSoft.CloudFS.Interface.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IgorSoft.CloudFS.GatewayTests
{
    [TestClass]
    public sealed partial class RetryPolicyTests
    {
        private const int FILE_SIZE = 100;

        private const int CHUNK_SIZE = 10;

        private Fixture fixture;

        private byte[] content;

        private byte[] result;

        [TestInitialize]
        public void Initialize()
        {
            fixture = new Fixture();

            content = fixture.GetContent(FILE_SIZE);
            result = new byte[FILE_SIZE];
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DownloadStraight_ViaMemoryStream_Succeeds()
        {
            fixture.SetupRemoteStreamInstances(content, FILE_SIZE);

            Action<MemoryStream> resetAction = default(Action<MemoryStream>);
            Func<MemoryStream, byte[], int, int, Task> writeAction = (stream, buffer, offset, count) => {
                stream.Write(buffer, offset, count);
                return Task.CompletedTask;
            };

            fixture.PerformDownload(result, resetAction, writeAction);

            CollectionAssert.AreEqual(content, result);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DownloadWithRetries_ViaMemoryStream_Succeeds()
        {
            fixture.SetupRemoteStreamInstances(content, FILE_SIZE / 10, FILE_SIZE / 5, FILE_SIZE / 2, FILE_SIZE);

            Action<MemoryStream> resetAction = stream => stream.Seek(0, SeekOrigin.Begin);
            Func<MemoryStream, byte[], int, int, Task> writeAction = (stream, buffer, offset, count) => {
                stream.Write(buffer, offset, count);
                return Task.CompletedTask;
            };

            fixture.PerformDownload(result, resetAction, writeAction);

            CollectionAssert.AreEqual(content, result);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DownloadAsyncStraight_ViaMemoryStream_Succeeds()
        {
            fixture.SetupRemoteStreamInstances(content, FILE_SIZE);

            Action<MemoryStream> resetAction = default(Action<MemoryStream>);
            Func<MemoryStream, byte[], int, int, Task> writeAction = (stream, buffer, offset, count) => stream.WriteAsync(buffer, offset, count);

            fixture.PerformDownload(result, resetAction, writeAction);

            CollectionAssert.AreEqual(content, result);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DownloadAsyncWithRetries_ViaMemoryStream_Succeeds()
        {
            fixture.SetupRemoteStreamInstances(content, FILE_SIZE / 10, FILE_SIZE / 5, FILE_SIZE / 2, FILE_SIZE);

            Action<MemoryStream> resetAction = stream => stream.Seek(0, SeekOrigin.Begin);
            Func<MemoryStream, byte[], int, int, Task> writeAction = (stream, buffer, offset, count) => stream.WriteAsync(buffer, offset, count);

            fixture.PerformDownload(result, resetAction, writeAction);

            CollectionAssert.AreEqual(content, result);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DownloadStraight_ViaProducerConsumerStream_Succeeds()
        {
            fixture.SetupRemoteStreamInstances(content, FILE_SIZE);

            Action<ProducerConsumerStream> resetAction = default(Action<ProducerConsumerStream>);
            Func<ProducerConsumerStream, byte[], int, int, Task> writeAction = (stream, buffer, offset, count) => {
                stream.Write(buffer, offset, count);
                return Task.CompletedTask;
            };

            Assert.IsTrue(fixture.PerformDownload(result, resetAction, writeAction), "Download timed out");

            CollectionAssert.AreEqual(content, result);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DownloadWithRetries_ViaProducerConsumerStream_Succeeds()
        {
            fixture.SetupRemoteStreamInstances(content, FILE_SIZE / 10, FILE_SIZE / 5, FILE_SIZE / 2, FILE_SIZE);

            Action<ProducerConsumerStream> resetAction = stream => stream.Reset();
            Func<ProducerConsumerStream, byte[], int, int, Task> writeAction = (stream, buffer, offset, count) => {
                stream.Write(buffer, offset, count);
                return Task.CompletedTask;
            };

            Assert.IsTrue(fixture.PerformDownload(result, resetAction, writeAction), "Download timed out");

            CollectionAssert.AreEqual(content, result);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DownloadAsyncStraight_ViaProducerConsumerStream_Succeeds()
        {
            fixture.SetupRemoteStreamInstances(content, FILE_SIZE);

            Action<ProducerConsumerStream> resetAction = default(Action<ProducerConsumerStream>);
            Func<ProducerConsumerStream, byte[], int, int, Task> writeAction = (stream, buffer, offset, count) => stream.WriteAsync(buffer, offset, count);

            Assert.IsTrue(fixture.PerformDownload(result, resetAction, writeAction), "Download timed out");

            CollectionAssert.AreEqual(content, result);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DownloadAsyncWithRetries_ViaProducerConsumerStream_Succeeds()
        {
            fixture.SetupRemoteStreamInstances(content, FILE_SIZE / 10, FILE_SIZE / 5, FILE_SIZE / 2, FILE_SIZE);

            Action<ProducerConsumerStream> resetAction = stream => stream.Reset();
            Func<ProducerConsumerStream, byte[], int, int, Task> writeAction = (stream, buffer, offset, count) => stream.WriteAsync(buffer, offset, count);

            Assert.IsTrue(fixture.PerformDownload(result, resetAction, writeAction), "Download timed out");

            CollectionAssert.AreEqual(content, result);
        }
    }
}
