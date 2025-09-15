using System.Text.Json;
using CodexBootstrap.Core;

namespace CodexBootstrap.Modules;

/// <summary>
/// Microsoft OAuth Provider - Real implementation for Microsoft OAuth authentication
/// Handles Microsoft OAuth flow including code-to-token exchange and user info fetching
/// </summary>
public class MicrosoftOAuthProvider : IIdentityProvider
{
    private readonly ICodexLogger _logger;
    private readonly HttpClient _httpClient;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _redirectUri;

    public MicrosoftOAuthProvider(ICodexLogger logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        _clientId = Environment.GetEnvironmentVariable("MICROSOFT_CLIENT_ID") ?? "";
        _clientSecret = Environment.GetEnvironmentVariable("MICROSOFT_CLIENT_SECRET") ?? "";
        _redirectUri = Environment.GetEnvironmentVariable("MICROSOFT_REDIRECT_URI") ?? "http://localhost:5000/identity/callback/microsoft";
    }

    public string ProviderName => "Microsoft";
    public bool IsEnabled => !string.IsNullOrEmpty(_clientId) && !string.IsNullOrEmpty(_clientSecret);

    public async Task<object> InitiateLogin(string? returnUrl = null)
    {
        try
        {
            if (!IsEnabled)
            {
                return new { success = false, error = "Microsoft OAuth not configured" };
            }

            var state = Guid.NewGuid().ToString("N");
            var scope = "openid email profile";
            var responseType = "code";
            var responseMode = "query";

            var authUrl = $"https://login.microsoftonline.com/common/oauth2/v2.0/authorize?" +
                         $"client_id={Uri.EscapeDataString(_clientId)}&" +
                         $"response_type={responseType}&" +
                         $"redirect_uri={Uri.EscapeDataString(_redirectUri)}&" +
                         $"response_mode={responseMode}&" +
                         $"scope={Uri.EscapeDataString(scope)}&" +
                         $"state={state}";

            if (!string.IsNullOrEmpty(returnUrl))
            {
                authUrl += $"&return_url={Uri.EscapeDataString(returnUrl)}";
            }

            _logger.Info($"Microsoft OAuth: Initiated login with state {state}");

            return new
            {
                success = true,
                provider = "microsoft",
                loginUrl = authUrl,
                state = state,
                message = "Microsoft OAuth login initiated successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Microsoft OAuth: Error initiating login: {ex.Message}", ex);
            return new { success = false, error = "Failed to initiate Microsoft OAuth login" };
        }
    }

    public async Task<IdentityCallbackResponse> HandleCallbackAsync(string code, string state, string? returnUrl = null)
    {
        try
        {
            if (!IsEnabled)
            {
                return new IdentityCallbackResponse("microsoft", false, Error: "Microsoft OAuth not configured");
            }

            // Exchange code for access token
            var tokenResponse = await ExchangeCodeForTokenAsync(code);
            if (tokenResponse == null)
            {
                return new IdentityCallbackResponse("microsoft", false, Error: "Failed to exchange code for token");
            }

            // Get user info using access token
            var userInfo = await GetUserInfoFromTokenAsync(tokenResponse.AccessToken);
            if (userInfo == null)
            {
                return new IdentityCallbackResponse("microsoft", false, Error: "Failed to get user info");
            }

            _logger.Info($"Microsoft OAuth: Successfully authenticated user {userInfo.UserPrincipalName}");

            return new IdentityCallbackResponse("microsoft", true, Token: tokenResponse.AccessToken);
        }
        catch (Exception ex)
        {
            _logger.Error($"Microsoft OAuth: Error handling callback: {ex.Message}", ex);
            return new IdentityCallbackResponse("microsoft", false, Error: "Microsoft OAuth callback failed");
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
            _logger.Error($"Microsoft OAuth: Error getting user info: {ex.Message}", ex);
            return null;
        }
    }

    private async Task<MicrosoftTokenResponse?> ExchangeCodeForTokenAsync(string code)
    {
        try
        {
            var tokenRequest = new Dictionary<string, string>
            {
                ["client_id"] = _clientId,
                ["client_secret"] = _clientSecret,
                ["code"] = code,
                ["grant_type"] = "authorization_code",
                ["redirect_uri"] = _redirectUri,
                ["scope"] = "openid email profile"
            };

            var content = new FormUrlEncodedContent(tokenRequest);
            var response = await _httpClient.PostAsync("https://login.microsoftonline.com/common/oauth2/v2.0/token", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.Error($"Microsoft OAuth: Token exchange failed: {responseContent}");
                return null;
            }

            var tokenData = JsonSerializer.Deserialize<MicrosoftTokenResponse>(responseContent);
            return tokenData;
        }
        catch (Exception ex)
        {
            _logger.Error($"Microsoft OAuth: Error exchanging code for token: {ex.Message}", ex);
            return null;
        }
    }

    private async Task<MicrosoftUserInfo?> GetUserInfoFromTokenAsync(string accessToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.Error($"Microsoft OAuth: User info request failed: {responseContent}");
                return null;
            }

            var userData = JsonSerializer.Deserialize<MicrosoftUserInfo>(responseContent);
            return userData;
        }
        catch (Exception ex)
        {
            _logger.Error($"Microsoft OAuth: Error getting user info: {ex.Message}", ex);
            return null;
        }
    }
}

// Microsoft OAuth response types
public record MicrosoftTokenResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    string? RefreshToken,
    string? Scope
);

public record MicrosoftUserInfo(
    string Id,
    string UserPrincipalName,
    string DisplayName,
    string? GivenName,
    string? Surname,
    string? Mail,
    string? JobTitle,
    string? OfficeLocation,
    string? PreferredLanguage
);
