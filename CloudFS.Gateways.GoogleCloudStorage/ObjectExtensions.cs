/*
The MIT License(MIT)

Copyright(c) 2016 IgorSoft

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
using System.IO;
using IgorSoft.CloudFS.Interface.IO;

namespace IgorSoft.CloudFS.Gateways.GoogleCloudStorage
{
    internal static class ObjectExtensions
    {
        private static string PathToName(string path) => path.Substring(path.LastIndexOf(Path.AltDirectorySeparatorChar) + 1);

        public static FileSystemInfoContract ToFileSystemInfoContract(this Google.Apis.Storage.v1.Data.Object item)
        {
            return item.Name.EndsWith(Path.AltDirectorySeparatorChar.ToString())
                ? new DirectoryInfoContract(item.Id, PathToName(item.Name.TrimEnd(Path.AltDirectorySeparatorChar)), new DateTimeOffset(item.TimeCreated.Value), new DateTimeOffset(item.Updated.Value)) as FileSystemInfoContract
                : new FileInfoContract(item.Id, PathToName(item.Name), new DateTimeOffset(item.TimeCreated.Value), new DateTimeOffset(item.Updated.Value), (FileSize)(long)item.Size.Value, item.Md5Hash);
        }
    }
}
