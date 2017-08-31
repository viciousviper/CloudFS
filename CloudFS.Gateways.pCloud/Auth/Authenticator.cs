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
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;
using pCloud.NET;
using IgorSoft.CloudFS.Authentication;

namespace IgorSoft.CloudFS.Gateways.pCloud.Auth
{
    internal static class Authenticator
    {
        private static DirectLogOn logOn;

        private static string LoadRefreshToken(string account, string settingsPassPhrase)
        {
            var refreshTokens = Properties.Settings.Default.RefreshTokens;
            var setting = refreshTokens?.SingleOrDefault(s => s.Account == account);
            return setting?.RefreshToken.DecryptUsing(settingsPassPhrase);
        }

        private static void SaveRefreshToken(string account, string refreshToken, string settingsPassPhrase)
        {
            var refreshTokens = Properties.Settings.Default.RefreshTokens;
            if (refreshTokens != null) {
                var setting = refreshTokens.SingleOrDefault(s => s.Account == account);
                if (setting != null)
                    refreshTokens.Remove(setting);
            } else {
                refreshTokens = Properties.Settings.Default.RefreshTokens = new System.Collections.ObjectModel.Collection<RefreshTokenSetting>();
            }

            refreshTokens.Insert(0, new RefreshTokenSetting() { Account = account, RefreshToken = refreshToken.EncryptUsing(settingsPassPhrase) });

            Properties.Settings.Default.Save();
        }

        public static string GetAuthCode(string account)
        {
            string authCode = null;

            if (logOn == null)
                logOn = new DirectLogOn(AsyncOperationManager.SynchronizationContext);

            EventHandler<AuthenticatedEventArgs> callback = (s, e) => authCode = string.Join(",", e.Parameters.Get("account"), e.Parameters.Get("password"));
            logOn.Authenticated += callback;

            logOn.Show("pCloud", account);

            logOn.Authenticated -= callback;

            return authCode;
        }

        public static async Task<pCloudClient> LoginAsync(string account, string code, string settingsPassPhrase)
        {
            if (string.IsNullOrEmpty(account))
                throw new ArgumentNullException(nameof(account));

            pCloudClient client;

            var refreshToken = LoadRefreshToken(account, settingsPassPhrase);

            if (refreshToken != null) {
                client = pCloudClient.FromAuthToken(refreshToken);
            } else {
                if (string.IsNullOrEmpty(code))
                    code = GetAuthCode(account);

                var parts = code?.Split(new[] { ',' }, 2) ?? Array.Empty<string>();
                if (parts.Length != 2)
                    throw new AuthenticationException(string.Format(CultureInfo.CurrentCulture, Properties.Resources.ProvideAuthenticationData, account));

                client = await pCloudClient.CreateClientAsync(parts[0], parts[1]);
            }

            SaveRefreshToken(account, client.AuthToken, settingsPassPhrase);

            return client;
        }

        public static void PurgeRefreshToken(string account)
        {
            var refreshTokens = Properties.Settings.Default.RefreshTokens;
            if (refreshTokens == null)
                return;

            var settings = refreshTokens.Where(s => account == null || s.Account == account).ToArray();
            foreach (var setting in settings)
                refreshTokens.Remove(setting);
            if (!refreshTokens.Any())
                Properties.Settings.Default.RefreshTokens = null;
            Properties.Settings.Default.Save();
        }
    }
}
