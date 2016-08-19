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
using CopyRestAPI;
using CopyRestAPI.Models;
using IgorSoft.CloudFS.Authentication;

namespace IgorSoft.CloudFS.Gateways.Copy.OAuth
{
    internal static class OAuthAuthenticator
    {
        private const string COPY_LOGIN_DESKTOP_URI = "https://localhost/copy_login";

        private static BrowserLogOn logOn;

        private static Tuple<string, string> LoadRefreshToken(string account, string settingsPassPhrase)
        {
            var refreshTokens = Properties.Settings.Default.RefreshTokens;
            if (refreshTokens != null)
                foreach (RefreshTokenSetting setting in refreshTokens)
                    if (setting.Account == account)
                        return new Tuple<string, string>(setting.Token.DecryptUsing(settingsPassPhrase), setting.TokenSecret.DecryptUsing(settingsPassPhrase));

            return null;
        }

        private static void SaveRefreshToken(string account, Tuple<string, string> refreshToken, string settingsPassPhrase)
        {
            var refreshTokens = Properties.Settings.Default.RefreshTokens;
            if (refreshTokens != null) {
                foreach (RefreshTokenSetting setting in refreshTokens)
                    if (setting.Account == account) {
                        refreshTokens.Remove(setting);
                        break;
                    }
            } else {
                refreshTokens = Properties.Settings.Default.RefreshTokens = new System.Collections.ObjectModel.Collection<RefreshTokenSetting>();
            }

            refreshTokens.Insert(0, new RefreshTokenSetting() { Account = account, Token = refreshToken.Item1.EncryptUsing(settingsPassPhrase), TokenSecret = refreshToken.Item2.EncryptUsing(settingsPassPhrase) });

            Properties.Settings.Default.Save();
        }

        private static Tuple<string, string> GetAuthCode(string account, Uri authenticationUri, Uri redirectUri)
        {
            string oauth_token = null, oauth_verifier = null;

            if (logOn == null)
                logOn = new BrowserLogOn(AsyncOperationManager.SynchronizationContext);

            EventHandler<AuthenticatedEventArgs> callback = (s, e) => {
                oauth_token = e.Parameters["oauth_token"];
                oauth_verifier = e.Parameters["oauth_verifier"];
            };
            logOn.Authenticated += callback;

            logOn.Show("Copy", account, authenticationUri, redirectUri);

            logOn.Authenticated -= callback;

            return new Tuple<string, string>(oauth_token, oauth_verifier);
        }

        private static Tuple<string, string> SplitToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                return new Tuple<string, string>(string.Empty, string.Empty);

            var parts = token.Split(new[] { ',' }, 2);
            return new Tuple<string, string>(parts[0], parts.Length == 2 ? parts[1] : string.Empty);
        }

        public static async Task<CopyClient> LoginAsync(string account, string code, string settingsPassPhrase)
        {
            if (string.IsNullOrEmpty(account))
                throw new ArgumentNullException(nameof(account));

            var client = new CopyClient(new Config() {
                ConsumerKey = Secrets.CONSUMER_KEY, ConsumerSecret = Secrets.CONSUMER_SECRET, CallbackUrl = COPY_LOGIN_DESKTOP_URI
            });

            var refreshToken = LoadRefreshToken(account, settingsPassPhrase);

            var tokens = refreshToken ?? (!string.IsNullOrEmpty(code) ? SplitToken(code) : default(Tuple<string, string>));

            if (tokens != null) {
                client.Config.Token = tokens.Item1;
                client.Config.TokenSecret = tokens.Item2;
            } else {
                var requestToken = await client.Authorization.GetRequestTokenAsync();
                tokens = GetAuthCode(account, requestToken.AuthCodeUri, new Uri(COPY_LOGIN_DESKTOP_URI));
                if (string.IsNullOrEmpty(tokens.Item1) || string.IsNullOrEmpty(tokens.Item2))
                    throw new AuthenticationException(string.Format(CultureInfo.CurrentCulture, Properties.Resources.RetrieveAuthenticationCodeFromUri, requestToken.AuthCodeUri.ToString()));

                await client.Authorization.GetAccessTokenAsync(tokens.Item2);

                tokens = new Tuple<string, string>(client.Config.Token, client.Config.TokenSecret);
            }

            SaveRefreshToken(account, tokens, settingsPassPhrase);

            return client;
        }
    }
}
