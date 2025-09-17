using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace Coding_Club_Anticheat.Services
{
    public class GoogleOAuthService
    {
        private readonly string _clientId;
        private readonly string? _clientSecret;
        private readonly string _redirectUri = "http://localhost:8080/auth/callback";
        private readonly string[] _scopes = { "openid", "profile", "email" };
        private HttpListener? _httpListener;
        private string _codeVerifier = "";
        private string _codeChallenge = "";

        public GoogleOAuthService(string clientId, string? clientSecret = null)
        {
            _clientId = clientId;
            _clientSecret = clientSecret;
            
            // Only generate PKCE if no client secret (Desktop app)
            if (string.IsNullOrEmpty(clientSecret))
            {
                GeneratePKCEParameters();
            }
        }

        private void GeneratePKCEParameters()
        {
            // Generate code verifier (random string)
            var bytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            _codeVerifier = Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

            // Generate code challenge (SHA256 hash of verifier)
            using var sha256 = SHA256.Create();
            var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(_codeVerifier));
            _codeChallenge = Convert.ToBase64String(challengeBytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        public async Task<GoogleUserInfo?> AuthenticateAsync()
        {
            try
            {
                // Step 1: Generate authorization URL with PKCE
                var authUrl = GenerateAuthorizationUrl();
                
                // Step 2: Start local HTTP server to handle redirect
                _httpListener = new HttpListener();
                _httpListener.Prefixes.Add($"{_redirectUri}/");
                _httpListener.Start();

                // Step 3: Open browser for authorization
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = authUrl,
                    UseShellExecute = true
                });

                // Step 4: Wait for authorization response
                var context = await _httpListener.GetContextAsync();
                var response = context.Response;
                
                // Send a simple response to the browser
                var responseString = "<html><body><h1>Authentication successful!</h1><p>You can close this window.</p></body></html>";
                var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();

                // Step 5: Extract authorization code from callback
                var query = context.Request.Url?.Query;
                if (string.IsNullOrEmpty(query))
                    throw new Exception("No query parameters received");

                var queryParams = HttpUtility.ParseQueryString(query);
                var authCode = queryParams["code"];
                
                if (string.IsNullOrEmpty(authCode))
                {
                    var error = queryParams["error"];
                    throw new Exception($"Authorization failed: {error}");
                }

                // Step 6: Exchange authorization code for access token using PKCE
                var tokenResponse = await ExchangeCodeForTokensAsync(authCode);
                
                // Step 7: Get user information
                var userInfo = await GetUserInfoAsync(tokenResponse.AccessToken);
                
                return userInfo;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Google OAuth failed: {ex.Message}");
                return null;
            }
            finally
            {
                _httpListener?.Stop();
                _httpListener?.Close();
            }
        }

        private string GenerateAuthorizationUrl()
        {
            var state = Guid.NewGuid().ToString("N");
            
            var parameters = new Dictionary<string, string>
            {
                ["client_id"] = _clientId,
                ["redirect_uri"] = _redirectUri,
                ["response_type"] = "code",
                ["scope"] = string.Join(" ", _scopes),
                ["state"] = state,
                ["access_type"] = "offline"
            };

            // Add PKCE parameters only for desktop apps (no client secret)
            if (string.IsNullOrEmpty(_clientSecret))
            {
                parameters["code_challenge"] = _codeChallenge;
                parameters["code_challenge_method"] = "S256";
            }

            var queryString = string.Join("&", 
                parameters.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

            var authUrl = $"https://accounts.google.com/o/oauth2/v2/auth?{queryString}";
            
            // Debug: Log the authorization URL parameters
            System.Diagnostics.Debug.WriteLine("=== AUTHORIZATION URL PARAMETERS ===");
            System.Diagnostics.Debug.WriteLine($"Client Type: {(string.IsNullOrEmpty(_clientSecret) ? "Desktop (PKCE)" : "Web (Client Secret)")}");
            foreach (var kvp in parameters)
            {
                var value = kvp.Key == "code_challenge" ? $"{kvp.Value[..10]}..." : kvp.Value;
                System.Diagnostics.Debug.WriteLine($"{kvp.Key}: {value}");
            }
            System.Diagnostics.Debug.WriteLine($"Full URL: {authUrl}");
            System.Diagnostics.Debug.WriteLine("=====================================");

            return authUrl;
        }

        private async Task<TokenResponse> ExchangeCodeForTokensAsync(string authCode)
        {
            using var httpClient = new HttpClient();
            
            var tokenRequest = new Dictionary<string, string>
            {
                ["client_id"] = _clientId,
                ["redirect_uri"] = _redirectUri,
                ["code"] = authCode,
                ["grant_type"] = "authorization_code"
            };

            // Add appropriate authentication method
            if (string.IsNullOrEmpty(_clientSecret))
            {
                // Desktop app: Use PKCE
                tokenRequest["code_verifier"] = _codeVerifier;
                System.Diagnostics.Debug.WriteLine("=== TOKEN EXCHANGE REQUEST (Desktop/PKCE) ===");
            }
            else
            {
                // Web app: Use client secret
                tokenRequest["client_secret"] = _clientSecret;
                System.Diagnostics.Debug.WriteLine("=== TOKEN EXCHANGE REQUEST (Web/Client Secret) ===");
            }

            // Debug: Log the request parameters
            foreach (var kvp in tokenRequest)
            {
                var value = kvp.Key switch
                {
                    "code_verifier" => $"{kvp.Value[..10]}...",
                    "client_secret" => "***HIDDEN***",
                    _ => kvp.Value
                };
                System.Diagnostics.Debug.WriteLine($"{kvp.Key}: {value}");
            }
            System.Diagnostics.Debug.WriteLine("============================================");

            var content = new FormUrlEncodedContent(tokenRequest);
            var response = await httpClient.PostAsync("https://oauth2.googleapis.com/token", content);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"Token response status: {response.StatusCode}");
            System.Diagnostics.Debug.WriteLine($"Token response content: {responseContent}");
            
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Token exchange failed: {responseContent}");
            }

            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            return tokenResponse ?? throw new Exception("Failed to parse token response");
        }

        private async Task<GoogleUserInfo> GetUserInfoAsync(string accessToken)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await httpClient.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");
            
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Failed to get user information");
            }

            var json = await response.Content.ReadAsStringAsync();
            var userInfo = JsonSerializer.Deserialize<GoogleUserInfo>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            return userInfo ?? throw new Exception("Failed to parse user information");
        }
    }

    public class TokenResponse
    {
        public string AccessToken { get; set; } = "";
        public string IdToken { get; set; } = "";
        public string RefreshToken { get; set; } = "";
        public int ExpiresIn { get; set; }
        public string TokenType { get; set; } = "";
    }

    public class GoogleUserInfo
    {
        public string Id { get; set; } = "";
        public string Email { get; set; } = "";
        public string Name { get; set; } = "";
        public string GivenName { get; set; } = "";
        public string FamilyName { get; set; } = "";
        public string Picture { get; set; } = "";
        public bool VerifiedEmail { get; set; }
    }
}