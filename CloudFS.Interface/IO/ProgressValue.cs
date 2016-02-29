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
using System.Globalization;

namespace IgorSoft.CloudFS.Interface.IO
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public struct ProgressValue
    {
        public int PercentCompleted { get; }

        public int BytesTransferred { get; }

        public int BytesTotal { get; }

        public ProgressValue(int bytesTransferred, int bytesTotal) : this(bytesTotal != 0 ? bytesTransferred * 100 / bytesTotal : 0, bytesTransferred, bytesTotal)
        {
        }

        public ProgressValue(int percentCompleted, int bytesTransferred, int bytesTotal)
        {
            this = default(ProgressValue);
            PercentCompleted = percentCompleted;
            BytesTransferred = bytesTransferred;
            BytesTotal = bytesTotal;
        }

        public bool Equals(ProgressValue other) => PercentCompleted == other.PercentCompleted && BytesTransferred == other.BytesTransferred && BytesTotal == other.BytesTotal;

        public override bool Equals(object obj) => obj is ProgressValue && Equals((ProgressValue)obj);

        public override int GetHashCode() => PercentCompleted ^ BytesTransferred ^ BytesTotal;

        public static bool operator ==(ProgressValue first, ProgressValue second) => first.Equals(second);

        public static bool operator !=(ProgressValue first, ProgressValue second) => !(first == second);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Used for DebuggerDisplay")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private string DebuggerDisplay() => $"{BytesTransferred}/{BytesTotal} ({PercentCompleted}%)".ToString(CultureInfo.CurrentCulture);
    }
}
