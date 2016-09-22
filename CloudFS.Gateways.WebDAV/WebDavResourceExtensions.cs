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
using System.Net;
using WebDav;
using IgorSoft.CloudFS.Interface.IO;

namespace IgorSoft.CloudFS.Gateways.WebDAV
{
    internal static class WebDavResourceExtensions
    {
        public static string GetName(this WebDavResource item)
        {
            var normalizedUri = item.Uri.TrimEnd('/');
            var lastSlashIndex = normalizedUri.LastIndexOf('/');
            return normalizedUri.Substring(lastSlashIndex + 1);
        }

        public static FileSystemInfoContract ToFileSystemInfoContract(this WebDavResource item) => item.IsCollection
                ? new DirectoryInfoContract(WebUtility.UrlDecode(item.Uri), WebUtility.UrlDecode(item.GetName()), item.CreationDate ?? DateTimeOffset.FromFileTime(0), item.LastModifiedDate ?? DateTimeOffset.FromFileTime(0)) as FileSystemInfoContract
                : new FileInfoContract(WebUtility.UrlDecode(item.Uri), WebUtility.UrlDecode(item.GetName()), item.CreationDate ?? DateTimeOffset.FromFileTime(0), item.LastModifiedDate ?? DateTimeOffset.FromFileTime(0), item.ContentLength ?? -1, item.ETag) as FileSystemInfoContract;
    }
}
