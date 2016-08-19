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
using CG.Web.MegaApiClient;
using IgorSoft.CloudFS.Authentication;

namespace IgorSoft.CloudFS.Gateways.Mega.Auth
{
    internal static class Authenticator
    {
        private static DirectLogOn logOn;

        private static MegaApiClient.AuthInfos LoadRefreshToken(string account, string settingsPassPhrase)
        {
            var refreshTokens = Properties.Settings.Default.RefreshTokens;
            if (refreshTokens != null)
                foreach (RefreshTokenSetting setting in refreshTokens)
                    if (setting.Account == account)
                        return new MegaApiClient.AuthInfos(setting.EMail.DecryptUsing(settingsPassPhrase), setting.Hash.DecryptUsing(settingsPassPhrase), Encoding.Unicode.GetBytes(setting.PasswordAesKey.DecryptUsing(settingsPassPhrase)));

            return null;
        }

        private static void SaveRefreshToken(string account, MegaApiClient.AuthInfos refreshToken, string settingsPassPhrase)
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

            refreshTokens.Insert(0, new RefreshTokenSetting() { Account = account, EMail = refreshToken.Email.EncryptUsing(settingsPassPhrase), Hash = refreshToken.Hash.EncryptUsing(settingsPassPhrase), PasswordAesKey = Encoding.Unicode.GetString(refreshToken.PasswordAesKey).EncryptUsing(settingsPassPhrase) });

            Properties.Settings.Default.Save();
        }

        public static string GetAuthCode(string account)
        {
            string authCode = null;

            if (logOn == null)
                logOn = new DirectLogOn(AsyncOperationManager.SynchronizationContext);

            EventHandler<AuthenticatedEventArgs> callback = (s, e) => authCode = string.Join(",", e.Parameters.Get("account"), e.Parameters.Get("password"));
            logOn.Authenticated += callback;

            logOn.Show("Mega", account);

            logOn.Authenticated -= callback;

            return authCode;
        }

        public static async Task<MegaApiClient> LoginAsync(string account, string code, string settingsPassPhrase)
        {
            if (string.IsNullOrEmpty(account))
                throw new ArgumentNullException(nameof(account));

            var client = new MegaApiClient();

            var refreshToken = LoadRefreshToken(account, settingsPassPhrase);

            if (refreshToken != null) {
                await client.LoginAsync(refreshToken);
            } else {
                if (string.IsNullOrEmpty(code))
                    code = GetAuthCode(account);

                var parts = code?.Split(new[] { ',' }, 2) ?? Array.Empty<string>();
                if (parts.Length != 2)
                    throw new AuthenticationException(string.Format(CultureInfo.CurrentCulture, Properties.Resources.ProvideAuthenticationData, account));

                refreshToken = MegaApiClient.GenerateAuthInfos(parts[0], parts[1]);
                client.Login(refreshToken);
            }

            SaveRefreshToken(account, refreshToken, settingsPassPhrase);

            return client;
        }
    }
}
