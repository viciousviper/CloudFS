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
using SemanticTypes;

namespace IgorSoft.CloudFS.Interface.IO
{
#pragma warning disable CS3009 // Basistyp ist nicht CLS-kompatibel
    /// <summary>
    /// The unique identifier of a cloud file system object.
    /// </summary>
    /// <seealso cref="SemanticType{string}" />
    public abstract class FileSystemId : SemanticType<string>
#pragma warning restore CS3009 // Basistyp ist nicht CLS-kompatibel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileSystemId"/> class.
        /// </summary>
        /// <param name="id">The unique identifier.</param>
        protected FileSystemId(string id) : base(i => !string.IsNullOrEmpty(i), id)
        {
        }
    }
}
