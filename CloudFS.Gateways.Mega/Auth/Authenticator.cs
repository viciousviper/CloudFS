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
using CG.Web.MegaApiClient;
using IgorSoft.CloudFS.Authentication;

namespace IgorSoft.CloudFS.Gateways.Mega.Auth
{
    internal static class Authenticator
    {
        private static DirectLogOn logOn;

        private static MegaApiClient.AuthInfos LoadRefreshToken(string account)
        {
            var refreshTokens = Settings.Default.RefreshTokens;
            if (refreshTokens != null)
                foreach (RefreshTokenSetting setting in refreshTokens)
                    if (setting.Account == account)
                        return new MegaApiClient.AuthInfos(setting.EMail, setting.Hash, setting.PasswordAesKey);

            return  null;
        }

        private static void SaveRefreshToken(string account, MegaApiClient.AuthInfos refreshToken)
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

            refreshTokens.Insert(0, new RefreshTokenSetting() { Account = account, EMail = refreshToken.Email, Hash = refreshToken.Hash, PasswordAesKey = refreshToken.PasswordAesKey });

            Settings.Default.Save();
        }

        public static string GetAuthCode(string account)
        {
            string authCode = null;

            if (logOn == null) {
                logOn = new DirectLogOn(AsyncOperationManager.SynchronizationContext);
                logOn.Authenticated += (s, e) => authCode = string.Join(",", e.Parameters.Get("account"), e.Parameters.Get("password"));
            }

            logOn.Show("Mega", account);

            return authCode;
        }

        public static MegaApiClient Login(string account, string code)
        {
            if (string.IsNullOrEmpty(account))
                throw new ArgumentNullException(nameof(account));

            var client = new MegaApiClient();

            var refreshToken = LoadRefreshToken(account);

            if (refreshToken != null) {
                client.Login(refreshToken);
            } else {
                if (string.IsNullOrEmpty(code))
                    code = GetAuthCode(account);

                var parts = code?.Split(new[] { ',' }, 2) ?? Array.Empty<string>();
                if (parts.Length != 2)
                    throw new AuthenticationException(string.Format(CultureInfo.CurrentCulture, Resources.ProvideAuthenticationData, account));

                client.Login(parts[0], parts[1]);
            }

            refreshToken = (MegaApiClient.AuthInfos)typeof(MegaApiClient).GetField("_authInfos", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(client);
            SaveRefreshToken(account, refreshToken);

            return client;
        }
    }
}
