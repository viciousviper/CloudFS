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
using System.IO;

namespace IgorSoft.CloudFS.Interface.IO
{
    /// <summary>
    /// The descriptor of a cloud directory.
    /// </summary>
    /// <seealso cref="FileSystemInfoContract" />
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public class DirectoryInfoContract : FileSystemInfoContract
    {
        /// <summary>
        /// Gets the unique directory identifier.
        /// </summary>
        /// <value>The unique directory identifier.</value>
        public new DirectoryId Id => (DirectoryId)base.Id;

        /// <summary>
        /// Gets or sets the parent directory of a specified subdirectory.
        /// </summary>
        /// <value>The parent directory, or <c>null</c> if the path is null or if the file path denotes a root.</value>
        public DirectoryInfoContract Parent { get; set; }

        /// <summary>
        /// Gets the full path of the directory.
        /// </summary>
        /// <value>A <see cref="string" /> containing the full path.</value>
        public override string FullName => (Parent?.FullName ?? string.Empty) + Name + Path.DirectorySeparatorChar;

        /// <summary>
        /// Gets the mode of the directory.
        /// </summary>
        /// <value>The <see cref="string" /> containing the mode.</value>
        public override string Mode => "d----";

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectoryInfoContract"/> class.
        /// </summary>
        /// <param name="id">The unique identifier.</param>
        /// <param name="name">The name.</param>
        /// <param name="created">The creation time.</param>
        /// <param name="updated">The time when the current <see cref="DirectoryInfoContract"/> was last written.</param>
        public DirectoryInfoContract(string id, string name, DateTimeOffset created, DateTimeOffset updated) : base(new DirectoryId(id), name, created, updated)
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private string DebuggerDisplay() => $"{nameof(DirectoryInfoContract)} {Id} ({Name})".ToString(CultureInfo.CurrentCulture);
    }
}
