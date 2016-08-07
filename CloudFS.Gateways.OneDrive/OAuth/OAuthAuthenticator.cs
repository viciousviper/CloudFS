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
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Security.Authentication;
using System.Threading.Tasks;
using Microsoft.OneDrive.Sdk;
using IgorSoft.CloudFS.Authentication;
using IgorSoft.CloudFS.Gateways.OneDrive.Properties;

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

            public async Task<IDictionary<string, string>> AuthenticateAsync(Uri requestUri, Uri callbackUri)
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

                return result;
            }
        }

        private const string LIVE_LOGIN_DESKTOP_URI = "https://login.live.com/oauth20_desktop.srf";

        private const string LIVE_LOGIN_AUTHORIZE_URI = "https://login.live.com/oauth20_authorize.srf";

        private static readonly string[] scopes = { "wl.basic", "wl.offline_access", "wl.signin", "onedrive.readwrite" };

        private static string LoadRefreshToken(string account)
        {
            var refreshTokens = Settings.Default.RefreshTokens;
            if (refreshTokens != null)
                foreach (RefreshTokenSetting setting in refreshTokens)
                    if (setting.Account == account)
                        return setting.RefreshToken;

            return null;
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
                refreshTokens = Settings.Default.RefreshTokens = new System.Collections.ObjectModel.Collection<OAuth.RefreshTokenSetting>();
            }

            refreshTokens.Insert(0, new RefreshTokenSetting() { Account = account, RefreshToken = refreshToken });

            Settings.Default.Save();
        }

        public static async Task<IOneDriveClient> LoginAsync(string account, string code)
        {
            if (string.IsNullOrEmpty(account))
                throw new ArgumentNullException(nameof(account));

            var refreshToken = LoadRefreshToken(account);

            var client = default(IOneDriveClient);
            if (!string.IsNullOrEmpty(refreshToken)) {
                client = await OneDriveClient.GetSilentlyAuthenticatedMicrosoftAccountClient(Secrets.CLIENT_ID, LIVE_LOGIN_DESKTOP_URI, scopes, Secrets.CLIENT_SECRET, refreshToken);
            } else {
                if (string.IsNullOrEmpty(code)) {
                    client = await OneDriveClient.GetAuthenticatedMicrosoftAccountClient(Secrets.CLIENT_ID, LIVE_LOGIN_DESKTOP_URI, scopes, Secrets.CLIENT_SECRET, new WebAuthenticationUi(account));
                } else {
                    client = OneDriveClient.GetMicrosoftAccountClient(Secrets.CLIENT_ID, LIVE_LOGIN_DESKTOP_URI, scopes, Secrets.CLIENT_SECRET);
                    client.AuthenticationProvider.CurrentAccountSession = new AccountSession() { AccessToken = code };
                }

                await client.AuthenticateAsync();

                if (!client.IsAuthenticated)
                    throw new AuthenticationException(string.Format(CultureInfo.CurrentCulture, Resources.RetrieveAuthenticationCodeFromUri, LIVE_LOGIN_AUTHORIZE_URI));
            }

            SaveRefreshToken(account, client.AuthenticationProvider.CurrentAccountSession.RefreshToken);

            return client;
        }
    }
}
