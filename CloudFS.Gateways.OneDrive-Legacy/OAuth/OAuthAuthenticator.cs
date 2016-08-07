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
using IgorSoft.CloudFS.Authentication;
using static IgorSoft.CloudFS.Authentication.OAuth.Constants;
using Newtonsoft.Json;
using OneDrive;

namespace IgorSoft.CloudFS.Gateways.OneDrive_Legacy.OAuth
{
    internal static class OAuthAuthenticator
    {
        private const string LIVE_LOGIN_DESKTOP_URI = "https://login.live.com/oauth20_desktop.srf";

        private const string LIVE_LOGIN_AUTHORIZE_URI = "https://login.live.com/oauth20_authorize.srf";

        private const string LIVE_LOGIN_TOKEN_URI = "https://login.live.com/oauth20_token.srf";

        private const string ONEDRIVE_API_URI = "https://api.onedrive.com/v1.0";

        private static BrowserLogOn logOn;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Used for JSON deserialization")]
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

        private static Uri GetAuthenticationUri(string clientId)
        {
            var queryStringBuilder = new global::OneDrive.QueryStringBuilder();
            queryStringBuilder.Add(Parameters.ClientId, clientId);
            queryStringBuilder.Add(Parameters.Scope, string.Join(" ", (new [] { Scope.Basic, Scope.OfflineAccess, Scope.Signin, Scope.OneDriveReadWrite }).Select(s => s.GetDescription())));
            queryStringBuilder.Add(Parameters.RedirectUri, LIVE_LOGIN_DESKTOP_URI);
            queryStringBuilder.Add(Parameters.ResponseType, ResponseTypes.Code);
            queryStringBuilder.Add("display", "popup");

            return new UriBuilder(LIVE_LOGIN_AUTHORIZE_URI) { Query = queryStringBuilder.ToString() }.Uri;
        }

        private static async Task<AppTokenResponse> RedeemAccessTokenAsync(string clientId, string clientSecret, string code)
        {
            var queryStringBuilder = new global::OneDrive.QueryStringBuilder();
            queryStringBuilder.Add(Parameters.ClientId, clientId);
            queryStringBuilder.Add(Parameters.ClientSecret, clientSecret);
            queryStringBuilder.Add(Parameters.RedirectUri, LIVE_LOGIN_DESKTOP_URI);
            queryStringBuilder.Add(Parameters.Code, code);
            queryStringBuilder.Add(Parameters.GrantType, GrantTypes.AuthorizationCode);

            var response = await PostQueryAsync(LIVE_LOGIN_TOKEN_URI, queryStringBuilder.ToString());

            return JsonConvert.DeserializeObject<AppTokenResponse>(response);
        }

        private static async Task<AppTokenResponse> RedeemRefreshTokenAsync(string clientId, string clientSecret, string refreshToken)
        {
            var queryStringBuilder = new global::OneDrive.QueryStringBuilder();
            queryStringBuilder.Add(Parameters.ClientId, clientId);
            queryStringBuilder.Add(Parameters.ClientSecret, clientSecret);
            queryStringBuilder.Add(Parameters.RedirectUri, LIVE_LOGIN_DESKTOP_URI);
            queryStringBuilder.Add(Parameters.RefreshToken, refreshToken);
            queryStringBuilder.Add(Parameters.GrantType, GrantTypes.RefreshToken);

            var response = await PostQueryAsync(LIVE_LOGIN_TOKEN_URI, queryStringBuilder.ToString());

            return JsonConvert.DeserializeObject<AppTokenResponse>(response);
        }

        private static async Task<string> PostQueryAsync(string uriString, string queryString)
        {
            var httpWebRequest = WebRequest.CreateHttp(uriString);
            httpWebRequest.Method = "POST";
            httpWebRequest.ContentType = "application/x-www-form-urlencoded";
            var stream = await httpWebRequest.GetRequestStreamAsync();
            using (var writer = new StreamWriter(stream)) {
                await writer.WriteAsync(queryString);
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
            string authCode = null;

            if (logOn == null)
                logOn = new BrowserLogOn(AsyncOperationManager.SynchronizationContext);

            EventHandler<AuthenticatedEventArgs> callback = (s, e) => authCode = e.Parameters[Parameters.Code]?.TrimStart('M');
            logOn.Authenticated += callback;

            logOn.Show("OneDrive-Legacy", account, authenticationUri, redirectUri);

            logOn.Authenticated -= callback;

            return authCode;
        }

        public static async Task<ODConnection> LoginAsync(string account, string code)
        {
            if (string.IsNullOrEmpty(account))
                throw new ArgumentNullException(nameof(account));

            var refreshToken = LoadRefreshToken(account);

            AppTokenResponse response = null;
            if (!string.IsNullOrEmpty(refreshToken))
                response = await RedeemRefreshTokenAsync(Secrets.CLIENT_ID, Secrets.CLIENT_SECRET, refreshToken);

            if (response == null) {
                if (string.IsNullOrEmpty(code)) {
                    var authenticationUri = GetAuthenticationUri(Secrets.CLIENT_ID);
                    code = GetAuthCode(account, authenticationUri, new Uri(LIVE_LOGIN_DESKTOP_URI));
                    if (string.IsNullOrEmpty(code))
                        throw new AuthenticationException(string.Format(CultureInfo.CurrentCulture, Resources.RetrieveAuthenticationCodeFromUri, authenticationUri.ToString()));
                }

                response = await RedeemAccessTokenAsync(Secrets.CLIENT_ID, Secrets.CLIENT_SECRET, code);
            }

            SaveRefreshToken(account, response?.RefreshToken ?? refreshToken);

            return response != null ? new ODConnection(ONEDRIVE_API_URI, response.AccessToken) : null;
        }
    }
}
