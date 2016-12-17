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
using System.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using IgorSoft.CloudFS.Interface;
using IgorSoft.CloudFS.Interface.Composition;
using IgorSoft.CloudFS.Interface.IO;

namespace IgorSoft.CloudFS.Gateways.File
{
    [ExportAsCloudGateway("File")]
    [ExportMetadata(nameof(CloudGatewayMetadata.CloudService), FileGateway.SCHEMA)]
    [ExportMetadata(nameof(CloudGatewayMetadata.Capabilities), FileGateway.CAPABILITIES)]
    [ExportMetadata(nameof(CloudGatewayMetadata.ApiAssembly), FileGateway.API)]
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public sealed class FileGateway : ICloudGateway
    {
        private const string SCHEMA = "file";

        private const GatewayCapabilities CAPABILITIES = GatewayCapabilities.All ^ GatewayCapabilities.ItemId;

        private const string API = "mscorlib";

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "PARAMETER")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "ROOT")]
        public const string PARAMETER_ROOT = "root";

        private string rootPath;

        public bool TryAuthenticate(RootName root, string apiKey, IDictionary<string, string> parameters)
        {
            return true;
        }

        public DriveInfoContract GetDrive(RootName root, string apiKey, IDictionary<string, string> parameters)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            if (parameters?.TryGetValue(PARAMETER_ROOT, out rootPath) != true)
                throw new ArgumentException($"Required {PARAMETER_ROOT} missing in {nameof(parameters)}".ToString(CultureInfo.CurrentCulture));
            if (string.IsNullOrEmpty(rootPath))
                throw new ArgumentException($"{PARAMETER_ROOT} cannot be empty".ToString(CultureInfo.CurrentCulture));

            var drive = new DriveInfo(Path.GetFullPath(rootPath));
            return new DriveInfoContract(root.Value, drive.AvailableFreeSpace, drive.TotalSize - drive.AvailableFreeSpace);
        }

        public RootDirectoryInfoContract GetRoot(RootName root, string apiKey, IDictionary<string, string> parameters)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            if (string.IsNullOrEmpty(rootPath))
                throw new InvalidOperationException($"{nameof(rootPath)} not initialized".ToString(CultureInfo.CurrentCulture));

            var directory = new DirectoryInfo(Path.GetFullPath(rootPath));
            return new RootDirectoryInfoContract(Path.DirectorySeparatorChar.ToString(), directory.CreationTime, directory.LastWriteTime);
        }

        private static string GetFullPath(string rootPath, string path)
        {
            if (Path.IsPathRooted(path))
                path = path.Remove(0, Path.GetPathRoot(path).Length);
            return Path.Combine(Path.GetFullPath(rootPath), path);
        }

        private static string GetRelativePath(string rootPath, string path)
        {
            var fullRootPath = Path.GetFullPath(rootPath);
            if (path.StartsWith(fullRootPath, StringComparison.Ordinal))
                path = path.Remove(0, fullRootPath.Length);
            return path.TrimEnd(Path.DirectorySeparatorChar);
        }

        public IEnumerable<FileSystemInfoContract> GetChildItem(RootName root, DirectoryId parent)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));
            if (string.IsNullOrEmpty(rootPath))
                throw new InvalidOperationException($"{nameof(rootPath)} not initialized".ToString(CultureInfo.CurrentCulture));

            var effectivePath = GetFullPath(rootPath, parent.Value);

            var directory = new DirectoryInfo(effectivePath);
            if (directory.Exists)
                return directory.EnumerateDirectories().Select(d => new DirectoryInfoContract(GetRelativePath(rootPath, d.FullName), d.Name, d.CreationTime, d.LastWriteTime)).Cast<FileSystemInfoContract>().Concat(
                    directory.EnumerateFiles().Select(f => new FileInfoContract(GetRelativePath(rootPath, f.FullName), f.Name, f.CreationTime, f.LastWriteTime, (FileSize)f.Length, null)).Cast<FileSystemInfoContract>());
            else
                return Array.Empty<FileSystemInfoContract>();
        }

        public void ClearContent(RootName root, FileId target)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (string.IsNullOrEmpty(rootPath))
                throw new InvalidOperationException($"{nameof(rootPath)} not initialized".ToString(CultureInfo.CurrentCulture));

            var effectivePath = GetFullPath(rootPath, target.Value);

            var file = new FileInfo(effectivePath);
            if (!file.Exists)
                throw new FileNotFoundException(string.Empty, target.Value);

            using (var stream = file.Open(FileMode.Truncate, FileAccess.Write)) {
                stream.Flush();
            }
        }

        public Stream GetContent(RootName root, FileId source)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (string.IsNullOrEmpty(rootPath))
                throw new InvalidOperationException($"{nameof(rootPath)} not initialized".ToString(CultureInfo.CurrentCulture));

            var effectivePath = GetFullPath(rootPath, source.Value);

            var file = new FileInfo(effectivePath);
            if (!file.Exists)
                throw new FileNotFoundException(string.Empty, source.Value);

            return new BufferedStream(file.OpenRead());
        }

        public void SetContent(RootName root, FileId target, Stream content, IProgress<ProgressValue> progress)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (content == null)
                throw new ArgumentNullException(nameof(content));
            if (string.IsNullOrEmpty(rootPath))
                throw new InvalidOperationException($"{nameof(rootPath)} not initialized".ToString(CultureInfo.CurrentCulture));

            var effectivePath = GetFullPath(rootPath, target.Value);

            var file = new FileInfo(effectivePath);
            //using (var stream = file.OpenWrite()) {
            //    content.CopyTo(stream);
            //}

            // HACK: Retry opening the FileStream once if an IOException with HResult == 0x80070020 is thrown.
            //
            var fileStream = default(FileStream);
            try {
                fileStream = file.OpenWrite();
            } catch (IOException ex) when ((uint)ex.HResult == 0x80070020) {
                System.Threading.Thread.Sleep(1);
            }

            using (var stream = fileStream ?? file.OpenWrite()) {
                content.CopyTo(stream);
            }
            //
            // END HACK
        }

        public FileSystemInfoContract CopyItem(RootName root, FileSystemId source, string copyName, DirectoryId destination, bool recurse)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (string.IsNullOrEmpty(copyName))
                throw new ArgumentNullException(nameof(copyName));
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            if (string.IsNullOrEmpty(rootPath))
                throw new InvalidOperationException($"{nameof(rootPath)} not initialized".ToString(CultureInfo.CurrentCulture));

            var effectivePath = GetFullPath(rootPath, source.Value);
            var destinationPath = destination.Value;
            if (Path.IsPathRooted(destinationPath))
                destinationPath = destinationPath.Remove(0, Path.GetPathRoot(destinationPath).Length);
            var effectiveCopyPath = GetFullPath(rootPath, Path.Combine(destinationPath, copyName));

            var directory = new DirectoryInfo(effectivePath);
            if (directory.Exists) {
                var directoryCopy = directory.CopyTo(effectiveCopyPath, recurse);
                return new DirectoryInfoContract(GetRelativePath(rootPath, directoryCopy.FullName), directoryCopy.Name, directoryCopy.CreationTime, directoryCopy.LastWriteTime);
            }

            var file = new FileInfo(effectivePath);
            if (file.Exists) {
                var fileCopy = file.CopyTo(effectiveCopyPath);
                return new FileInfoContract(GetRelativePath(rootPath, fileCopy.FullName), fileCopy.Name, fileCopy.CreationTime, fileCopy.LastWriteTime, (FileSize)fileCopy.Length, null);
            }

            throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, Properties.Resources.PathNotFound, source.Value));
        }

        public FileSystemInfoContract MoveItem(RootName root, FileSystemId source, string moveName, DirectoryId destination)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (string.IsNullOrEmpty(moveName))
                throw new ArgumentNullException(nameof(moveName));
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            if (string.IsNullOrEmpty(rootPath))
                throw new InvalidOperationException($"{nameof(rootPath)} not initialized".ToString(CultureInfo.CurrentCulture));

            var effectivePath = GetFullPath(rootPath, source.Value);
            var destinationPath = destination.Value;
            if (Path.IsPathRooted(destinationPath))
                destinationPath = destinationPath.Remove(0, Path.GetPathRoot(destinationPath).Length);
            var effectiveMovePath = GetFullPath(rootPath, Path.Combine(destinationPath, moveName));

            var directory = new DirectoryInfo(effectivePath);
            if (directory.Exists) {
                directory.MoveTo(effectiveMovePath);
                return new DirectoryInfoContract(GetRelativePath(rootPath, directory.FullName), directory.Name, directory.CreationTime, directory.LastWriteTime);
            }

            var file = new FileInfo(effectivePath);
            if (file.Exists) {
                file.MoveTo(effectiveMovePath);
                return new FileInfoContract(GetRelativePath(rootPath, file.FullName), file.Name, file.CreationTime, file.LastWriteTime, (FileSize)file.Length, null);
            }

            throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, Properties.Resources.PathNotFound, source.Value));
        }

        public DirectoryInfoContract NewDirectoryItem(RootName root, DirectoryId parent, string name)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrEmpty(rootPath))
                throw new InvalidOperationException($"{nameof(rootPath)} not initialized".ToString(CultureInfo.CurrentCulture));

            var effectivePath = GetFullPath(rootPath, Path.Combine(parent.Value, name));

            var directory = new DirectoryInfo(effectivePath);
            if (directory.Exists)
                throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, Properties.Resources.DuplicatePath, effectivePath));

            directory.Create();

            return new DirectoryInfoContract(GetRelativePath(rootPath, directory.FullName), directory.Name, directory.CreationTime, directory.LastWriteTime);
        }

        public FileInfoContract NewFileItem(RootName root, DirectoryId parent, string name, Stream content, IProgress<ProgressValue> progress)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrEmpty(rootPath))
                throw new InvalidOperationException($"{nameof(rootPath)} not initialized".ToString(CultureInfo.CurrentCulture));

            var effectivePath = GetFullPath(rootPath, Path.Combine(parent.Value, name));

            var file = new FileInfo(effectivePath);
            if (file.Exists)
                throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, Properties.Resources.DuplicatePath, parent.Value));

            using (var fileStream = file.Create()) {
                if (content != null)
                    content.CopyTo(fileStream);
            }

            file.Refresh();
            return new FileInfoContract(GetRelativePath(rootPath, file.FullName), file.Name, file.CreationTime, file.LastWriteTime, (FileSize)file.Length, null);
        }

        public void RemoveItem(RootName root, FileSystemId target, bool recurse)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (string.IsNullOrEmpty(rootPath))
                throw new InvalidOperationException($"{nameof(rootPath)} not initialized".ToString(CultureInfo.CurrentCulture));

            var effectivePath = GetFullPath(rootPath, target.Value);

            var directory = new DirectoryInfo(effectivePath);
            if (directory.Exists) {
                directory.Delete(recurse);
                return;
            }

            var file = new FileInfo(effectivePath);
            if (file.Exists) {
                file.Delete();
                return;
            }

            throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, Properties.Resources.PathNotFound, target.Value));
        }

        public FileSystemInfoContract RenameItem(RootName root, FileSystemId target, string newName)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (string.IsNullOrEmpty(newName))
                throw new ArgumentNullException(nameof(newName));
            if (string.IsNullOrEmpty(rootPath))
                throw new InvalidOperationException($"{nameof(rootPath)} not initialized".ToString(CultureInfo.CurrentCulture));

            var effectivePath = GetFullPath(rootPath, target.Value);
            var newPath = GetFullPath(rootPath, Path.Combine(Path.GetDirectoryName(target.Value), newName));

            var directory = new DirectoryInfo(effectivePath);
            if (directory.Exists) {
                directory.MoveTo(newPath);
                return new DirectoryInfoContract(GetRelativePath(rootPath, directory.FullName), directory.Name, directory.CreationTime, directory.LastWriteTime);
            }

            var file = new FileInfo(effectivePath);
            if (file.Exists) {
                file.MoveTo(newPath);
                return new FileInfoContract(GetRelativePath(rootPath, file.FullName), file.Name, file.CreationTime, file.LastWriteTime, (FileSize)file.Length, null);
            }

            throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, Properties.Resources.PathNotFound, target.Value));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private string DebuggerDisplay() => $"{nameof(FileGateway)} rootPath='{rootPath}'".ToString(CultureInfo.CurrentCulture);
    }
}
