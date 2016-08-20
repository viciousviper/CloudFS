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
using System.Configuration;

namespace IgorSoft.CloudFS.Authentication.Configuration
{
    /// <summary>
    /// Implements synchronized access to a local .config file on top of the <see cref="LocalFileSettingsProvider"/>.
    /// </summary>
    /// <seealso cref=LocalFileSettingsProvider" />
    public class SynchronizedFileSettingsProvider : LocalFileSettingsProvider
    {
        private static readonly object lockObject = new object();

        /// <summary>
        /// Returns the collection of setting property values for the specified application instance and settings property group.
        /// </summary>
        /// <param name="context">A <see cref="T:System.Configuration.SettingsContext" /> describing the current application usage.</param>
        /// <param name="properties">A <see cref="T:System.Configuration.SettingsPropertyCollection" /> containing the settings property group whose values are to be retrieved.</param>
        /// <returns>
        /// A <see cref="T:System.Configuration.SettingsPropertyValueCollection" /> containing the values for the specified settings property group.
        /// </returns>
        /// <PermissionSet>
        ///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        ///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        ///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="ControlEvidence, ControlPrincipal" />
        /// </PermissionSet>
        public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext context, SettingsPropertyCollection properties)
        {
            lock (lockObject){
                return base.GetPropertyValues(context, properties);
            }
        }

        /// <summary>Sets the values of the specified group of property settings.</summary>
        /// <param name="context">A <see cref="T:System.Configuration.SettingsContext" />, der die aktuelle Anwendungsverwendung beschreibt.</param>
        /// <param name="values">A <see cref="T:System.Configuration.SettingsPropertyValueCollection" /> representing the group of property settings to set.</param>
        /// <PermissionSet>
        ///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        ///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        ///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="ControlEvidence, ControlPrincipal" />
        /// </PermissionSet>
        public override void SetPropertyValues(SettingsContext context, SettingsPropertyValueCollection values)
        {
            lock (lockObject) {
                base.SetPropertyValues(context, values);
            }
        }
    }
}
