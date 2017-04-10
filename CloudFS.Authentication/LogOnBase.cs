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
using System.Globalization;
using System.Threading;
using System.Windows;

namespace IgorSoft.CloudFS.Authentication
{
    /// <summary>
    /// Base class for interactive user logOn.
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public abstract class LogOnBase : IDisposable
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass")]
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);

        private readonly EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

        private Window window;

        private readonly SynchronizationContext synchonizationContext;

        /// <summary>
        /// Occurs when the user has been authenticated.
        /// </summary>
        public event EventHandler<AuthenticatedEventArgs> Authenticated;

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

                window.Title = $"{serviceName} authentication - {account}".ToString(CultureInfo.CurrentCulture);
                open(window);
                window.Show();
            });

            waitHandle.WaitOne();
        }

        /// <summary>
        /// Closes the logOn window.
        /// </summary>
        /// <exception cref="InvalidOperationException">The window has not been initialized.</exception>
        public void Close()
        {
            if (window == null)
                throw new InvalidOperationException(Properties.Resources.WindowNotInitialized);

            window.Close();
        }

        protected void OnAuthenticated(NameValueCollection parameters)
        {
            var handler = Authenticated;
            synchonizationContext.Post(state => handler?.Invoke(this, (AuthenticatedEventArgs)state), new AuthenticatedEventArgs(parameters));
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Implements resource cleanup.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                waitHandle.Dispose();
            }
        }
    }
}
