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
    /// The descriptor of a cloud drive.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public sealed class DriveInfoContract
    {
        /// <summary>
        /// Gets the drive identifier.
        /// </summary>
        /// <value>
        /// The drive identifier.
        /// </value>
        public DriveId Id { get; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets the free space in bytes.
        /// </summary>
        /// <value>
        /// The free space in bytes, or <c>null</c> if unknown.
        /// </value>
        public long? FreeSpace { get; }

        /// <summary>
        /// Gets the used space in bytes.
        /// </summary>
        /// <value>
        /// The used space in bytes, or <c>null</c> if unknown.
        /// </value>
        public long? UsedSpace { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DriveInfoContract"/> class.
        /// </summary>
        /// <param name="id">The unique drive identifier.</param>
        /// <param name="freeSpace">The free space in bytes, or <c>null</c> if unknown.</param>
        /// <param name="usedSpace">The used space in bytes, or <c>null</c> if unknown.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public DriveInfoContract(string id, long? freeSpace, long? usedSpace)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException(nameof(id));

            Id = new DriveId(id);
            FreeSpace = freeSpace;
            UsedSpace = usedSpace;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private string DebuggerDisplay() => $"{nameof(DriveInfoContract)} {Id} ({Name})".ToString(CultureInfo.CurrentCulture);
    }
}
