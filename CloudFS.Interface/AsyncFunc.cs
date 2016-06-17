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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IgorSoft.CloudFS.Interface
{
    public static class AsyncFunc
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public static Task RetryAsync<TException>(this Func<Task> taskFactory, int retries)
            where TException : Exception
        {
            if (taskFactory == null)
                throw new ArgumentNullException(nameof(taskFactory));

            var exceptions = new List<TException>();
            do {
                try {
                    taskFactory().Wait();
                } catch (TException ex) {
                    exceptions.Add(ex);
                    Thread.Sleep((1 << (exceptions.Count - 1)) * 100);
                }
            } while (--retries >= 0);

            throw new AggregateException(string.Join(", ", exceptions.Select(e => e.Message)), exceptions);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public static async Task<TResult> RetryAsync<TResult, TException>(this Func<Task<TResult>> taskFactory, int retries)
            where TException : Exception
        {
            if (taskFactory == null)
                throw new ArgumentNullException(nameof(taskFactory));

            var exceptions = new List<TException>();
            do {
                try {
                    return await taskFactory();
                } catch (TException ex) {
                    exceptions.Add(ex);
                    Thread.Sleep((1 << (exceptions.Count - 1)) * 100);
                }
            } while (--retries >= 0);

            throw new AggregateException(string.Join(", ", exceptions.Select(e => e.Message)), exceptions);
        }
    }
}
