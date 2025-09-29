using System.Text.Json;
using CodexBootstrap.Core;

namespace CodexBootstrap.Modules;

/// <summary>
/// Facebook OAuth Provider - Real implementation for Facebook OAuth authentication
/// Handles Facebook OAuth flow including code-to-token exchange and user info fetching
/// </summary>
public class FacebookOAuthProvider : IIdentityProvider
{
    private readonly ICodexLogger _logger;
    private readonly HttpClient _httpClient;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _redirectUri;

    public FacebookOAuthProvider(ICodexLogger logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        _clientId = Environment.GetEnvironmentVariable("FACEBOOK_CLIENT_ID") ?? "";
        _clientSecret = Environment.GetEnvironmentVariable("FACEBOOK_CLIENT_SECRET") ?? "";
        _redirectUri = Environment.GetEnvironmentVariable("FACEBOOK_REDIRECT_URI") ?? "http://localhost:5000/identity/callback/facebook";
    }

    public string ProviderName => "facebook";
    public bool IsEnabled => false; // Disabled by default

    public async Task<object> InitiateLogin(string? returnUrl = null)
    {
        try
        {
            if (!IsEnabled)
            {
                return new { success = false, error = "Facebook OAuth not configured" };
            }

            var state = Guid.NewGuid().ToString("N");
            var scope = "email,public_profile";

            var authUrl = $"https://www.facebook.com/v18.0/dialog/oauth?" +
                         $"client_id={Uri.EscapeDataString(_clientId)}&" +
                         $"redirect_uri={Uri.EscapeDataString(_redirectUri)}&" +
                         $"scope={Uri.EscapeDataString(scope)}&" +
                         $"response_type=code&" +
                         $"state={state}";

            if (!string.IsNullOrEmpty(returnUrl))
            {
                authUrl += $"&return_url={Uri.EscapeDataString(returnUrl)}";
            }

            _logger.Info($"Facebook OAuth: Initiated login with state {state}");

            return new
            {
                success = true,
                provider = "facebook",
                loginUrl = authUrl,
                state = state,
                message = "Facebook OAuth login initiated successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Facebook OAuth: Error initiating login: {ex.Message}", ex);
            return new { success = false, error = "Failed to initiate Facebook OAuth login" };
        }
    }

    public async Task<IdentityCallbackResponse> HandleCallbackAsync(string code, string state, string? returnUrl = null)
    {
        try
        {
            if (!IsEnabled)
            {
                return new IdentityCallbackResponse("facebook", false, Error: "Facebook OAuth not configured");
            }

            // Exchange code for access token
            var tokenResponse = await ExchangeCodeForTokenAsync(code);
            if (tokenResponse == null)
            {
                return new IdentityCallbackResponse("facebook", false, Error: "Failed to exchange code for token");
            }

            // Get user info using access token
            var userInfo = await GetUserInfoFromTokenAsync(tokenResponse.AccessToken);
            if (userInfo == null)
            {
                return new IdentityCallbackResponse("facebook", false, Error: "Failed to get user info");
            }

            _logger.Info($"Facebook OAuth: Successfully authenticated user {userInfo.Email}");

            return new IdentityCallbackResponse("facebook", true, Token: tokenResponse.AccessToken);
        }
        catch (Exception ex)
        {
            _logger.Error($"Facebook OAuth: Error handling callback: {ex.Message}", ex);
            return new IdentityCallbackResponse("facebook", false, Error: "Facebook OAuth callback failed");
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
            _logger.Error($"Facebook OAuth: Error getting user info: {ex.Message}", ex);
            return null;
        }
    }

    private async Task<FacebookTokenResponse?> ExchangeCodeForTokenAsync(string code)
    {
        try
        {
            var tokenRequest = new Dictionary<string, string>
            {
                ["client_id"] = _clientId,
                ["client_secret"] = _clientSecret,
                ["code"] = code,
                ["redirect_uri"] = _redirectUri
            };

            var content = new FormUrlEncodedContent(tokenRequest);
            var response = await _httpClient.PostAsync("https://graph.facebook.com/v18.0/oauth/access_token", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.Error($"Facebook OAuth: Token exchange failed: {responseContent}");
                return null;
            }

            var tokenData = JsonSerializer.Deserialize<FacebookTokenResponse>(responseContent);
            return tokenData;
        }
        catch (Exception ex)
        {
            _logger.Error($"Facebook OAuth: Error exchanging code for token: {ex.Message}", ex);
            return null;
        }
    }

    private async Task<FacebookUserInfo?> GetUserInfoFromTokenAsync(string accessToken)
    {
        try
        {
            var fields = "id,name,email,first_name,last_name,picture";
            var url = $"https://graph.facebook.com/v18.0/me?fields={fields}&access_token={accessToken}";

            var response = await _httpClient.GetStringAsync(url);
            var userData = JsonSerializer.Deserialize<FacebookUserInfo>(response);
            return userData;
        }
        catch (Exception ex)
        {
            _logger.Error($"Facebook OAuth: Error getting user info: {ex.Message}", ex);
            return null;
        }
    }
}

// Facebook OAuth response types
public record FacebookTokenResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn
);

public record FacebookUserInfo(
    string Id,
    string Name,
    string? Email,
    string? FirstName,
    string? LastName,
    FacebookPicture? Picture
);

public record FacebookPicture(
    FacebookPictureData? Data
);

public record FacebookPictureData(
    string? Url,
    int? Width,
    int? Height
);
