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
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IgorSoft.CloudFS.Interface.IO
{
    public sealed class ProducerConsumerStream : Stream
    {
        private readonly BlockingCollection<byte[]> blocks = new BlockingCollection<byte[]>();

        private byte[] currentBlock;

        private int currentBlockIndex;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanTimeout => false;

        public override bool CanWrite => true;

        public override long Length => blocks.IsCompleted
            ? TotalBytesWritten
            : throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int ReadTimeout
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int WriteTimeout
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public long TotalBytesWritten { get; private set; }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public override void Flush()
        {
            CompleteWriting();
        }

        public override object InitializeLifetimeService()
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateBufferArgs(buffer, offset, count);

            var bytesRead = 0;
            while (true) {
                if (currentBlock != null) {
                    var copy = Math.Min(count - bytesRead, currentBlock.Length - currentBlockIndex);
                    Array.Copy(currentBlock, currentBlockIndex, buffer, offset + bytesRead, copy);
                    currentBlockIndex += copy;
                    bytesRead += copy;

                    if (currentBlock.Length <= currentBlockIndex) {
                        currentBlock = null;
                        currentBlockIndex = 0;
                    }

                    if (bytesRead == count)
                        return bytesRead;
                }

                if (!blocks.TryTake(out currentBlock, Timeout.Infinite))
                    return bytesRead;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ValidateBufferArgs(buffer, offset, count);

            var newBlock = new byte[count];
            Array.Copy(buffer, offset, newBlock, 0, count);
            blocks.Add(newBlock);
            TotalBytesWritten += count;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                blocks.Dispose();

            base.Dispose(disposing);
        }

        private void CompleteWriting()
        {
            if (!blocks.IsAddingCompleted)
                blocks.CompleteAdding();
        }

        private static void ValidateBufferArgs(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (buffer.Length - offset < count)
                throw new ArgumentException($"{nameof(buffer)}.{nameof(buffer.Length)} - {nameof(offset)} < {nameof(count)}");
        }
    }
}
