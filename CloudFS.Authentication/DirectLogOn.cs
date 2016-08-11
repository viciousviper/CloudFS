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
using System.Collections.Specialized;
using System.Threading;

namespace IgorSoft.CloudFS.Authentication
{
    /// <summary>
    /// Allows interactive user logOn in a form.
    /// </summary>
    /// <seealso cref="LogOnBase" />
    public sealed class DirectLogOn : LogOnBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DirectLogOn"/> class.
        /// </summary>
        /// <param name="synchonizationContext">The synchonization context.</param>
        public DirectLogOn(SynchronizationContext synchonizationContext) : base(synchonizationContext)
        {
        }

        /// <summary>
        /// Shows the logOn form.
        /// </summary>
        /// <param name="serviceName">Name of the authenticating cloud service.</param>
        /// <param name="account">The user account.</param>
        public void Show(string serviceName, string account)
        {
            base.Show(serviceName, account,
                () => {
                    var window = new LogOnWindow();
                    window.btnAuthenticate.Click += (s, e) => {
                        var parameters = new NameValueCollection();
                        parameters.Add("account", window.tbAccount.Text);
                        parameters.Add("password", window.pbPassword.Password);

                        OnAuthenticated(parameters);

                        window.Close();
                    };
                    return window;
                },
                window => { }
            );
        }
    }
}
