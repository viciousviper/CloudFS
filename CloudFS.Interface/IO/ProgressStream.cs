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
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IgorSoft.CloudFS.Interface.IO
{
    public class ProgressStream : Stream
    {
        private const int REPORT_PERCENTAGE_THRESHOLD = 5;

        private Stream attachedStream;

        private IProgress<ProgressValue> progress;

        private int bytesTransferred;

        private int bytesTotal;

        private int reported;

        public override bool CanRead => attachedStream.CanRead;

        public override bool CanSeek => attachedStream.CanSeek;

        public override bool CanTimeout => attachedStream.CanTimeout;

        public override bool CanWrite => attachedStream.CanWrite;

        public override long Length => attachedStream.Length;

        public override long Position
        {
            get { return attachedStream.Position; }
            set { attachedStream.Position = value; }
        }

        public ProgressStream(Stream attachedStream, IProgress<ProgressValue> progress)
        {
            if (attachedStream == null)
                throw new ArgumentNullException(nameof(attachedStream));

            this.attachedStream = attachedStream;
            this.progress = progress;
            bytesTotal = (int)attachedStream.Length;
        }

        private int Report(int bytesRead)
        {
            bytesTransferred += bytesRead;

            if (reported == 0 || (bytesTransferred - reported) * 100 / bytesTotal > REPORT_PERCENTAGE_THRESHOLD) {
                progress.Report(new ProgressValue(bytesTransferred, bytesTotal));
                reported = bytesTransferred;
            }

            return bytesRead;
        }

        public override void Flush()
        {
            attachedStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return Report(attachedStream.Read(buffer, offset, count));
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return Report(await attachedStream.ReadAsync(buffer, offset, count, cancellationToken));
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return attachedStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            attachedStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            attachedStream.Write(buffer, offset, count);
            Report(count);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await base.WriteAsync(buffer, offset, count, cancellationToken);
            Report(count);
        }
    }
}
