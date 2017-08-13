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
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IgorSoft.CloudFS.Interface.IO;
using IgorSoft.CloudFS.InterfaceTests;

namespace IgorSoft.CloudFS.InterfaceTests.IO
{
    [TestClass]
    public sealed partial class ProducerConsumerStreamTests
    {
        private ProducerConsumerStream sut;

        [TestInitialize]
        public void Initialize()
        {
            sut = Fixture.CreateStream();
        }

        [TestCleanup]
        public void Cleanup()
        {
            sut.Dispose();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void ProducerConsumerStream_CanRead_ReturnsTrue()
        {
            Assert.IsTrue(sut.CanRead);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void ProducerConsumerStream_CanSeek_ReturnsFalse()
        {
            Assert.IsFalse(sut.CanSeek);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void ProducerConsumerStream_CanTimeout_ReturnsFalse()
        {
            Assert.IsFalse(sut.CanTimeout);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void ProducerConsumerStream_CanWrite_ReturnsTrue()
        {
            Assert.IsTrue(sut.CanWrite);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(NotSupportedException))]
        public void ProducerConsumerStream_Length_OnUnflushedStream_Throws()
        {
#pragma warning disable S1481 // Unused local variables should be removed
            var length = sut.Length;
#pragma warning restore S1481 // Unused local variables should be removed
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void ProducerConsumerStream_Length_OnFlushedStream_ReturnsLength()
        {
            sut.Flush();
            var length = sut.Length;

            Assert.AreEqual(0, length);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(NotSupportedException))]
        public void ProducerConsumerStream_GetPosition_Throws()
        {
#pragma warning disable S1481 // Unused local variables should be removed
            var position = sut.Position;
#pragma warning restore S1481 // Unused local variables should be removed
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(NotSupportedException))]
        public void ProducerConsumerStream_SetPosition_Throws()
        {
            sut.Position = 0;
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(NotSupportedException))]
        public void ProducerConsumerStream_GetReadTimeout_Throws()
        {
#pragma warning disable S1481 // Unused local variables should be removed
            var readTimeout = sut.ReadTimeout;
#pragma warning restore S1481 // Unused local variables should be removed
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(NotSupportedException))]
        public void ProducerConsumerStream_SetReadTimeout_Throws()
        {
            sut.ReadTimeout = 0;
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(NotSupportedException))]
        public void ProducerConsumerStream_GetWriteTimeout_Throws()
        {
#pragma warning disable S1481 // Unused local variables should be removed
            var writeTimeout = sut.WriteTimeout;
#pragma warning restore S1481 // Unused local variables should be removed
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(NotSupportedException))]
        public void ProducerConsumerStream_SetWriteTimeout_Throws()
        {
            sut.WriteTimeout = 0;
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(NotSupportedException))]
        public void ProducerConsumerStream_CopyToAsync_Throws()
        {
            var destination = Stream.Null;

            sut.CopyToAsync(destination, 1024);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(NotSupportedException))]
        public void ProducerConsumerStream_InitializeLifetimeService_Throws()
        {
#pragma warning disable S1481 // Unused local variables should be removed
            var result = sut.InitializeLifetimeService();
#pragma warning restore S1481 // Unused local variables should be removed
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ProducerConsumerStream_Read_WhereBufferIsNull_Throws()
        {
#pragma warning disable S1481 // Unused local variables should be removed
            var result = sut.Read(null, 0, 0);
#pragma warning restore S1481 // Unused local variables should be removed
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ProducerConsumerStream_Read_WhereOffsetIsNegative_Throws()
        {
#pragma warning disable S1481 // Unused local variables should be removed
            var result = sut.Read(new byte[0], -1, 0);
#pragma warning restore S1481 // Unused local variables should be removed
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ProducerConsumerStream_Read_WhereCountIsNegative_Throws()
        {
#pragma warning disable S1481 // Unused local variables should be removed
            var result = sut.Read(new byte[0], 0, -1);
#pragma warning restore S1481 // Unused local variables should be removed
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentException))]
        public void ProducerConsumerStream_Read_ExceedingBufferBounds_Throws()
        {
#pragma warning disable S1481 // Unused local variables should be removed
            var result = sut.Read(new byte[0], 0, 1);
#pragma warning restore S1481 // Unused local variables should be removed
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void ProducerConsumerStream_Read_FromEmptyStream_ReturnsZeroBytes()
        {
            const int count = 10;
            var readBuffer = new byte[count];

            sut.Flush();
            var result = sut.Read(readBuffer, 0, readBuffer.Length);

            Assert.AreEqual(0, result);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void ProducerConsumerStream_Read_FromFilledStream_ReturnsContent()
        {
            const int count = 10;
            var writeBuffer = Enumerable.Range(0, count).Select(i => (byte)i).ToArray();
            var readBuffer = new byte[count];

            sut.Write(writeBuffer, 0, writeBuffer.Length);
            var result = sut.Read(readBuffer, 0, readBuffer.Length);

            Assert.AreEqual(count, result);
            CollectionAssert.AreEqual(writeBuffer, readBuffer);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void ProducerConsumerStream_Read_FromStarvedStream_ReturnsContent()
        {
            const int count = 10;
            var writeBuffer = Enumerable.Range(0, count).Select(i => (byte)i).ToArray();
            var readBuffer = new byte[count + 1];

            sut.Write(writeBuffer, 0, writeBuffer.Length);
            sut.Flush();
            var result = sut.Read(readBuffer, 0, readBuffer.Length);

            Assert.AreEqual(count, result);
            CollectionAssert.AreEqual(writeBuffer, readBuffer.Take(count).ToArray());
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(NotSupportedException))]
        public void ProducerConsumerStream_Seek_Throws()
        {
#pragma warning disable S1481 // Unused local variables should be removed
            var result = sut.Seek(0, SeekOrigin.Begin);
#pragma warning restore S1481 // Unused local variables should be removed
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(NotSupportedException))]
        public void ProducerConsumerStream_SetLength_Throws()
        {
            sut.SetLength(1000);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ProducerConsumerStream_Write_WhereBufferIsNull_Throws()
        {
            sut.Write(null, 0, 0);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ProducerConsumerStream_Write_WhereOffsetIsNegative_Throws()
        {
            sut.Write(new byte[0], -1, 0);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ProducerConsumerStream_Write_WhereCountIsNegative_Throws()
        {
            sut.Write(new byte[0], 0, -1);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentException))]
        public void ProducerConsumerStream_Write_ExceedingBufferBounds_Throws()
        {
            sut.Write(new byte[0], 0, 1);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void ProducerConsumerStream_Write_UpdatesTotalBytesWritten()
        {
            const int count = 10;
            var writeBuffer = new byte[count];

            sut.Write(writeBuffer, 0, count);
            var result = sut.TotalBytesWritten;

            Assert.AreEqual(count, result);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ProducerConsumerStream_Write_ToFlushedStream_Throws()
        {
            const int count = 10;
            var writeBuffer = new byte[count];

            sut.Flush();
            sut.Write(writeBuffer, 0, writeBuffer.Length);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void ProducerConsumerStream_ConcurrentReadWrite_Succeeds()
        {
            const int count = 48;
            var writeBuffer = Enumerable.Range(0, count).Select(i => (byte)i).ToArray();
            var readBuffer = new byte[count];

            var result = 0;
            var readThread = new Thread(() => {
                for (var i = 0; i < count / 4; ++i) {
                    result += sut.Read(readBuffer, 4 * i, 4);
                    Thread.Sleep(3);
                }
            });
            readThread.Start();
            var writeThread = new Thread(() => {
                for (var i = 0; i < count / 3; ++i) {
                    Thread.Sleep(4);
                    sut.Write(writeBuffer, 3 * i, 3);
                }
                sut.Flush();
            });
            writeThread.Start();

            Assert.IsTrue(readThread.Join(200));
            Assert.IsTrue(writeThread.Join(200));

            Assert.AreEqual(count, result);
            CollectionAssert.AreEqual(writeBuffer, readBuffer);
        }
    }
}
