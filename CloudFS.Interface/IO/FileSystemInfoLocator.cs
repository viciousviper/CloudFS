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
    /// An association of a file system object ID with its name.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public sealed class FileSystemInfoLocator
    {
        /// <summary>
        /// Gets the unique file system object identifier.
        /// </summary>
        /// <value>The unique file system object identifier.</value>
        public FileSystemId Id { get; }

        /// <summary>
        /// For files, gets the name of the file. For directories, gets the name of the last directory in the hierarchy if a hierarchy exists.
        /// Otherwise, the <paramref name="Name"/ > property gets the name of the directory.
        /// </summary>
        /// <value>A <see cref="string" /> that is the name of the parent directory, the name of the last directory in the hierarchy, or the name of a file, including the file name extension.</value>
        public string Name { get; }

        /// <summary>
        /// Gets the parent directory ID.
        /// </summary>
        /// <value>The parent directory ID.</value>
        public DirectoryId ParentId { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSystemInfoLocator"/> class.
        /// </summary>
        /// <param name="fileSystem">The file system info object.</param>
        /// <exception cref="ArgumentNullException">The filesystem is <c>null</c>.</exception>
        public FileSystemInfoLocator(FileSystemInfoContract fileSystem)
        {
            if (fileSystem == null)
                throw new ArgumentNullException(nameof(fileSystem));

            Id = fileSystem.Id;
            Name = fileSystem.Name;
            ParentId = (fileSystem as DirectoryInfoContract)?.Parent.Id ?? (fileSystem as FileInfoContract)?.Directory.Id;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Used for DebuggerDisplay")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private string DebuggerDisplay() => $"{nameof(FileSystemInfoLocator)} {Id.Value} Name={Name} ParentId={ParentId.Value}".ToString(CultureInfo.CurrentCulture);
    }
}
