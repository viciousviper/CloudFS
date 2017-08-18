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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IgorSoft.CloudFS.Interface.IO;
using Polly;

namespace IgorSoft.CloudFS.GatewayTests
{
    public partial class RetryPolicyTests
    {
        private sealed class Fixture
        {
            private class SizeLimitedStream : MemoryStream
            {
                private readonly int limit;

                public SizeLimitedStream(byte[] buffer, int limit) : base(buffer)
                {
                    this.limit = limit;
                }

                public override int Read(byte[] buffer, int offset, int count)
                {
                    return Position + count <= limit
                        ? base.Read(buffer, offset, count)
                        : throw new IOException($"{nameof(Read)} failed for Position={Position}, count={count}");
                }

                public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
                {
                    return Position + count <= limit
                        ? base.ReadAsync(buffer, offset, count, cancellationToken)
                        : throw new IOException($"{nameof(ReadAsync)} failed for Position={Position}, count={count}");
                }

                public override long Seek(long offset, SeekOrigin loc)
                {
                    return offset <= limit
                        ? base.Seek(offset, loc)
                        : throw new IOException($"{nameof(Seek)} failed for offset={offset}");
                }

                public override void SetLength(long value)
                {
                    if (value <= limit)
                        base.SetLength(value);
                    else
                        throw new IOException($"{nameof(SetLength)} failed for value={value}");
                }

                public override void Write(byte[] buffer, int offset, int count)
                {
                    if (Position + count <= limit)
                        base.Write(buffer, offset, count);
                    else
                        throw new IOException($"{nameof(Write)} failed for Position={Position}, count={count}");
                }

                public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
                {
                    return Position + count <= limit
                        ? base.WriteAsync(buffer, offset, count, cancellationToken)
                        : throw new IOException($"{nameof(WriteAsync)} failed for Position={Position}, count={count}");
                }
            }

            private readonly Queue<Stream> streamFactory = new Queue<Stream>();

            public Stream GetRemoteStream()
            {
                return streamFactory.Dequeue();
            }

            public Policy GetWaitAndRetryPolicy(Action<Exception, TimeSpan> retryAction)
            {
                return Policy.Handle<IOException>().WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromMilliseconds(5), retryAction);
            }

            public byte[] GetContent(int size)
            {
                return Enumerable.Range(0, size).Select(i => (byte)i).ToArray();
            }

            public void SetupRemoteStreamInstances(byte[] content, params int[] sizes)
            {
                foreach (var size in sizes)
                    streamFactory.Enqueue(size < content.Length ? new SizeLimitedStream(content, size) : new MemoryStream(content));
            }

            public void PerformDownload(byte[] result, Action<MemoryStream> resetAction, Func<MemoryStream, byte[], int, int, Task> writeAction)
            {
                using (var localStream = new MemoryStream(result.Length)) {
                    var remoteStream = default(Stream);
                    try {
                        remoteStream = GetRemoteStream();

                        var sut = GetWaitAndRetryPolicy((ex, ts) => {
                            remoteStream.Dispose();
                            remoteStream = GetRemoteStream();
                            resetAction(localStream);
                        });
                        sut.ExecuteAsync(async () => {
                            var buffer = new byte[CHUNK_SIZE];
                            for (int offset = 0; offset < result.Length; offset += CHUNK_SIZE) {
                                var bytesRead = await remoteStream.ReadAsync(buffer, 0, CHUNK_SIZE, CancellationToken.None);
                                await writeAction(localStream, buffer, 0, bytesRead);
                            }
                            return Task.CompletedTask;
                        }).Wait();

                        Array.Copy(localStream.GetBuffer(), result, localStream.Length);
                    } finally {
                        remoteStream?.Dispose();
                    }
                }
            }

            public bool PerformDownload(byte[] result, Action<ProducerConsumerStream> resetAction, Func<ProducerConsumerStream, byte[], int, int, Task> writeAction)
            {
                using (var localStream = new ProducerConsumerStream()) {
                    var readTask = Task.Run(async () => {
                        var offset = 0;
                        while (offset < result.Length)
                            offset += await localStream.ReadAsync(result, offset, CHUNK_SIZE, CancellationToken.None);
                        return offset;
                    });

                    var remoteStream = default(Stream);
                    try {
                        remoteStream = GetRemoteStream();

                        var sut = GetWaitAndRetryPolicy((ex, ts) => {
                            remoteStream.Dispose();
                            remoteStream = GetRemoteStream();
                            resetAction(localStream);
                        });
                        sut.ExecuteAsync(async () => {
                            var buffer = new byte[CHUNK_SIZE];
                            for (int offset = 0; offset < result.Length; offset += CHUNK_SIZE) {
                                var bytesRead = await remoteStream.ReadAsync(buffer, 0, CHUNK_SIZE, CancellationToken.None);
                                await writeAction(localStream, buffer, 0, bytesRead);
                            }
                            return Task.CompletedTask;
                        }).Wait();
                    } finally {
                        remoteStream?.Dispose();
                    }

                    return readTask.Wait(TimeSpan.FromMilliseconds(100));
                }
            }
        }
    }
}
