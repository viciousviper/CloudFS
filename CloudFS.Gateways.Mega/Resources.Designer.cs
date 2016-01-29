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

namespace IgorSoft.CloudFS.Gateways.Mega {
    using System;

    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {

        private static global::System.Resources.ResourceManager resourceMan;

        private static global::System.Globalization.CultureInfo resourceCulture;

        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }

        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("IgorSoft.CloudFS.Gateways.Mega.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }

        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Mega does not support copying of files or directories..
        /// </summary>
        internal static string CopyingOfFilesNotSupported {
            get {
                return ResourceManager.GetString("CopyingOfFilesNotSupported", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Please provide authentication data for account &apos;{0}&apos; as &quot;&lt;email&gt;,&lt;password&gt;&quot;..
        /// </summary>
        internal static string ProvideAuthenticationData {
            get {
                return ResourceManager.GetString("ProvideAuthenticationData", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Mega does not support renaming of files or directories..
        /// </summary>
        internal static string RenamingOfFilesNotSupported {
            get {
                return ResourceManager.GetString("RenamingOfFilesNotSupported", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Mega does not support setting the content of an existing file..
        /// </summary>
        internal static string SettingOfFileContentNotSupported {
            get {
                return ResourceManager.GetString("SettingOfFileContentNotSupported", resourceCulture);
            }
        }
    }
}
