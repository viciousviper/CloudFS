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
    /// <summary>
    /// A progress update value.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public struct ProgressValue
    {
        /// <summary>
        /// Gets the completion percentage of the associated operation.
        /// </summary>
        /// <value>The completion percentage.</value>
        public int PercentCompleted { get; }

        /// <summary>
        /// Gets the number of transferred bytes.
        /// </summary>
        /// <value>The transferred bytes.</value>
        public int BytesTransferred { get; }

        /// <summary>
        /// Gets the number of total bytes.
        /// </summary>
        /// <value>The total bytes.</value>
        public int BytesTotal { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressValue"/> struct.
        /// </summary>
        /// <param name="bytesTransferred">The number of transferred bytes.</param>
        /// <param name="bytesTotal">The number of total bytes.</param>
        public ProgressValue(int bytesTransferred, int bytesTotal) : this(bytesTotal != 0 ? bytesTransferred * 100 / bytesTotal : 0, bytesTransferred, bytesTotal)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressValue"/> struct.
        /// </summary>
        /// <param name="percentCompleted">The completion percentage.</param>
        /// <param name="bytesTransferred">The number of transferred bytes.</param>
        /// <param name="bytesTotal">The number of total bytes.</param>
        public ProgressValue(int percentCompleted, int bytesTransferred, int bytesTotal)
        {
            this = default(ProgressValue);
            PercentCompleted = percentCompleted;
            BytesTransferred = bytesTransferred;
            BytesTotal = bytesTotal;
        }

        /// <summary>
        /// Determines whether the specified <see cref="ProgressValue" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="ProgressValue" /> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="ProgressValue" /> is equal to this instance; otherwise, <c>false</c>.</returns>
        public bool Equals(ProgressValue other) => PercentCompleted == other.PercentCompleted && BytesTransferred == other.BytesTransferred && BytesTotal == other.BytesTotal;

        /// <summary>
        /// Determines whether the specified <see cref="object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object" /> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="object" /> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj) => obj is ProgressValue && Equals((ProgressValue)obj);

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode() => PercentCompleted ^ BytesTransferred ^ BytesTotal;

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="first">The first <see cref="ProgressValue"/>.</param>
        /// <param name="second">The second <see cref="ProgressValue"/>.</param>
        /// <returns><c>true</c> if both values are equal; otherwise, <c>false</c>.</returns>
        public static bool operator ==(ProgressValue first, ProgressValue second) => first.Equals(second);

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="first">The first <see cref="ProgressValue"/>.</param>
        /// <param name="second">The second <see cref="ProgressValue"/>.</param>
        /// <returns><c>true</c> if both values are different; otherwise, <c>false</c>.</returns>
        public static bool operator !=(ProgressValue first, ProgressValue second) => !(first == second);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Used for DebuggerDisplay")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private string DebuggerDisplay() => $"{BytesTransferred}/{BytesTotal} ({PercentCompleted}%)".ToString(CultureInfo.CurrentCulture);
    }
}
