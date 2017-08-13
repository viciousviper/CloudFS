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
using System.Threading;
using System.Threading.Tasks;
using Moq;
using IgorSoft.CloudFS.Interface.IO;

namespace IgorSoft.CloudFS.InterfaceTests.IO
{
    public partial class ProgressStreamTests
    {
        private sealed class Fixture
        {
            private readonly Mock<Stream> attachedStreamMock = new Mock<Stream>();

            private readonly Mock<IProgress<ProgressValue>> progressMock = new Mock<IProgress<ProgressValue>>();

            public ProgressStream GetProgressStream() => new ProgressStream(attachedStreamMock.Object, progressMock.Object);

            public void ConfigureCanRead(params bool[] values)
            {
                var sequence = attachedStreamMock.SetupSequence(a => a.CanRead);
                foreach (var value in values)
                    sequence.Returns(value);
            }

            public void ConfigureCanSeek(params bool[] values)
            {
                var sequence = attachedStreamMock.SetupSequence(a => a.CanSeek);
                foreach (var value in values)
                    sequence.Returns(value);
            }

            public void ConfigureCanTimeout(params bool[] values)
            {
                var sequence = attachedStreamMock.SetupSequence(a => a.CanTimeout);
                foreach (var value in values)
                    sequence.Returns(value);
            }

            public void ConfigureCanWrite(params bool[] values)
            {
                var sequence = attachedStreamMock.SetupSequence(a => a.CanWrite);
                foreach (var value in values)
                    sequence.Returns(value);
            }

            public void ConfigureLength(long value)
            {
                attachedStreamMock
                    .SetupGet(a => a.Length)
                    .Returns(value);
            }

            public void ConfigureGetPosition(long value)
            {
                attachedStreamMock
                    .SetupGet(a => a.Position)
                    .Returns(value)
                    .Verifiable();
            }

            public void ConfigureSetPosition(long value)
            {
                attachedStreamMock
                    .SetupSet(a => a.Position = value)
                    .Verifiable();
            }

            public void ConfigureFlush()
            {
                attachedStreamMock
                    .Setup(a => a.Flush())
                    .Verifiable();
            }

            public void ConfigureRead(long length, int size)
            {
                attachedStreamMock
                    .Setup(a => a.Read(It.Is<byte[]>(b => b.Length == size), 0, size))
                    .Returns(size)
                    .Verifiable();
                progressMock
                    .Setup(p => p.Report(It.Is<ProgressValue>(v => v.BytesTotal == length && v.BytesTransferred == size)))
                    .Verifiable();
            }

            public void ConfigureReadAsync(long length, int size)
            {
                attachedStreamMock
                    .Setup(a => a.ReadAsync(It.Is<byte[]>(b => b.Length == size), 0, size, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(size)
                    .Verifiable();
                progressMock
                    .Setup(p => p.Report(It.Is<ProgressValue>(v => v.BytesTotal == length && v.BytesTransferred == size)))
                    .Verifiable();
            }

            public void ConfigureSeek(long offset, SeekOrigin origin, long result)
            {
                attachedStreamMock
                    .Setup(a => a.Seek(offset, origin))
                    .Returns(result)
                    .Verifiable();
            }

            public void ConfigureSetLength(long value)
            {
                attachedStreamMock
                    .Setup(a => a.SetLength(value))
                    .Verifiable();
            }

            public void ConfigureWrite(long length, int size)
            {
                attachedStreamMock
                    .Setup(a => a.Write(It.Is<byte[]>(b => b.Length == size), 0, size))
                    .Verifiable();
                progressMock
                    .Setup(p => p.Report(It.Is<ProgressValue>(v => v.BytesTotal == length && v.BytesTransferred == size)))
                    .Verifiable();
            }

            public void ConfigureWriteAsync(long length, int size)
            {
                attachedStreamMock
                    .SetupGet(a => a.CanWrite)
                    .Returns(true);
                attachedStreamMock
                    .Setup(a => a.WriteAsync(It.Is<byte[]>(b => b.Length == size), 0, size, It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(default(object)))
                    .Verifiable();
                progressMock
                    .Setup(p => p.Report(It.Is<ProgressValue>(v => v.BytesTotal == length && v.BytesTransferred == size)))
                    .Verifiable();
            }

            public void Verify()
            {
                attachedStreamMock.Verify();
            }
        }
    }
}
