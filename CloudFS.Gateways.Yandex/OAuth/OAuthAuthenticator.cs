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
using YandexDisk.Client;
using YandexDisk.Client.Http;
using IgorSoft.CloudFS.Authentication;
using static IgorSoft.CloudFS.Authentication.OAuth.Constants;

namespace IgorSoft.CloudFS.Gateways.Yandex.OAuth
{
    internal static class OAuthAuthenticator
    {
        private const string YANDEX_LOGIN_AUTHORIZE_URI = "https://oauth.yandex.com/authorize";

        private const string YANDEX_LOGIN_TOKEN_URI = "https://oauth.yandex.com/verification_code";

        private static BrowserLogOn logOn;

        private static string LoadRefreshToken(string account, string settingsPassPhrase)
        {
            var refreshTokens = Properties.Settings.Default.RefreshTokens;
            var setting = refreshTokens?.SingleOrDefault(s => s.Account == account);
            return setting?.Token.DecryptUsing(settingsPassPhrase);
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

            refreshTokens.Insert(0, new RefreshTokenSetting() { Account = account, Token = refreshToken.EncryptUsing(settingsPassPhrase) });

            Properties.Settings.Default.Save();
        }

        private static Uri GetAuthenticationUri(string clientId)
        {
            var queryBuilder = new QueryStringBuilder()
                .AppendParameter(Parameters.ClientId, clientId)
                .AppendParameter(Parameters.ResponseType, ResponseTypes.Token)
                .AppendParameter("display", "popup");

            return new UriBuilder(YANDEX_LOGIN_AUTHORIZE_URI) { Query = queryBuilder.ToString() }.Uri;
        }

        private static string GetAuthCode(string account, Uri authenticationUri, Uri redirectUri)
        {
            string oauth_token = null;

            if (logOn == null)
                logOn = new BrowserLogOn(AsyncOperationManager.SynchronizationContext);

            EventHandler<AuthenticatedEventArgs> callback = (s, e) => oauth_token = e.Parameters[Parameters.AccessToken];
            logOn.Authenticated += callback;

            logOn.Show("Yandex", account, authenticationUri, redirectUri);

            logOn.Authenticated -= callback;

            return oauth_token;
        }

        public static Task<IDiskApi> LoginAsync(string account, string code, string settingsPassPhrase)
        {
            if (string.IsNullOrEmpty(account))
                throw new ArgumentNullException(nameof(account));

            var refreshToken = LoadRefreshToken(account, settingsPassPhrase);

            if (string.IsNullOrEmpty(refreshToken)) {
                if (string.IsNullOrEmpty(code)) {
                    var authenticationUri = GetAuthenticationUri(Secrets.CLIENT_ID);
                    code = GetAuthCode(account, authenticationUri, new Uri(YANDEX_LOGIN_TOKEN_URI));

                    if (string.IsNullOrEmpty(code))
                        throw new AuthenticationException(string.Format(CultureInfo.CurrentCulture, Properties.Resources.RetrieveAuthenticationCodeFromUri, authenticationUri.ToString()));
                }

                refreshToken = code;
            }

            SaveRefreshToken(account, refreshToken, settingsPassPhrase);

            return Task.FromResult<IDiskApi>(new DiskHttpApi(refreshToken));
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
