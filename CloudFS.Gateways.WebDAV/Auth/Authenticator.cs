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
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Threading.Tasks;
using WebDav;
using IgorSoft.CloudFS.Authentication;

namespace IgorSoft.CloudFS.Gateways.WebDAV.Auth
{
    internal static class Authenticator
    {
        private static DirectLogOn logOn;

        private static NetworkCredential LoadCredential(string account, string settingsPassPhrase)
        {
            var credentials = Properties.Settings.Default.Credentials;
            var setting = credentials?.SingleOrDefault(s => s.Account == account);
            return setting != null
                ? new NetworkCredential(setting.UserName.DecryptUsing(settingsPassPhrase), setting.Password.DecryptUsing(settingsPassPhrase))
                : null;
        }

        private static void SaveCredential(string account, NetworkCredential credential, string settingsPassPhrase)
        {
            var credentials = Properties.Settings.Default.Credentials;
            if (credentials != null) {
                var setting = credentials.SingleOrDefault(s => s.Account == account);
                if (setting != null)
                    credentials.Remove(setting);
            } else {
                credentials = Properties.Settings.Default.Credentials = new System.Collections.ObjectModel.Collection<CredentialsSetting>();
            }

            credentials.Insert(0, new CredentialsSetting() { Account = account, UserName = credential.UserName.EncryptUsing(settingsPassPhrase), Password = credential.Password.EncryptUsing(settingsPassPhrase) });

            Properties.Settings.Default.Save();
        }

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

        public static Task<WebDavClient> LoginAsync(string account, string code, Uri baseAddress, string settingsPassPhrase)
        {
            if (string.IsNullOrEmpty(account))
                throw new ArgumentNullException(nameof(account));
            if (baseAddress == null)
                throw new ArgumentNullException(nameof(baseAddress));

            var credential = LoadCredential(account, settingsPassPhrase);

            if (credential == null) {
                if (string.IsNullOrEmpty(code))
                    code = GetAuthCode(account);

                var parts = code?.Split(new[] { ',' }, 2) ?? Array.Empty<string>();
                if (parts.Length != 2)
                    throw new AuthenticationException(string.Format(CultureInfo.CurrentCulture, Properties.Resources.ProvideAuthenticationData, account));

                credential = new NetworkCredential(parts[0], parts[1]);
            }

            SaveCredential(account, credential, settingsPassPhrase);

            var clientParams = new WebDavClientParams() {
                BaseAddress = baseAddress,
                Credentials = credential,
                UseDefaultCredentials = false
            };
            return Task.FromResult(new WebDavClient(clientParams));
        }

        public static void PurgeRefreshToken(string account)
        {
            var credentials = Properties.Settings.Default.Credentials;
            var settings = credentials?.Where(s => account == null || s.Account == account).ToArray();
            foreach (var setting in settings)
                credentials.Remove(setting);
            Properties.Settings.Default.Save();
        }
    }
}
