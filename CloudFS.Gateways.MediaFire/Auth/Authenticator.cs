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
using MediaFireSDK;
using MediaFireSDK.Model;
using IgorSoft.CloudFS.Authentication;

namespace IgorSoft.CloudFS.Gateways.MediaFire.Auth
{
    internal static class Authenticator
    {
        private static DirectLogOn logOn;

        private static string LoadRefreshToken(string account)
        {
            var refreshTokens = Settings.Default.RefreshTokens;
            if (refreshTokens != null)
                foreach (RefreshTokenSetting setting in refreshTokens)
                    if (setting.Account == account)
                        return setting.RefreshToken;

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

            refreshTokens.Insert(0, new RefreshTokenSetting() { Account = account, RefreshToken = refreshToken });

            Settings.Default.Save();
        }

        private static System.Reflection.BindingFlags nonPublicInstance = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;

        private static async Task<string> RefreshSessionToken(IMediaFireAgent agent, string refreshToken)
        {
            // Workaround for non-public token refresh functionality in MediaFireSDK

            var mediaFireUserApiType = agent.User.GetType();
            var requestController = mediaFireUserApiType.GetProperty("RequestController", nonPublicInstance).GetValue(agent.User);
            var cryptoService = mediaFireUserApiType.GetField("_cryptoService", nonPublicInstance).GetValue(agent.User);

            var sessionBroker = mediaFireUserApiType.Assembly.GetTypes().Single(t => t.Name == "MediaFireSessionBroker").GetConstructors().Single().Invoke(new[] {
                cryptoService, agent.Configuration, null, null, requestController
            });
            requestController.GetType().GetProperty("SessionBroker", nonPublicInstance).SetValue(requestController, sessionBroker);
            sessionBroker.GetType().GetProperty("CurrentSessionToken", nonPublicInstance).SetValue(sessionBroker, refreshToken);

            try {
                return await ((Task<string>)sessionBroker.GetType().GetMethod("RenewSessionTokenInternal", nonPublicInstance).Invoke(sessionBroker, Array.Empty<object>()));
            } catch (MediaFireSDK.Model.Errors.MediaFireApiException) {
                return null;
            }
        }

        private static string GetAuthCode(string account)
        {
            var authCode = string.Empty;

            if (logOn == null) {
                logOn = new DirectLogOn(AsyncOperationManager.SynchronizationContext);
                logOn.Authenticated += (s, e) => authCode = string.Join(",", e.Parameters.Get("account"), e.Parameters.Get("password"));
            }

            logOn.Show("Mediafire", account);

            return authCode;
        }

        public static async Task<MediaFireAgent> Login(string account, string code)
        {
            if (string.IsNullOrEmpty(account))
                throw new ArgumentNullException(nameof(account));

            var agent = new MediaFireAgent(new MediaFireApiConfiguration(Secrets.API_KEY, Secrets.APP_ID, periodicallyRenewToken: true, useHttpV1: true));

            var refreshToken = LoadRefreshToken(account);

            if (refreshToken != null)
                refreshToken = await RefreshSessionToken(agent, refreshToken);

            if (refreshToken == null) {
                if (string.IsNullOrEmpty(code))
                    code = GetAuthCode(account);

                var parts = code?.Split(new[] { ',' }, 2) ?? Array.Empty<string>();
                if (parts.Length != 2)
                    throw new AuthenticationException(string.Format(CultureInfo.CurrentCulture, Resources.ProvideAuthenticationData, account));

                refreshToken = await agent.User.GetSessionToken(parts[0], parts[1]);
            }

            SaveRefreshToken(account, refreshToken);

            return agent;
        }
    }
}
