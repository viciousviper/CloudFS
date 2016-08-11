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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace IgorSoft.CloudFS.Authentication
{
    /// <summary>
    /// Allows interactive user logOn in a web browser.
    /// </summary>
    /// <seealso cref="LogOnBase" />
    public sealed class BrowserLogOn : LogOnBase
    {
        private WebBrowser browser;

        /// <summary>
        /// Initializes a new instance of the <see cref="BrowserLogOn"/> class.
        /// </summary>
        /// <param name="synchonizationContext">The synchonization context.</param>
        public BrowserLogOn(SynchronizationContext synchonizationContext) : base(synchonizationContext)
        {
        }

        /// <summary>
        /// Shows the logOn web browser.
        /// </summary>
        /// <param name="serviceName">Name of the authenticating cloud service.</param>
        /// <param name="account">The usser account.</param>
        /// <param name="authenticationUri">The authentication URI.</param>
        /// <param name="redirectUri">The redirect URI.</param>
        /// <remarks>Please see the authentication protocol documentation of the respective cloud service for details on the correct choice of <paramref name="authenticationUri"/> and <paramref name="redirectUri"/>.</remarks>
        public void Show(string serviceName, string account, Uri authenticationUri, Uri redirectUri)
        {
            base.Show(serviceName, account,
                () => {
                    var window = new Window();
                    browser = new WebBrowser();
                    browser.Loaded += (s, e) => {
                        browser.Navigate(authenticationUri);
                    };
                    browser.Navigating += (s, e) => {
                        if (redirectUri.IsBaseOf(e.Uri) && redirectUri.AbsolutePath == e.Uri.AbsolutePath) {
                            var parameters = new NameValueCollection();
                            foreach (var parameter in e.Uri.Query.TrimStart('?').Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries)) {
                                var nameValue = parameter.Split('=');
                                parameters.Add(nameValue[0], nameValue[1]);
                            }
                            foreach (var parameter in e.Uri.Fragment.TrimStart('#').Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries)) {
                                var nameValue = parameter.Split('=');
                                parameters.Add(nameValue[0], nameValue[1]);
                            }

                            OnAuthenticated(parameters);

                            e.Cancel = true;
                            window.Close();
                        }
                    };
                    browser.Navigated += (s, e) => {
                        if (authenticationUri.IsBaseOf(e.Uri))
                            SetForegroundWindow(new WindowInteropHelper(window).Handle);
                    };
                    window.Content = browser;
                    return window;
                },
                window => browser.Navigate(authenticationUri)
            );
        }
    }
}
