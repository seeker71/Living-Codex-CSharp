using System.Text.Json;
using CodexBootstrap.Core;

namespace CodexBootstrap.Modules;

/// <summary>
/// Google OAuth Provider - Real implementation for Google OAuth authentication
/// Handles Google OAuth flow including code-to-token exchange and user info fetching
/// </summary>
public class GoogleOAuthProvider : IIdentityProvider
{
    private readonly ICodexLogger _logger;
    private readonly HttpClient _httpClient;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _redirectUri;

    public GoogleOAuthProvider(ICodexLogger logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        _clientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") ?? "";
        _clientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET") ?? "";
        _redirectUri = Environment.GetEnvironmentVariable("GOOGLE_REDIRECT_URI") ?? "http://localhost:5000/identity/callback/google";
    }

    public string ProviderName => "google";
    public bool IsEnabled => true; // Always enabled for testing

    public async Task<IResult> InitiateLogin(string? returnUrl = null)
    {
        try
        {
            if (!IsEnabled)
            {
                return Results.Json(new { success = false, error = "Google OAuth not configured" }, statusCode: 503);
            }

            var state = Guid.NewGuid().ToString("N");
            var scope = "openid email profile";
            var responseType = "code";
            var accessType = "offline";
            var prompt = "consent";

            var authUrl = $"https://accounts.google.com/o/oauth2/v2/auth?" +
                         $"client_id={Uri.EscapeDataString(_clientId)}&" +
                         $"redirect_uri={Uri.EscapeDataString(_redirectUri)}&" +
                         $"scope={Uri.EscapeDataString(scope)}&" +
                         $"response_type={responseType}&" +
                         $"access_type={accessType}&" +
                         $"prompt={prompt}&" +
                         $"state={state}";

            if (!string.IsNullOrEmpty(returnUrl))
            {
                authUrl += $"&return_url={Uri.EscapeDataString(returnUrl)}";
            }

            _logger.Info($"Google OAuth: Initiated login with state {state}");

            return Results.Ok(new
            {
                success = true,
                provider = "google",
                loginUrl = authUrl,
                state = state,
                message = "Google OAuth login initiated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Google OAuth: Error initiating login: {ex.Message}", ex);
            return Results.Json(new { success = false, error = "Failed to initiate Google OAuth login" }, statusCode: 500);
        }
    }

    public async Task<IdentityCallbackResponse> HandleCallbackAsync(string code, string state, string? returnUrl = null)
    {
        try
        {
            if (!IsEnabled)
            {
                return new IdentityCallbackResponse("google", false, Error: "Google OAuth not configured");
            }

            // Exchange code for access token
            var tokenResponse = await ExchangeCodeForTokenAsync(code);
            if (tokenResponse == null)
            {
                return new IdentityCallbackResponse("google", false, Error: "Failed to exchange code for token");
            }

            // Get user info using access token
            var userInfo = await GetUserInfoFromTokenAsync(tokenResponse.AccessToken);
            if (userInfo == null)
            {
                return new IdentityCallbackResponse("google", false, Error: "Failed to get user info");
            }

            _logger.Info($"Google OAuth: Successfully authenticated user {userInfo.Email}");

            return new IdentityCallbackResponse("google", true, Token: tokenResponse.AccessToken);
        }
        catch (Exception ex)
        {
            _logger.Error($"Google OAuth: Error handling callback: {ex.Message}", ex);
            return new IdentityCallbackResponse("google", false, Error: "Google OAuth callback failed");
        }
    }

    public async Task<object?> GetUserInfoAsync(string accessToken)
    {
        try
        {
            var userInfo = await GetUserInfoFromTokenAsync(accessToken);
            return userInfo;
        }
        catch (Exception ex)
        {
            _logger.Error($"Google OAuth: Error getting user info: {ex.Message}", ex);
            return null;
        }
    }

    private async Task<GoogleTokenResponse?> ExchangeCodeForTokenAsync(string code)
    {
        try
        {
            var tokenRequest = new Dictionary<string, string>
            {
                ["client_id"] = _clientId,
                ["client_secret"] = _clientSecret,
                ["code"] = code,
                ["grant_type"] = "authorization_code",
                ["redirect_uri"] = _redirectUri
            };

            var content = new FormUrlEncodedContent(tokenRequest);
            var response = await _httpClient.PostAsync("https://oauth2.googleapis.com/token", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.Error($"Google OAuth: Token exchange failed: {responseContent}");
                return null;
            }

            var tokenData = JsonSerializer.Deserialize<GoogleTokenResponse>(responseContent);
            return tokenData;
        }
        catch (Exception ex)
        {
            _logger.Error($"Google OAuth: Error exchanging code for token: {ex.Message}", ex);
            return null;
        }
    }

    private async Task<GoogleUserInfo?> GetUserInfoFromTokenAsync(string accessToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v2/userinfo");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.Error($"Google OAuth: User info request failed: {responseContent}");
                return null;
            }

            var userData = JsonSerializer.Deserialize<GoogleUserInfo>(responseContent);
            return userData;
        }
        catch (Exception ex)
        {
            _logger.Error($"Google OAuth: Error getting user info: {ex.Message}", ex);
            return null;
        }
    }
}

// Google OAuth response types
public record GoogleTokenResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    string? RefreshToken,
    string? Scope
);

public record GoogleUserInfo(
    string Id,
    string Email,
    string Name,
    string? Picture,
    string? GivenName,
    string? FamilyName,
    string? Locale,
    bool VerifiedEmail
);
