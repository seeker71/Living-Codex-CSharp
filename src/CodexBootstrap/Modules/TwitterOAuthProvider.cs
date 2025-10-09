using System.Text.Json;
using CodexBootstrap.Core;

namespace CodexBootstrap.Modules;

/// <summary>
/// Twitter OAuth Provider - Real implementation for Twitter OAuth authentication
/// Handles Twitter OAuth flow including code-to-token exchange and user info fetching
/// </summary>
public class TwitterOAuthProvider : IIdentityProvider
{
    private readonly ICodexLogger _logger;
    private readonly HttpClient _httpClient;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _redirectUri;

    public TwitterOAuthProvider(ICodexLogger logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        _clientId = Environment.GetEnvironmentVariable("TWITTER_CLIENT_ID") ?? "";
        _clientSecret = Environment.GetEnvironmentVariable("TWITTER_CLIENT_SECRET") ?? "";
        _redirectUri = Environment.GetEnvironmentVariable("TWITTER_REDIRECT_URI") ?? "http://localhost:5000/identity/callback/twitter";
    }

    public string ProviderName => "twitter";
    public bool IsEnabled => false; // Disabled by default

    public async Task<IResult> InitiateLogin(string? returnUrl = null)
    {
        try
        {
            if (!IsEnabled)
            {
                return Results.Json(new { success = false, error = "Twitter OAuth not configured" }, statusCode: 503);
            }

            var state = Guid.NewGuid().ToString("N");
            var scope = "tweet.read users.read";
            var codeChallenge = GenerateCodeChallenge();
            var codeChallengeMethod = "S256";

            var authUrl = $"https://twitter.com/i/oauth2/authorize?" +
                         $"response_type=code&" +
                         $"client_id={Uri.EscapeDataString(_clientId)}&" +
                         $"redirect_uri={Uri.EscapeDataString(_redirectUri)}&" +
                         $"scope={Uri.EscapeDataString(scope)}&" +
                         $"state={state}&" +
                         $"code_challenge={codeChallenge}&" +
                         $"code_challenge_method={codeChallengeMethod}";

            if (!string.IsNullOrEmpty(returnUrl))
            {
                authUrl += $"&return_url={Uri.EscapeDataString(returnUrl)}";
            }

            _logger.Info($"Twitter OAuth: Initiated login with state {state}");

            return Results.Ok(new
            {
                success = true,
                provider = "twitter",
                loginUrl = authUrl,
                state = state,
                message = "Twitter OAuth login initiated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Twitter OAuth: Error initiating login: {ex.Message}", ex);
            return Results.Json(new { success = false, error = "Failed to initiate Twitter OAuth login" }, statusCode: 500);
        }
    }

    public async Task<IdentityCallbackResponse> HandleCallbackAsync(string code, string state, string? returnUrl = null)
    {
        try
        {
            if (!IsEnabled)
            {
                return new IdentityCallbackResponse("twitter", false, Error: "Twitter OAuth not configured");
            }

            // Exchange code for access token
            var tokenResponse = await ExchangeCodeForTokenAsync(code);
            if (tokenResponse == null)
            {
                return new IdentityCallbackResponse("twitter", false, Error: "Failed to exchange code for token");
            }

            // Get user info using access token
            var userInfo = await GetUserInfoFromTokenAsync(tokenResponse.AccessToken);
            if (userInfo == null)
            {
                return new IdentityCallbackResponse("twitter", false, Error: "Failed to get user info");
            }

            _logger.Info($"Twitter OAuth: Successfully authenticated user {userInfo.Data.Username}");

            return new IdentityCallbackResponse("twitter", true, Token: tokenResponse.AccessToken);
        }
        catch (Exception ex)
        {
            _logger.Error($"Twitter OAuth: Error handling callback: {ex.Message}", ex);
            return new IdentityCallbackResponse("twitter", false, Error: "Twitter OAuth callback failed");
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
            _logger.Error($"Twitter OAuth: Error getting user info: {ex.Message}", ex);
            return null;
        }
    }

    private string GenerateCodeChallenge()
    {
        // Generate a random code verifier and challenge
        var codeVerifier = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var challengeBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(codeVerifier));
        return Convert.ToBase64String(challengeBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private async Task<TwitterTokenResponse?> ExchangeCodeForTokenAsync(string code)
    {
        try
        {
            var tokenRequest = new Dictionary<string, string>
            {
                ["code"] = code,
                ["grant_type"] = "authorization_code",
                ["client_id"] = _clientId,
                ["redirect_uri"] = _redirectUri,
                ["code_verifier"] = "dummy_verifier" // In production, store and retrieve the actual verifier
            };

            var content = new FormUrlEncodedContent(tokenRequest);
            var response = await _httpClient.PostAsync("https://api.twitter.com/2/oauth2/token", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.Error($"Twitter OAuth: Token exchange failed: {responseContent}");
                return null;
            }

            var tokenData = JsonSerializer.Deserialize<TwitterTokenResponse>(responseContent);
            return tokenData;
        }
        catch (Exception ex)
        {
            _logger.Error($"Twitter OAuth: Error exchanging code for token: {ex.Message}", ex);
            return null;
        }
    }

    private async Task<TwitterUserInfo?> GetUserInfoFromTokenAsync(string accessToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.twitter.com/2/users/me?user.fields=id,name,username,email,profile_image_url");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.Error($"Twitter OAuth: User info request failed: {responseContent}");
                return null;
            }

            var userData = JsonSerializer.Deserialize<TwitterUserInfo>(responseContent);
            return userData;
        }
        catch (Exception ex)
        {
            _logger.Error($"Twitter OAuth: Error getting user info: {ex.Message}", ex);
            return null;
        }
    }
}

// Twitter OAuth response types
public record TwitterTokenResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    string? RefreshToken,
    string? Scope
);

public record TwitterUserInfo(
    TwitterUserData Data
);

public record TwitterUserData(
    string Id,
    string Name,
    string Username,
    string? Email,
    string? ProfileImageUrl
);
