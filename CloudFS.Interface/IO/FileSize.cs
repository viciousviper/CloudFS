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
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using SemanticTypes;

namespace IgorSoft.CloudFS.Interface.IO
{
    /// <summary>
    /// The size of a file in bytes.
    /// </summary>
    /// <seealso cref="SemanticType{long}" />
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay(),nq}")]
#pragma warning disable CS3009 // Base type is not CLS-compliant
    public sealed class FileSize : SemanticType<long>
#pragma warning restore CS3009 // Base type is not CLS-compliant
    {
        private const string B = nameof(B);

        private const string kB = nameof(kB);

        private const string MB = nameof(MB);

        private const string GB = nameof(GB);

        private const string TB = nameof(TB);

        private const string PB = nameof(PB);

        private const string EB = nameof(EB);

        private static readonly Regex regex = new Regex($"(?<units>\\d+({CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator}\\d+)?)(?<multiplier>{B}|{kB}|{MB}|{GB}|{TB}|{PB}|{EB})", RegexOptions.Compiled);

        private static readonly Dictionary<string, long> multipliers = new Dictionary<string, long>()
        {
            [B] = 1L << 0,
            [kB] = 1L << 10,
            [MB] = 1L << 20,
            [GB] = 1L << 30,
            [TB] = 1L << 40,
            [PB] = 1L << 50,
            [EB] = 1L << 60
        };

        /// <summary>
        /// The undefined file size.
        /// </summary>
        public static readonly FileSize Undefined = new FileSize(-1);

        /// <summary>
        /// The empty file size.
        /// </summary>
        public static readonly FileSize Empty = new FileSize(0);

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSize"/> class.
        /// </summary>
        /// <param name="bytes">The file size in bytes.</param>
        public FileSize(long bytes) : base(b => b >= -1, bytes)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSize"/> class.
        /// </summary>
        /// <param name="bytes">The file size in bytes.</param>
        public FileSize(decimal bytes) : base(b => b >= -1, EnsureIntegral(bytes))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSize"/> class.
        /// </summary>
        /// <param name="size">The file size as <see cref="string"/>.</param>
        public FileSize(string size) : base(b => b >= -1, Parse(size))
        {
        }

        private static long EnsureIntegral(decimal value)
        {
            if (decimal.Truncate(value) != value)
                throw new ArithmeticException();

            return (long)value;
        }

        private static long Parse(string size)
        {
            if (size == nameof(Undefined))
                return -1;

            var match = regex.Match(size);
            if (!match.Success)
                throw new FormatException(string.Format(Properties.Resources.InvalidFormat, nameof(FileSize), string.Join("|", multipliers.Keys), CultureInfo.CurrentCulture));

            var units = decimal.Parse(match.Groups["units"].Value);
            var multiplier = match.Groups["multiplier"].Value;

            return EnsureIntegral(units * multipliers[multiplier]);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="FileSize"/> to <see cref="long"/>.
        /// </summary>
        /// <param name="fileSize">The <see cref="FileSize"/> instance to convert.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator long(FileSize fileSize) => (fileSize ?? Undefined).Value;

        /// <summary>
        /// Performs an implicit conversion from <see cref="long"/> to <see cref="FileSize"/>.
        /// </summary>
        /// <param name="bytes">The file size in bytes.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator FileSize(long bytes) => new FileSize(bytes);

        /// <summary>
        /// Implements the operator +.
        /// </summary>
        /// <param name="fileSize1">A <see cref="FileSize"/>.</param>
        /// <param name="fileSize2">A <see cref="FileSize"/>.</param>
        /// <returns>A <see cref="FileSize"/> representing the sum of <paramref name="fileSize1"/> and <paramref name="fileSize2"/>.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1013:OverloadOperatorEqualsOnOverloadingAddAndSubtract")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates")]
        public static FileSize operator +(FileSize fileSize1, FileSize fileSize2)
        {
            if (fileSize1 == null)
                throw new ArgumentNullException(nameof(fileSize1));
            if (fileSize2 == null)
                throw new ArgumentNullException(nameof(fileSize2));
            if (fileSize1 == Undefined)
                throw new ArgumentException(string.Format(Properties.Resources.FileSizeArgumentUndefined, nameof(fileSize1), CultureInfo.CurrentCulture));
            if (fileSize2 == Undefined)
                throw new ArgumentException(string.Format(Properties.Resources.FileSizeArgumentUndefined, nameof(fileSize2), CultureInfo.CurrentCulture));

            return new FileSize(fileSize1.Value + fileSize2.Value);
        }

        /// <summary>
        /// Implements the operator -.
        /// </summary>
        /// <param name="fileSize1">A <see cref="FileSize"/>.</param>
        /// <param name="fileSize2">A <see cref="FileSize"/>.</param>
        /// <returns>A <see cref="FileSize"/> representing the difference of <paramref name="fileSize1"/> and <paramref name="fileSize2"/>.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1013:OverloadOperatorEqualsOnOverloadingAddAndSubtract")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates")]
        public static FileSize operator -(FileSize fileSize1, FileSize fileSize2)
        {
            if (fileSize1 == null)
                throw new ArgumentNullException(nameof(fileSize1));
            if (fileSize2 == null)
                throw new ArgumentNullException(nameof(fileSize2));
            if (fileSize1 == Undefined)
                throw new ArgumentException(string.Format(Properties.Resources.FileSizeArgumentUndefined, nameof(fileSize1), CultureInfo.CurrentCulture));
            if (fileSize2 == Undefined)
                throw new ArgumentException(string.Format(Properties.Resources.FileSizeArgumentUndefined, nameof(fileSize2), CultureInfo.CurrentCulture));

            return new FileSize(fileSize1.Value - fileSize2.Value);
        }

        /// <summary>
        /// Implements the operator *.
        /// </summary>
        /// <param name="fileSize">The base <see cref="FileSize"/>.</param>
        /// <param name="scale">The scale.</param>
        /// <returns>A <see cref="FileSize"/> representing <paramref name="fileSize"/> multiplied by <paramref name="scale"/>.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates")]
        public static FileSize operator *(FileSize fileSize, decimal scale)
        {
            if (fileSize == null)
                throw new ArgumentNullException(nameof(fileSize));
            if (fileSize == Undefined)
                throw new ArgumentException(string.Format(Properties.Resources.FileSizeArgumentUndefined, nameof(fileSize), CultureInfo.CurrentCulture));

            return new FileSize(fileSize.Value * scale);
        }

        /// <summary>
        /// Implements the operator *.
        /// </summary>
        /// <param name="scale">The scale.</param>
        /// <param name="fileSize">The base <see cref="FileSize"/>.</param>
        /// <returns>A <see cref="FileSize"/> representing <paramref name="fileSize"/> multiplied by <paramref name="scale"/>.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates")]
        public static FileSize operator *(decimal scale, FileSize fileSize) => fileSize * scale;

        /// <summary>
        /// Implements the operator *.
        /// </summary>
        /// <param name="fileSize">The base <see cref="FileSize"/>.</param>
        /// <param name="scale">The scale.</param>
        /// <returns>A <see cref="FileSize"/> representing <paramref name="fileSize"/> multiplied by <paramref name="scale"/>.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates")]
        public static FileSize operator *(FileSize fileSize, long scale)
        {
            if (fileSize == null)
                throw new ArgumentNullException(nameof(fileSize));
            if (fileSize == Undefined)
                throw new ArgumentException(string.Format(Properties.Resources.FileSizeArgumentUndefined, nameof(fileSize), CultureInfo.CurrentCulture));

            return new FileSize(fileSize.Value * scale);
        }

        /// <summary>
        /// Implements the operator *.
        /// </summary>
        /// <param name="scale">The scale.</param>
        /// <param name="fileSize">The base <see cref="FileSize"/>.</param>
        /// <returns>A <see cref="FileSize"/> representing <paramref name="fileSize"/> multiplied by <paramref name="scale"/>.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates")]
        public static FileSize operator *(long scale, FileSize fileSize) => fileSize * scale;

        /// <summary>
        /// Implements the operator /.
        /// </summary>
        /// <param name="fileSize">The base <see cref="FileSize"/>.</param>
        /// <param name="scale">The scale.</param>
        /// <returns>A <see cref="FileSize"/> representing <paramref name="fileSize"/> multiplied by <paramref name="scale"/>.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates")]
        public static FileSize operator /(FileSize fileSize, decimal scale)
        {
            if (fileSize == null)
                throw new ArgumentNullException(nameof(fileSize));
            if (fileSize == Undefined)
                throw new ArgumentException(string.Format(Properties.Resources.FileSizeArgumentUndefined, nameof(fileSize), CultureInfo.CurrentCulture));

            return new FileSize(fileSize.Value / scale);
        }

        /// <summary>
        /// Implements the operator /.
        /// </summary>
        /// <param name="fileSize">The base <see cref="FileSize"/>.</param>
        /// <param name="scale">The scale.</param>
        /// <returns>A <see cref="FileSize"/> representing <paramref name="fileSize"/> multiplied by <paramref name="scale"/>.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates")]
        public static FileSize operator /(FileSize fileSize, long scale)
        {
            if (fileSize == null)
                throw new ArgumentNullException(nameof(fileSize));
            if (fileSize == Undefined)
                throw new ArgumentException(string.Format(Properties.Resources.FileSizeArgumentUndefined, nameof(fileSize), CultureInfo.CurrentCulture));

            return new FileSize((decimal)fileSize.Value / scale);
        }

        /// <summary>
        /// Returns a <see cref="string"/> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="string"/> that represents this instance.</returns>
        public override string ToString()
        {
            if (Value == -1)
                return nameof(Undefined);

            foreach (var multiplier in multipliers.Reverse())
                if (Value >= multiplier.Value && Value % ((decimal)multiplier.Value / 1000) == 0)
                    return $"{Value / (decimal)multiplier.Value}{multiplier.Key}".ToString(CultureInfo.CurrentCulture);

            return $"{Value.ToString(CultureInfo.CurrentCulture)}{B}";
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Used for DebuggerDisplay")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private string DebuggerDisplay() => $"{nameof(FileSize)} {ToString()}".ToString(CultureInfo.CurrentCulture);
    }
}
