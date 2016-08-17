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
using System.ComponentModel;
using System.Globalization;
using System.Net;
using System.Security.Authentication;
using System.Threading.Tasks;
using WebDav;
using IgorSoft.CloudFS.Authentication;
using IgorSoft.CloudFS.Gateways.WebDAV.Properties;

namespace IgorSoft.CloudFS.Gateways.WebDAV.Auth
{
    internal static class Authenticator
    {
        private static DirectLogOn logOn;

        public static string GetAuthCode(string account)
        {
            string authCode = null;

            if (logOn == null)
                logOn = new DirectLogOn(AsyncOperationManager.SynchronizationContext);

            EventHandler<AuthenticatedEventArgs> callback = (s, e) => authCode = string.Join(",", e.Parameters.Get("account"), e.Parameters.Get("password"));
            logOn.Authenticated += callback;

            logOn.Show("WebDAV", account);

            logOn.Authenticated -= callback;

            return authCode;
        }

        public static async Task<WebDavClient> LoginAsync(string account, string code, Uri baseAddress)
        {
            if (string.IsNullOrEmpty(account))
                throw new ArgumentNullException(nameof(account));
            if (baseAddress == null)
                throw new ArgumentNullException(nameof(baseAddress));

            if (string.IsNullOrEmpty(code))
                code = GetAuthCode(account);

            var parts = code?.Split(new[] { ',' }, 2) ?? Array.Empty<string>();
            if (parts.Length != 2)
                throw new AuthenticationException(string.Format(CultureInfo.CurrentCulture, Resources.ProvideAuthenticationData, account));

            var clientParams = new WebDavClientParams() {
                BaseAddress = baseAddress,
                Credentials = new NetworkCredential(parts[0], parts[1]),
                UseDefaultCredentials = false
            };
            return new WebDavClient(clientParams);
        }
    }
}
