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
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using SemanticTypes;

namespace IgorSoft.CloudFS.Interface
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public sealed class RootName : SemanticType<string>
    {
        private const string rootNamePattern = @"^(?<Schema>[a-z]+)(@(?<UserName>[_a-zA-Z0-9]+))?(\|(?<Root>.+))?$";

        private static Regex validationRegex = new Regex(rootNamePattern, RegexOptions.Compiled);

        public string Schema { get; }

        public string UserName { get; }

        public string Root { get; }

        public RootName(string name) : base(n => validationRegex.IsMatch(n), name)
        {
            var groups = validationRegex.Match(name).Groups;
            Schema = groups["Schema"].Value;
            UserName = groups["UserName"].Value;
            Root = groups["Root"].Value;
        }

        public RootName(string schema, string userName, string root) : this(Format(schema, userName, root))
        {
        }

        private static string Format(string schema, string userName, string root)
        {
            var builder = new StringBuilder(schema);
            if (!string.IsNullOrEmpty(userName)) {
                builder.Append("@");
                builder.Append(userName);
            }
            if (!string.IsNullOrEmpty(root) && root != Path.DirectorySeparatorChar.ToString()) {
                builder.Append("|");
                builder.Append(root);
            }
            return builder.ToString();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Used for DebuggerDisplay")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private string DebuggerDisplay() => $"{nameof(RootName)} {Format(Schema, UserName, Root)}";
    }
}
