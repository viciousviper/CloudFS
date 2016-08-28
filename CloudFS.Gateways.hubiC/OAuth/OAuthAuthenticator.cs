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
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SwiftClient;
using IgorSoft.CloudFS.Authentication;
using static IgorSoft.CloudFS.Authentication.OAuth.Constants;

namespace IgorSoft.CloudFS.Gateways.hubiC.OAuth
{
    internal static class OAuthAuthenticator
    {
        private const string HUBIC_LOGIN_AUTHORIZE_URI = "https://api.hubic.com/oauth/auth";

        private const string HUBIC_AUTH_TOKEN_URI = "https://api.hubic.com/oauth/token";

        private const string HUBIC_CREDENTIALS_URI = "https://api.hubic.com/1.0/account/credentials";

        private const string HUBIC_LOGIN_REDIRECT_URI = "http://localhost/hubic_redirect/";

        private static BrowserLogOn logOn;

        private class AppTokenResponse
        {
            [JsonProperty(Parameters.TokenType)]
            public string TokenType { get; set; }

            [JsonProperty(Parameters.Scope)]
            public string Scope { get; set; }

            [JsonProperty(Parameters.AccessToken)]
            public string AccessToken { get; set; }

            [JsonProperty(Parameters.ExpiresIn)]
            public int AccessTokenExpirationDuration { get; set; }

            [JsonProperty(Parameters.RefreshToken)]
            public string RefreshToken { get; set; }
        }

        private class CredentialsResponse
        {
            [JsonProperty("token")]
            public string Token { get; set; }

            [JsonProperty("endpoint")]
            public string Endpoint { get; set; }

            [JsonProperty("expires")]
            public DateTimeOffset TokenExpirationDuration { get; set; }
        }

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
                .AppendParameter(Parameters.RedirectUri, WebUtility.HtmlEncode(HUBIC_LOGIN_REDIRECT_URI))
                .AppendParameter(Parameters.Scope, "usage.r,account.r,getAllLinks.r,credentials.r,sponsorCode.r,activate.w,sponsored.r,links.drw")
                .AppendParameter(Parameters.ResponseType, ResponseTypes.Code);

            return new UriBuilder(HUBIC_LOGIN_AUTHORIZE_URI) { Query = queryBuilder.ToString() }.Uri;
        }

        private static async Task<AppTokenResponse> RedeemAccessTokenAsync(string clientId, string clientSecret, string code)
        {
            var queryBuilder = new QueryStringBuilder()
                .AppendParameter(Parameters.ClientId, clientId)
                .AppendParameter(Parameters.ClientSecret, clientSecret)
                .AppendParameter(Parameters.Code, code)
                .AppendParameter(Parameters.RedirectUri, WebUtility.HtmlEncode(HUBIC_LOGIN_REDIRECT_URI))
                .AppendParameter(Parameters.GrantType, GrantTypes.AuthorizationCode);

            var response = await PostQueryAsync(HUBIC_AUTH_TOKEN_URI, queryBuilder.ToString());

            return JsonConvert.DeserializeObject<AppTokenResponse>(response);
        }

        private static async Task<AppTokenResponse> RedeemRefreshTokenAsync(string clientId, string clientSecret, string refreshToken)
        {
            var queryBuilder = new QueryStringBuilder()
                .AppendParameter(Parameters.ClientId, clientId)
                .AppendParameter(Parameters.ClientSecret, clientSecret)
                .AppendParameter(Parameters.RefreshToken, refreshToken)
                .AppendParameter(Parameters.GrantType, GrantTypes.RefreshToken);

            var response = await PostQueryAsync(HUBIC_AUTH_TOKEN_URI, queryBuilder.ToString());

            return JsonConvert.DeserializeObject<AppTokenResponse>(response);
        }

        private static async Task<SwiftAuthData> AuthenticateAsync(string accessToken)
        {
            var httpWebRequest = WebRequest.CreateHttp(HUBIC_CREDENTIALS_URI);
            httpWebRequest.Headers.Add(HttpRequestHeader.Authorization, $"Bearer {accessToken}".ToString(CultureInfo.InvariantCulture));
            var response = await httpWebRequest.GetResponseAsync() as HttpWebResponse;
            if (response != null && response.StatusCode == HttpStatusCode.OK) {
                using (var reader = new StreamReader(response.GetResponseStream())) {
                    var credentials = JsonConvert.DeserializeObject<CredentialsResponse>(await reader.ReadToEndAsync());
                    return new SwiftAuthData() { AuthToken = credentials.Token, StorageUrl = credentials.Endpoint };
                }
            }

            return null;
        }

        private static async Task<string> PostQueryAsync(string uri, string query)
        {
            var httpWebRequest = WebRequest.CreateHttp(uri);
            httpWebRequest.Method = "POST";
            httpWebRequest.ContentType = "application/x-www-form-urlencoded";
            var stream = await httpWebRequest.GetRequestStreamAsync();
            using (var writer = new StreamWriter(stream)) {
                await writer.WriteAsync(query);
                await writer.FlushAsync();
            }

            var response = await httpWebRequest.GetResponseAsync() as HttpWebResponse;
            if (response != null && response.StatusCode == HttpStatusCode.OK) {
                using (var reader = new StreamReader(response.GetResponseStream())) {
                    return await reader.ReadToEndAsync();
                }
            }

            return null;
        }

        private static string GetAuthCode(string account, Uri authenticationUri, Uri redirectUri)
        {
            string oauth_token = null;

            if (logOn == null)
                logOn = new BrowserLogOn(AsyncOperationManager.SynchronizationContext);

            EventHandler<AuthenticatedEventArgs> callback = (s, e) => oauth_token = e.Parameters[Parameters.Code];
            logOn.Authenticated += callback;

            logOn.Show("hubiC", account, authenticationUri, redirectUri);

            logOn.Authenticated -= callback;

            return oauth_token;
        }

        public static async Task<Client> LoginAsync(string account, string code, string settingsPassPhrase)
        {
            if (string.IsNullOrEmpty(account))
                throw new ArgumentNullException(nameof(account));

            var refreshToken = LoadRefreshToken(account, settingsPassPhrase);

            AppTokenResponse response = null;
            if (!string.IsNullOrEmpty(refreshToken))
                response = await RedeemRefreshTokenAsync(Secrets.CLIENT_ID, Secrets.CLIENT_SECRET, refreshToken);

            if (response == null) {
                if (string.IsNullOrEmpty(code)) {
                    var authenticationUri = GetAuthenticationUri(Secrets.CLIENT_ID);
                    code = GetAuthCode(account, authenticationUri, new Uri(HUBIC_LOGIN_REDIRECT_URI));
                    if (string.IsNullOrEmpty(code))
                        throw new AuthenticationException(string.Format(CultureInfo.CurrentCulture, Properties.Resources.RetrieveAuthenticationCodeFromUri, authenticationUri.ToString()));
                }

                response = await RedeemAccessTokenAsync(Secrets.CLIENT_ID, Secrets.CLIENT_SECRET, code);
            }

            SaveRefreshToken(account, response?.RefreshToken ?? refreshToken, settingsPassPhrase);

            var authManager = new SwiftAuthManager() { Credentials = new SwiftCredentials() { Password = response.AccessToken }, Authenticate = (user, password, endpoint) => AuthenticateAsync(password) };
            authManager.SetEndpoints(new[] { "http://localhost:8080" }.ToList());
            return new Client(authManager);
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
