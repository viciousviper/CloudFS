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
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace IgorSoft.CloudFS.Interface.Composition
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public sealed class CloudGatewayMetadata
    {
        private IDictionary<string, object> values;

        public string CloudService
        {
            get {
                object result = null;
                return values.TryGetValue(nameof(CloudService), out result) ? result as string : null;
            }
        }

        public GatewayCapabilities Capabilities
        {
            get {
                object result = null;
                return values.TryGetValue(nameof(Capabilities), out result) ? (GatewayCapabilities)result : GatewayCapabilities.None;
            }
        }

        public Uri ServiceUri
        {
            get {
                object result = null;
                return values.TryGetValue(nameof(ServiceUri), out result) ? new Uri(result as string) : null;
            }
        }

        public AssemblyName ApiAssembly
        {
            get {
                object result = null;
                return values.TryGetValue(nameof(ApiAssembly), out result) ? AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == result as string)?.GetName() : null;
            }
        }

        public CloudGatewayMetadata(IDictionary<string, object> values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            this.values = values;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private string DebuggerDisplay() => $"{nameof(CloudGatewayMetadata)} '{CloudService}' Capabilities={Capabilities} ServiceUri='{ServiceUri}' ApiAssembly='{ApiAssembly}'".ToString(CultureInfo.CurrentCulture);
    }
}
