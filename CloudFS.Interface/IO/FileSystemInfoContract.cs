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

namespace IgorSoft.CloudFS.Interface.IO
{
    /// <summary>
    /// The descriptor of a cloud file system object.
    /// </summary>
    public abstract class FileSystemInfoContract
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
        /// Gets the full path of the directory or file.
        /// </summary>
        /// <value>A <see cref="string" /> containing the full path.</value>
        public abstract string FullName { get; }

        /// <summary>
        /// Gets the mode of the directory or file.
        /// </summary>
        /// <value>The <see cref="string" /> containing the mode.</value>
        public abstract string Mode { get; }

        /// <summary>
        /// Gets the creation time of the current file or directory.
        /// </summary>
        /// <value>The creation <see cref="DateTimeOffset" /> of the current <see cref="FileSystemInfoContract"/> object.</value>
        public DateTimeOffset Created { get; }

        /// <summary>
        /// Gets the time when the current file or directory was last written to.
        /// </summary>
        /// <value>The <see cref="DateTimeOffset" /> the current file or directory was last written.</value>
        public DateTimeOffset Updated { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSystemInfoContract"/> class.
        /// </summary>
        /// <param name="id">The unique identifier.</param>
        /// <param name="name">The name.</param>
        /// <param name="created">The creation time.</param>
        /// <param name="updated">The time when the current <see cref="FileSystemInfoContract"/> was last written.</param>
        /// <exception cref="ArgumentNullException">The name or id is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Created or updated is less than the minimum file time.</exception>
        protected FileSystemInfoContract(FileSystemId id, string name, DateTimeOffset created, DateTimeOffset updated)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            if (created < DateTimeOffset.FromFileTime(0))
                throw new ArgumentOutOfRangeException(nameof(created));
            if (updated < DateTimeOffset.FromFileTime(0))
                throw new ArgumentOutOfRangeException(nameof(updated));

            Id = id;
            Name = name;
            Created = created;
            Updated = updated;
        }

        /// <summary>
        /// Returns a <see cref="string"/> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="string"/> that represents this instance.</returns>
        public override string ToString() => FullName;
    }
}
