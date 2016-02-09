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

namespace IgorSoft.CloudFS.Authentication
{
    public abstract class LogOnBase
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass")]
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        protected static extern bool SetForegroundWindow(IntPtr hWnd);

        private EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

        private Window window;

        private SynchronizationContext synchonizationContext;

        protected LogOnBase(SynchronizationContext synchonizationContext)
        {
            this.synchonizationContext = synchonizationContext;
        }

        protected void Show(string serviceName, string account, Func<Window> initialize, Action<Window> open)
        {
            if (string.IsNullOrEmpty(serviceName))
                throw new ArgumentNullException(nameof(serviceName));
            if (string.IsNullOrEmpty(account))
                throw new ArgumentNullException(nameof(account));
            if (initialize == null)
                throw new ArgumentNullException(nameof(initialize));
            if (open == null)
                throw new ArgumentNullException(nameof(open));

            UIThread.GetDispatcher().Invoke(() => {
                if (window == null) {
                    window = initialize();
                    window.Closing += (s, e) => {
                        window.Hide();
                        waitHandle.Set();
                        e.Cancel = true;
                    };
                }

                window.Title = $"{serviceName} authentication - {account}";
                open(window);
                window.Show();
            });

            waitHandle.WaitOne();
        }

        public void Close()
        {
            if (window == null)
                throw new InvalidOperationException(Resources.WindowNotInitialized);

            window.Close();
        }

        protected void OnAuthenticated(NameValueCollection parameters)
        {
            var handler = Authenticated;
            synchonizationContext.Post(state => handler?.Invoke(this, (AuthenticatedEventArgs)state), new AuthenticatedEventArgs(parameters));
        }

        public event EventHandler<AuthenticatedEventArgs> Authenticated;
    }
}
