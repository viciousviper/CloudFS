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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IgorSoft.CloudFS.InterfaceTests.IO
{
    [TestClass]
    public sealed partial class ProgressStreamTests
    {
        private Fixture fixture;

        [TestInitialize]
        public void Initialize()
        {
            fixture = new Fixture();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void ProgressStream_CanRead_ReturnsAttachedStreamCanRead()
        {
            var values = new[] { true, false };

            fixture.ConfigureCanRead(values);

            using (var sut = fixture.GetProgressStream()) {
                Array.ForEach(values, v => Assert.AreEqual(v, sut.CanRead));
            }
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void ProgressStream_CanSeek_ReturnsAttachedStreamCanSeek()
        {
            var values = new[] { true, false };

            fixture.ConfigureCanSeek(values);

            using (var sut = fixture.GetProgressStream()) {
                Array.ForEach(values, v => Assert.AreEqual(v, sut.CanSeek));
            }
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void ProgressStream_CanTimeout_ReturnsAttachedStreamCanTimeout()
        {
            var values = new[] { true, false };

            fixture.ConfigureCanTimeout(values);

            using (var sut = fixture.GetProgressStream()) {
                Array.ForEach(values, v => Assert.AreEqual(v, sut.CanTimeout));
            }
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void ProgressStream_CanWrite_ReturnsAttachedStreamCanWrite()
        {
            var values = new[] { true, false };

            fixture.ConfigureCanWrite(values);

            using (var sut = fixture.GetProgressStream()) {
                Array.ForEach(values, v => Assert.AreEqual(v, sut.CanWrite));
            }
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void ProgressStream_Length_ReturnsAttachedStreamLength()
        {
            const long value = 100L;

            fixture.ConfigureLength(value);

            using (var sut = fixture.GetProgressStream()) {
                Assert.AreEqual(value, sut.Length);
            }
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void ProgressStream_GetPosition_ReturnsAttachedStreamPosition()
        {
            const long value = 100L;

            fixture.ConfigureGetPosition(value);

            using (var sut = fixture.GetProgressStream()) {
                Assert.AreEqual(value, sut.Position);
            }

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void ProgressStream_SetPosition_SetsAttachedStreamPosition()
        {
            const long value = 100L;

            fixture.ConfigureSetPosition(value);

            using (var sut = fixture.GetProgressStream()) {
                sut.Position = value;
            }

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void ProgressStream_Flush_CallsFlushOnAttachedStream()
        {
            fixture.ConfigureFlush();

            using (var sut = fixture.GetProgressStream()) {
                sut.Flush();
            }

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void ProgressStream_Read_ReportsToProgress()
        {
            const long length = 1000L;
            const int size = 100;
            fixture.ConfigureLength(length);
            fixture.ConfigureRead(length, size);

            using (var sut = fixture.GetProgressStream()) {
                var result = sut.Read(new byte[size], 0, size);

                Assert.AreEqual(size, result);
            }

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void ProgressStream_ReadAsync_ReportsProgress()
        {
            const long length = 1000L;
            const int size = 100;
            fixture.ConfigureLength(length);
            fixture.ConfigureReadAsync(length, size);

            using (var sut = fixture.GetProgressStream()) {
                var result = sut.ReadAsync(new byte[size], 0, size).Result;

                Assert.AreEqual(size, result);
            }

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void ProgressStream_Seek_CallsSeekOnAttachedStream()
        {
            const long offset = 1000L;
            const SeekOrigin origin = SeekOrigin.Begin;
            fixture.ConfigureSeek(offset, origin, offset);

            using (var sut = fixture.GetProgressStream()) {
                var result = sut.Seek(offset, origin);

                Assert.AreEqual(offset, result);
            }

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void ProgressStream_SetLength_CallsSetLengthOnAttachedStream()
        {
            const long value = 1000L;
            fixture.ConfigureSetLength(value);

            using (var sut = fixture.GetProgressStream()) {
                sut.SetLength(value);
            }

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void ProgressStream_Write_ReportsToProgress()
        {
            const long length = 1000L;
            const int size = 100;
            fixture.ConfigureLength(length);
            fixture.ConfigureWrite(length, size);

            using (var sut = fixture.GetProgressStream()) {
                sut.Write(new byte[size], 0, size);
            }

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void ProgressStream_WriteAsync_ReportsToProgress()
        {
            const long length = 1000L;
            const int size = 100;
            fixture.ConfigureLength(length);
            fixture.ConfigureWriteAsync(length, size);

            using (var sut = fixture.GetProgressStream()) {
                sut.WriteAsync(new byte[size], 0, size).Wait();
            }

            fixture.Verify();
        }
    }
}
