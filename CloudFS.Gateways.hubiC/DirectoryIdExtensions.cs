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
using IgorSoft.CloudFS.Interface.IO;

namespace IgorSoft.CloudFS.Gateways.hubiC
{
    internal static class DirectoryIdExtensions
    {
        public static string GetObjectId(this DirectoryId parent, string name) => parent.Value == "/" ? name : parent.Value + "/" + name;

        public static string GetObjectName(this DirectoryId parent, string objectId)
        {
            var parentPath = parent.Value == "/" ? string.Empty : parent.Value + "/";

            if (!(objectId.Length > parentPath.Length && objectId.StartsWith(parentPath, StringComparison.InvariantCulture)))
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.ObjectNotInDirectory, objectId, parent.Value));

            var objectName = objectId.Remove(0, parentPath.Length);

            if (objectName.IndexOf('/') != -1)
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.ObjectNotInDirectory, objectId, parent.Value));

            return objectName;
        }
    }
}
