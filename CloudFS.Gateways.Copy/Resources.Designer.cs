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

namespace IgorSoft.CloudFS.Gateways.Copy {
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
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("IgorSoft.CloudFS.Gateways.Copy.Resources", typeof(Resources).Assembly);
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
        ///   Looks up a localized string similar to Copy does not support copying of files or directories.
        /// </summary>
        internal static string CopyingOfFilesOrDirectoriesNotSupported {
            get {
                return ResourceManager.GetString("CopyingOfFilesOrDirectoriesNotSupported", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to The file/folder &apos;{0}&apos; could not be moved to &apos;{1}{2}&apos;.
        /// </summary>
        internal static string MoveFailed {
            get {
                return ResourceManager.GetString("MoveFailed", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to The file/folder &apos;{0}&apos; could not be renamed to &apos;{1}&apos;.
        /// </summary>
        internal static string RenameFailed {
            get {
                return ResourceManager.GetString("RenameFailed", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Please retrieve an authentication code from the following Uri: {0}
        /// </summary>
        internal static string RetrieveAuthenticationCodeFromUri {
            get {
                return ResourceManager.GetString("RetrieveAuthenticationCodeFromUri", resourceCulture);
            }
        }
    }
}
