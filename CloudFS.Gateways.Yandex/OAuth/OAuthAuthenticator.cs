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
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using YandexDisk.Client;
using YandexDisk.Client.Http;
using IgorSoft.CloudFS.Authentication;

namespace IgorSoft.CloudFS.Gateways.Yandex.OAuth
{
    internal static class OAuthAuthenticator
    {
        private const string YANDEX_LOGIN_AUTHORIZE_URI = "https://oauth.yandex.com/authorize";

        private const string YANDEX_LOGIN_TOKEN_URI = "https://oauth.yandex.com/verification_code";

        private static BrowserLogOn logOn;

        private static string LoadRefreshToken(string account)
        {
            var refreshTokens = Settings.Default.RefreshTokens;
            if (refreshTokens != null)
                foreach (RefreshTokenSetting setting in refreshTokens)
                    if (setting.Account == account)
                        return setting.Token;

            return  null;
        }

        private static void SaveRefreshToken(string account, string refreshToken)
        {
            var refreshTokens = Settings.Default.RefreshTokens;
            if (refreshTokens != null) {
                foreach (RefreshTokenSetting setting in refreshTokens)
                    if (setting.Account == account) {
                        refreshTokens.Remove(setting);
                        break;
                    }
            } else {
                refreshTokens = Settings.Default.RefreshTokens = new System.Collections.ObjectModel.Collection<RefreshTokenSetting>();
            }

            refreshTokens.Insert(0, new RefreshTokenSetting() { Account = account, Token = refreshToken });

            Settings.Default.Save();
        }

        private static Uri GetAuthenticationUri(string clientId)
        {
            var stringBuilder = new StringBuilder()
                .Append($"client_id={clientId}")
                .Append("&response_type=token")
                .Append("&display=popup");

            return new UriBuilder(YANDEX_LOGIN_AUTHORIZE_URI) { Query = stringBuilder.ToString() }.Uri;
        }

        private static string GetAuthCode(string account, Uri authenticationUri, Uri redirectUri)
        {
            string oauth_token = null;

            if (logOn == null) {
                logOn = new BrowserLogOn(AsyncOperationManager.SynchronizationContext);
                logOn.Authenticated += (s, e) => oauth_token = e.Parameters["access_token"];
            }

            logOn.Show("Yandex", account, authenticationUri, redirectUri);

            return oauth_token;
        }

        public static Task<IDiskApi> Login(string account, string code)
        {
            if (string.IsNullOrEmpty(account))
                throw new ArgumentNullException(nameof(account));

            var refreshToken = LoadRefreshToken(account);

            if (string.IsNullOrEmpty(refreshToken) && string.IsNullOrEmpty(code)) {
                var authenticationUri = GetAuthenticationUri(Secrets.CLIENT_ID);
                code = GetAuthCode(account, authenticationUri, new Uri(YANDEX_LOGIN_TOKEN_URI));

                if (string.IsNullOrEmpty(code))
                    throw new AuthenticationException(string.Format(CultureInfo.CurrentCulture, Resources.RetrieveAuthenticationCodeFromUri, authenticationUri.ToString()));

                refreshToken = code;
            }

            SaveRefreshToken(account, refreshToken);

            return Task.FromResult<IDiskApi>(new DiskHttpApi(refreshToken));
        }
    }
}
