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
    public interface ICloudGateway
    {
        bool TryAuthenticate(RootName root, string apiKey);

        DriveInfoContract GetDrive(RootName root, string apiKey, IDictionary<string, string> parameters);

        RootDirectoryInfoContract GetRoot(RootName root, string apiKey);

        IEnumerable<FileSystemInfoContract> GetChildItem(RootName root, DirectoryId parent);

        void ClearContent(RootName root, FileId target);

        Stream GetContent(RootName root, FileId source);

        void SetContent(RootName root, FileId target, Stream content, IProgress<ProgressValue> progress);

        FileSystemInfoContract CopyItem(RootName root, FileSystemId source, string copyName, DirectoryId destination, bool recurse);

        FileSystemInfoContract MoveItem(RootName root, FileSystemId source, string moveName, DirectoryId destination);

        DirectoryInfoContract NewDirectoryItem(RootName root, DirectoryId parent, string name);

        FileInfoContract NewFileItem(RootName root, DirectoryId parent, string name, Stream content, IProgress<ProgressValue> progress);

        void RemoveItem(RootName root, FileSystemId target, bool recurse);

        FileSystemInfoContract RenameItem(RootName root, FileSystemId target, string newName);
    }
}
