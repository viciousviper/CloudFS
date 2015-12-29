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
    public class BrowserLogOn
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass")]
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private static WebBrowser browser;

        private static Window window;

        private static EventWaitHandle waitHandle;

        public void Show(string serviceName, string account, Uri authenticationUri, Uri redirectUri)
        {
            if (window == null) {
                waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
                var uiThread = new Thread(() => {
                    window = new Window() { Title = $"{serviceName} authentication - {account}" };
                    window.Closing += (s, e) => {
                        window.Hide();
                        waitHandle.Set();
                        e.Cancel = true;
                    };

                    browser = new WebBrowser();
                    browser.Loaded += (s, e) => {
                        browser.Navigate(authenticationUri);
                    };
                    browser.Navigating += (s, e) => {
                        if (redirectUri.IsBaseOf(e.Uri) && redirectUri.AbsolutePath == e.Uri.AbsolutePath) {
                            var parameters = new NameValueCollection();
                            foreach (var parameter in e.Uri.Query.TrimStart('?').Split('&')) {
                                var nameValue = parameter.Split('=');
                                parameters.Add(nameValue[0], nameValue[1]);
                            }
                            var handler = Authenticated;
                            handler?.Invoke(this, new AuthenticatedEventArgs(parameters));
                            e.Cancel = true;
                        }
                    };
                    browser.Navigated += (s, e) => {
                        if (authenticationUri.IsBaseOf(e.Uri))
                            SetForegroundWindow(new WindowInteropHelper(window).Handle);
                    };

                    window.Content = browser;
                    window.Show();

                    System.Windows.Threading.Dispatcher.Run();
                });
                uiThread.SetApartmentState(ApartmentState.STA);
                uiThread.Start();
            } else {
                window.Dispatcher.Invoke(() => {
                    browser.Source = authenticationUri;
                    window.Title = account;
                    window.Show();
                });
            }

            waitHandle.WaitOne();
        }

        public void Close()
        {
            window.Hide();
            waitHandle.Set();
        }

        public event EventHandler<AuthenticatedEventArgs> Authenticated;
    }
}
