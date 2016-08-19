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
using System.Threading.Tasks;
using IgorSoft.CloudFS.Authentication;
using static IgorSoft.CloudFS.Authentication.OAuth.Constants;
using Box.V2;
using Box.V2.Config;
using Box.V2.Auth;
using Box.V2.Exceptions;

namespace IgorSoft.CloudFS.Gateways.Box.OAuth
{
    internal static class OAuthAuthenticator
    {
        private const string BOX_LOGIN_DESKTOP_URI = "https://localhost/box_login";

        private const string BOX_LOGIN_AUTHORIZE_URI = "https://app.box.com/api/oauth2/authorize";

        private const string BOX_LOGIN_TOKEN_URI = "https://app.box.com/api/oauth2/token";

        private const string BOX_API_URI = "https://api.box.com/2.0";

        private static BrowserLogOn logOn;

        private static string LoadRefreshToken(string account, string settingsPassPhrase)
        {
            var refreshTokens = Properties.Settings.Default.RefreshTokens;
            if (refreshTokens != null)
                foreach (RefreshTokenSetting setting in refreshTokens)
                    if (setting.Account == account)
                        return setting.RefreshToken.DecryptUsing(settingsPassPhrase);

            return null;
        }

        private static void SaveRefreshToken(string account, string refreshToken, string settingsPassPhrase)
        {
            var refreshTokens = Properties.Settings.Default.RefreshTokens;
            if (refreshTokens != null) {
                foreach (RefreshTokenSetting setting in refreshTokens)
                    if (setting.Account == account) {
                        refreshTokens.Remove(setting);
                        break;
                    }
            } else {
                refreshTokens = Properties.Settings.Default.RefreshTokens = new System.Collections.ObjectModel.Collection<OAuth.RefreshTokenSetting>();
            }

            refreshTokens.Insert(0, new RefreshTokenSetting() { Account = account, RefreshToken = refreshToken.EncryptUsing(settingsPassPhrase) });

            Properties.Settings.Default.Save();
        }

        private static string GetAuthCode(string account, Uri authenticationUri, Uri redirectUri)
        {
            string authCode = null;

            if (logOn == null)
                logOn = new BrowserLogOn(AsyncOperationManager.SynchronizationContext);

            EventHandler<AuthenticatedEventArgs> callback = (s, e) => authCode = e.Parameters[Parameters.Code];
            logOn.Authenticated += callback;

            logOn.Show("Box", account, authenticationUri, redirectUri);

            logOn.Authenticated -= callback;

            return authCode;
        }

        public static async Task<BoxClient> LoginAsync(string account, string code, string settingsPassPhrase)
        {
            if (string.IsNullOrEmpty(account))
                throw new ArgumentNullException(nameof(account));

            var refreshToken = LoadRefreshToken(account, settingsPassPhrase);

            var client = default(BoxClient);
            var config = new BoxConfig(Secrets.CLIENT_ID, Secrets.CLIENT_SECRET, new Uri(BOX_LOGIN_DESKTOP_URI));

            var response = default(OAuthSession);
            if (!string.IsNullOrEmpty(refreshToken)) {
                client = new BoxClient(config, new OAuthSession(null, refreshToken, 0, TokenTypes.Bearer));
                try {
                    response = await client.Auth.RefreshAccessTokenAsync(refreshToken);
                } catch (BoxSessionInvalidatedException) {
                }
            }

            if (response == null) {
                client = new BoxClient(config, (OAuthSession)null);
                if (string.IsNullOrEmpty(code)) {
                    var authenticationUri = client.Config.AuthCodeUri;
                    code = GetAuthCode(account, authenticationUri, new Uri(BOX_LOGIN_DESKTOP_URI));
                    if (string.IsNullOrEmpty(code))
                        throw new AuthenticationException(string.Format(CultureInfo.CurrentCulture, Properties.Resources.RetrieveAuthenticationCodeFromUri, authenticationUri.ToString()));
                }

                response = await client.Auth.AuthenticateAsync(code);
            }

            SaveRefreshToken(account, response?.RefreshToken ?? refreshToken, settingsPassPhrase);

            return client;
        }
    }
}
