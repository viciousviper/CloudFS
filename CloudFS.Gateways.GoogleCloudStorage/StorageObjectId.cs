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
using System.Text.RegularExpressions;
using SemanticTypes;

namespace IgorSoft.CloudFS.Gateways.GoogleCloudStorage
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay(),nq}")]
    internal sealed class StorageObjectId : SemanticType<string>
    {
        private static Regex idRegex = new Regex(@"(?<Bucket>[-0-9a-z]+)/(?<Path>.*)/(?<Generation>\d+)", RegexOptions.Compiled);

        private int indexOfName;

        public string Bucket { get; }

        public string Path { get; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public long Generation { get; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string Directory => Path.Substring(0, indexOfName);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string Name => Path.Substring(indexOfName + 1);

        public StorageObjectId(string id) : base(i => idRegex.IsMatch(i), id)
        {
            var match = idRegex.Match(id);

            Bucket = match.Groups["Bucket"].Value;
            Path = match.Groups["Path"].Value;
            Generation = long.Parse(match.Groups["Generation"].Value);

            indexOfName = Path.TrimEnd(System.IO.Path.AltDirectorySeparatorChar).LastIndexOf(System.IO.Path.AltDirectorySeparatorChar);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private string DebuggerDisplay() => $"{nameof(StorageObjectId)} {Bucket}:{Path} [{Generation}]";
    }
}
