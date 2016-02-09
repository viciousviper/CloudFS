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
using System.Windows.Threading;

namespace IgorSoft.CloudFS.Authentication
{
    public sealed class BrowserLogOn : LogOnBase
    {
        private WebBrowser browser;

        public BrowserLogOn(SynchronizationContext synchonizationContext) : base(synchonizationContext)
        {
        }

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
