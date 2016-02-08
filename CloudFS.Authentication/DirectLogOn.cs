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
    public class DirectLogOn
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass")]
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private static LogOnWindow window;

        private static EventWaitHandle waitHandle;

        public void Show(string serviceName, string account)
        {
            if (window == null) {
                waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

                UIThread.GetDispatcher().Invoke(() => {
                    window = new LogOnWindow() { Title = $"{serviceName} authentication - {account}" };
                    window.Closing += (s, e) => {
                        window.Hide();
                        waitHandle.Set();
                        e.Cancel = true;
                    };

                    window.btnAuthenticate.Click += (s, e) => {
                        var parameters = new NameValueCollection();
                        parameters.Add("account", window.tbAccount.Text);
                        parameters.Add("password", window.pbPassword.Password);

                        var handler = Authenticated;
                        handler?.Invoke(this, new AuthenticatedEventArgs(parameters));
                    };

                    window.Show();
                });
            } else {
                window.Dispatcher.Invoke(() => {
                    window.Title = $"{serviceName} authentication - {account}";
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
