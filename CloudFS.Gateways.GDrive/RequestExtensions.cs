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
using Google.Apis.Drive.v3;
using IgorSoft.CloudFS.Interface.IO;

namespace IgorSoft.CloudFS.Gateways.GDrive
{
    internal static class RequestExtensions
    {
        public static AboutResource.GetRequest AsDrive(this AboutResource.GetRequest request)
        {
            request.Fields = "storageQuota,user";
            return request;
        }

        public static FilesResource.CopyRequest AsFileSystem(this FilesResource.CopyRequest request)
        {
            request.Fields = "id,name,createdTime,modifiedTime,size,md5Checksum";
            return request;
        }

        public static FilesResource.CreateRequest AsDirectory(this FilesResource.CreateRequest request)
        {
            request.Fields = "id,name,createdTime,modifiedTime";
            return request;
        }

        public static FilesResource.CreateMediaUpload AsFile(this FilesResource.CreateMediaUpload request)
        {
            request.Fields = "id,name,createdTime,modifiedTime,size,md5Checksum";
            return request;
        }

        public static FilesResource.GetRequest AsRootDirectory(this FilesResource.GetRequest request)
        {
            request.Fields = "id,createdTime,modifiedTime";
            return request;
        }

        public static FilesResource.GetRequest AsFileSystem(this FilesResource.GetRequest request)
        {
            request.Fields = "id,name,createdTime,modifiedTime,size,md5Checksum";
            return request;
        }

        public static FilesResource.GetRequest WithParents(this FilesResource.GetRequest request)
        {
            request.Fields = "parents";
            return request;
        }

        public static FilesResource.ListRequest WithFiles(this FilesResource.ListRequest request, DirectoryId parent)
        {
            request.Fields = "files";
            request.Q = $"'{parent.Value}' in parents";
            return request;
        }

        public static FilesResource.UpdateRequest AsFileSystem(this FilesResource.UpdateRequest request)
        {
            request.Fields = "id,name,createdTime,modifiedTime,size,md5Checksum";
            return request;
        }

        public static FilesResource.UpdateRequest AsFileSystem(this FilesResource.UpdateRequest request, DirectoryId parent, DirectoryId destination)
        {
            request.RemoveParents = parent.Value;
            request.AddParents = destination.Value;
            return request.AsFileSystem();
        }
    }
}
