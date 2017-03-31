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
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;
using Microsoft.OneDrive.Sdk;
using Microsoft.OneDrive.Sdk.Authentication;
using IgorSoft.CloudFS.Authentication;

namespace IgorSoft.CloudFS.Gateways.OneDrive.OAuth
{
    internal static class OAuthAuthenticator
    {
        private class WebAuthenticationUi : IWebAuthenticationUi
        {
            private static BrowserLogOn logOn;

            private readonly string account;

            public WebAuthenticationUi(string account)
            {
                this.account = account;
            }

            public Task<IDictionary<string, string>> AuthenticateAsync(Uri requestUri, Uri callbackUri)
            {
                var result = new Dictionary<string, string>();

                if (logOn == null)
                    logOn = new BrowserLogOn(AsyncOperationManager.SynchronizationContext);

                EventHandler<AuthenticatedEventArgs> callback = (s, e) => {
                    for (int i = 0; i < e.Parameters.Count; ++i)
                        result.Add(e.Parameters.Keys[i], e.Parameters[i]);
                };
                logOn.Authenticated += callback;

                logOn.Show("OneDrive", account, requestUri, callbackUri);

                logOn.Authenticated -= callback;

                return Task.FromResult<IDictionary<string, string>>(result);
            }
        }

        private const string LIVE_LOGIN_DESKTOP_URI = "https://login.live.com/oauth20_desktop.srf";

        private const string LIVE_LOGIN_AUTHORIZE_URI = "https://login.live.com/oauth20_authorize.srf";

        private static readonly string[] scopes = { "wl.basic", "wl.offline_access", "wl.signin", "onedrive.readwrite" };

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
                    refreshTokens.Remove(setting);
            } else {
                refreshTokens = Properties.Settings.Default.RefreshTokens = new System.Collections.ObjectModel.Collection<OAuth.RefreshTokenSetting>();
            }

            refreshTokens.Insert(0, new RefreshTokenSetting() { Account = account, RefreshToken = refreshToken.EncryptUsing(settingsPassPhrase) });

            Properties.Settings.Default.Save();
        }

        public static async Task<IOneDriveClient> LoginAsync(string account, string code, string settingsPassPhrase)
        {
            if (string.IsNullOrEmpty(account))
                throw new ArgumentNullException(nameof(account));

            var refreshToken = LoadRefreshToken(account, settingsPassPhrase);

            var authProvider = new MsaAuthenticationProvider(Secrets.CLIENT_ID, Secrets.CLIENT_SECRET, LIVE_LOGIN_DESKTOP_URI, scopes, default(CredentialCache));
            var oauthHelper = new OAuthHelper();
            if (!string.IsNullOrEmpty(refreshToken)) {
                authProvider.CurrentAccountSession = await oauthHelper.RedeemRefreshTokenAsync(refreshToken, Secrets.CLIENT_ID, Secrets.CLIENT_SECRET, LIVE_LOGIN_DESKTOP_URI, scopes);
            } else {
                if (string.IsNullOrEmpty(code))
                    code = await oauthHelper.GetAuthorizationCodeAsync(Secrets.CLIENT_ID, LIVE_LOGIN_DESKTOP_URI, scopes, new WebAuthenticationUi(account));

                authProvider.CurrentAccountSession = await oauthHelper.RedeemAuthorizationCodeAsync(code, Secrets.CLIENT_ID, Secrets.CLIENT_SECRET, LIVE_LOGIN_DESKTOP_URI, scopes);

                if (string.IsNullOrEmpty(authProvider.CurrentAccountSession?.AccessToken))
                    throw new AuthenticationException(string.Format(CultureInfo.CurrentCulture, Properties.Resources.RetrieveAuthenticationCodeFromUri, LIVE_LOGIN_AUTHORIZE_URI));
            }

            var client = new OneDriveClient(authProvider);

            SaveRefreshToken(account, authProvider.CurrentAccountSession.RefreshToken, settingsPassPhrase);

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
