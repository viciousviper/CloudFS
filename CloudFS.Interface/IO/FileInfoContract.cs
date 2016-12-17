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
    /// The descriptor of a cloud file.
    /// </summary>
    /// <seealso cref="FileSystemInfoContract" />
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public class FileInfoContract : FileSystemInfoContract
    {
        /// <summary>
        /// Gets the unique file identifier.
        /// </summary>
        /// <value>The unique file identifier.</value>
        public new FileId Id => (FileId)base.Id;

        /// <summary>
        /// Gets an instance of the parent directory.
        /// </summary>
        /// <value>A <see cref="DirectoryInfoContract"/> object representing the parent directory of this file.</value>
        public DirectoryInfoContract Directory { get; set; }

        /// <summary>
        /// Gets the full path of the file.
        /// </summary>
        /// <value>A <see cref="string"/> containing the full path.</value>
        public override string FullName => (Directory?.FullName ?? string.Empty) + Name;

        /// <summary>
        /// Gets the mode of the file.
        /// </summary>
        /// <value>The <see cref="string" /> containing the mode.</value>
        public override string Mode => "-----";

        /// <summary>
        /// Gets or sets the size, in bytes, of the current file.
        /// </summary>
        /// <value>The size of the current file in bytes.</value>
        public FileSize Size { get; set; }

        /// <summary>
        /// Gets the cryptographic hash of the current file.
        /// </summary>
        /// <value>A <see cref="string" /> containing the cryptographic hash of the current file.</value>
        public string Hash { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileInfoContract"/> class.
        /// </summary>
        /// <param name="id">The unique identifier.</param>
        /// <param name="name">The name.</param>
        /// <param name="created">The creation time.</param>
        /// <param name="updated">The time when the current <see cref="FileInfoContract"/> was last written.</param>
        /// <param name="size">The file size in bytes.</param>
        /// <param name="hash">The cryptographic hash.</param>
        /// <exception cref="ArgumentException">The size is negative.</exception>
        public FileInfoContract(string id, string name, DateTimeOffset created, DateTimeOffset updated, FileSize size, string hash) : base(new FileId(id), name, created, updated)
        {
            if (size == FileSize.Undefined)
                throw new ArgumentException($"{nameof(size)} == {nameof(FileSize.Undefined)}", nameof(size));

            Size = size;
            Hash = hash;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private string DebuggerDisplay() => $"{nameof(FileInfoContract)} {Id} ({Name}) [{Size}]".ToString(CultureInfo.CurrentCulture);
    }
}
