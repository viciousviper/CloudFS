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
using MediaFireSDK;
using MediaFireSDK.Core;
using MediaFireSDK.Model;
using MediaFireSDK.Model.Errors;
using MediaFireSDK.Model.Responses;
using IgorSoft.CloudFS.Authentication;

namespace IgorSoft.CloudFS.Gateways.MediaFire.Auth
{
    internal static class Authenticator
    {
        private class SynchronizationContext
        {
            private IList<IMediaFireUserApi> contextHolders = new List<IMediaFireUserApi>();

            public AuthenticationContext LatestContext { get; private set; }

            public bool ContextUpdated { get; private set; }

            public SynchronizationContext(IMediaFireUserApi contextHolder)
            {
                contextHolders.Add(contextHolder);
                LatestContext = contextHolder.GetAuthenticationContext();
                contextHolder.AuthenticationContextChanged += UpdateContexts;
            }

            public void UpdateContexts(object source, EventArgs eventArgs)
            {
                LatestContext = ((IMediaFireUserApi)source).GetAuthenticationContext();
                ContextUpdated = true;
                foreach (var contextHolder in contextHolders)
                    if (source != contextHolder)
                        contextHolder.SetAuthenticationContext(LatestContext);
            }

            public void AttachContextHolder(IMediaFireUserApi contextHolder)
            {
                contextHolder.SetAuthenticationContext(LatestContext);
                contextHolders.Add(contextHolder);
                contextHolder.AuthenticationContextChanged += UpdateContexts;
            }
        }

        private static DirectLogOn logOn;

        private static IDictionary<string, SynchronizationContext> contextDirectory = new Dictionary<string, SynchronizationContext>();

        static Authenticator()
        {
            AppDomain.CurrentDomain.DomainUnload += ShutdownHandler<EventArgs>;
            AppDomain.CurrentDomain.UnhandledException += ShutdownHandler<UnhandledExceptionEventArgs>;
        }

        private static void ShutdownHandler<T>(object sender, T eventArgs)
            where T : EventArgs
        {
            foreach (var item in contextDirectory)
                if (item.Value.ContextUpdated)
                    SaveRefreshToken(item.Key, item.Value.LatestContext);
        }

        private static AuthenticationContext LoadRefreshToken(string account)
        {
            var refreshTokens = Settings.Default.RefreshTokens;
            if (refreshTokens != null)
                foreach (RefreshTokenSetting setting in refreshTokens)
                    if (setting.Account == account)
                        return new AuthenticationContext(setting.SessionToken, setting.SecretKey, setting.Time);

            return null;
        }

        internal static void SaveRefreshToken(string account, AuthenticationContext refreshToken)
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

            refreshTokens.Insert(0, new RefreshTokenSetting() { Account = account, SessionToken = refreshToken.SessionToken, SecretKey = refreshToken.SecretKey, Time = refreshToken.Time });

            Settings.Default.Save();
        }

        private static async Task<AuthenticationContext> RefreshSessionTokenAsync(IMediaFireAgent agent)
        {
            try {
                var userInfo = await agent.GetAsync<MediaFireGetUserInfoResponse>(MediaFireApiUserMethods.GetInfo);

                return agent.User.GetAuthenticationContext();
            } catch (MediaFireApiException) {
                return null;
            }
        }

        private static string GetAuthCode(string account)
        {
            var authCode = string.Empty;

            if (logOn == null)
                logOn = new DirectLogOn(AsyncOperationManager.SynchronizationContext);

            EventHandler<AuthenticatedEventArgs> callback = (s, e) => authCode = string.Join(",", e.Parameters.Get("account"), e.Parameters.Get("password"));
            logOn.Authenticated += callback;

            logOn.Show("Mediafire", account);

            logOn.Authenticated -= callback;

            return authCode;
        }

        public static async Task<MediaFireAgent> LoginAsync(string account, string code)
        {
            if (string.IsNullOrEmpty(account))
                throw new ArgumentNullException(nameof(account));

            var agent = new MediaFireAgent(new MediaFireApiConfiguration(Secrets.API_KEY, Secrets.APP_ID, useHttpV1: true, automaticallyRenewToken: false));

            var synchronizationContext = default(SynchronizationContext);
            if (contextDirectory.TryGetValue(account, out synchronizationContext)) {
                synchronizationContext.AttachContextHolder(agent.User);
            } else {
                var refreshToken = LoadRefreshToken(account);

                agent.User.SetAuthenticationContext(refreshToken);
                contextDirectory.Add(account, synchronizationContext = new SynchronizationContext(agent.User));

                if (refreshToken != null) {
                    refreshToken = await RefreshSessionTokenAsync(agent);
                }

                if (refreshToken == null) {
                    if (string.IsNullOrEmpty(code))
                        code = GetAuthCode(account);

                    var parts = code?.Split(new[] { ',' }, 2) ?? Array.Empty<string>();
                    if (parts.Length != 2)
                        throw new AuthenticationException(string.Format(CultureInfo.CurrentCulture, Resources.ProvideAuthenticationData, account));

                    await agent.User.GetSessionToken(parts[0], parts[1], TokenVersion.V2);

                    synchronizationContext.UpdateContexts(agent.User, EventArgs.Empty);
                }
            }

            return agent;
        }
    }
}
