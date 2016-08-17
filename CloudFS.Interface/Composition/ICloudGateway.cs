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
using System.IO;
using IgorSoft.CloudFS.Interface.IO;

namespace IgorSoft.CloudFS.Interface.Composition
{
    /// <summary>
    /// A synchronous gateway to a cloud file system.
    /// </summary>
    public interface ICloudGateway
    {
        /// <summary>
        /// Tries to authenticate the cloud gateway instance to its associated cloud service.
        /// </summary>
        /// <param name="root">The root name identifying a cloud file system instance.</param>
        /// <param name="apiKey">An optional API key authenticating the user.</param>
        /// <param name="parameters">Additional parameters for drive configuration.</param>
        /// <returns><c>true</c> if the authentication succeeded; otherwise <c>false</c>.</returns>
        /// <remarks>If no <paramref name="apiKey"/> is specified, this method will load a stored authentication token, if present, or else prompt the user for credentials.</remarks>
        bool TryAuthenticate(RootName root, string apiKey, IDictionary<string, string> parameters);

        /// <summary>
        /// Gets the drive associated with a cloud file system.
        /// </summary>
        /// <param name="root">The root name identifying a cloud file system instance.</param>
        /// <param name="apiKey">An optional API key authenticating the user.</param>
        /// <param name="parameters">Additional parameters for drive configuration.</param>
        /// <returns>A <see cref="DriveInfoContract"/> representing the drive.</returns>
        /// <remarks>If no <paramref name="apiKey"/> is specified, this method will load a stored authentication token, if present, or else prompt the user for credentials.</remarks>
        DriveInfoContract GetDrive(RootName root, string apiKey, IDictionary<string, string> parameters);

        /// <summary>
        /// Gets the root directory.
        /// </summary>
        /// <param name="root">The root name identifying a cloud file system instance.</param>
        /// <param name="apiKey">An optional API key authenticating the user.</param>
        /// <param name="parameters">Additional parameters for drive configuration.</param>
        /// <returns>A <see cref="RootDirectoryInfoContract"/> representing the root directory.</returns>
        /// <remarks>If no <paramref name="apiKey"/> is specified, this method will load a stored authentication token, if present, or else prompt the user for credentials.</remarks>
        RootDirectoryInfoContract GetRoot(RootName root, string apiKey, IDictionary<string, string> parameters);

        /// <summary>
        /// Gets the child items of the specified parent directory.
        /// </summary>
        /// <param name="root">The root name identifying a cloud file system instance.</param>
        /// <param name="parent">The parent directory info.</param>
        /// <returns>An enumeration of <see cref="FileSystemInfoContract"/>s representing the child items.</returns>
        IEnumerable<FileSystemInfoContract> GetChildItem(RootName root, DirectoryId parent);

        /// <summary>
        /// Clears the content of the specified file.
        /// </summary>
        /// <param name="root">The root name identifying a cloud file system instance.</param>
        /// <param name="target">The target file ID.</param>
        void ClearContent(RootName root, FileId target);

        /// <summary>
        /// Creates a read-only <see cref="Stream"/> object for the the content of the specified file.
        /// </summary>
        /// <param name="root">The root name identifying a cloud file system instance.</param>
        /// <param name="source">The source file ID.</param>
        /// <returns>A read-only <see cref="Stream"/> object for the the content of the specified file.</returns>
        Stream GetContent(RootName root, FileId source);

        /// <summary>
        /// Sets the content of the specified file from the specified <see cref="Stream"/>.
        /// </summary>
        /// <param name="root">The root name identifying a cloud file system instance.</param>
        /// <param name="target">The target file ID.</param>
        /// <param name="content">A <see cref="Stream"/> object providing the content.</param>
        /// <param name="progress">A provider for progress updates.</param>
        void SetContent(RootName root, FileId target, Stream content, IProgress<ProgressValue> progress);

        /// <summary>
        /// Copies and optionally renames the specified cloud file system object to the specified destination directory.
        /// </summary>
        /// <param name="root">The root name identifying a cloud file system instance.</param>
        /// <param name="source">The source file system object ID.</param>
        /// <param name="copyName">The name of the copy.</param>
        /// <param name="destination">The destination directory ID.</param>
        /// <param name="recurse">if set to <c>true</c>, copies child items recursively.</param>
        /// <returns>A <see cref="FileSystemInfoContract"/> representing the copied cloud file system object.</returns>
        /// <remarks>A file system object cannot be copied to a different cloud drive.</remarks>
        FileSystemInfoContract CopyItem(RootName root, FileSystemId source, string copyName, DirectoryId destination, bool recurse);

        /// <summary>
        /// Moves and optionally renames the specified cloud file system object to the specified destination directory.
        /// </summary>
        /// <param name="root">The root name identifying a cloud file system instance.</param>
        /// <param name="source">The source file system object ID.</param>
        /// <param name="moveName">The name of the moved file system object.</param>
        /// <param name="destination">The destination directory ID.</param>
        /// <returns>A <see cref="FileSystemInfoContract"/> representing the moved cloud file system object.</returns>
        /// <remarks>A file system object cannot be moved to a different cloud drive.</remarks>
        FileSystemInfoContract MoveItem(RootName root, FileSystemId source, string moveName, DirectoryId destination);

        /// <summary>
        /// Creates a new cloud directory.
        /// </summary>
        /// <param name="root">The root name identifying a cloud file system instance.</param>
        /// <param name="parent">The parent directory ID.</param>
        /// <param name="name">The name.</param>
        /// <returns>A <see cref="DirectoryInfoContract"/> representing the new directory.</returns>
        DirectoryInfoContract NewDirectoryItem(RootName root, DirectoryId parent, string name);

        /// <summary>
        /// Creates a new cloud file.
        /// </summary>
        /// <param name="root">The root name identifying a cloud file system instance.</param>
        /// <param name="parent">The parent directory ID.</param>
        /// <param name="name">The name.</param>
        /// <param name="content">A <see cref="Stream"/> object providing the content.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A <see cref="FileInfoContract"/> representing the new file.</returns>
        FileInfoContract NewFileItem(RootName root, DirectoryId parent, string name, Stream content, IProgress<ProgressValue> progress);

        /// <summary>
        /// Removes the specified cloud file system object.
        /// </summary>
        /// <param name="root">The root name identifying a cloud file system instance.</param>
        /// <param name="target">The target file system object ID.</param>
        /// <param name="recurse">if set to <c>true</c>, removes a non-empty directory with its child items recursively.</param>
        void RemoveItem(RootName root, FileSystemId target, bool recurse);

        /// <summary>
        /// Renames the specified cloud file system object.
        /// </summary>
        /// <param name="root">The root name identifying a cloud file system instance.</param>
        /// <param name="target">The target file system object ID.</param>
        /// <param name="newName">The new name.</param>
        /// <returns>A <see cref="FileSystemInfoContract"/> representing the renamed file system object.</returns>
        FileSystemInfoContract RenameItem(RootName root, FileSystemId target, string newName);
    }
}
