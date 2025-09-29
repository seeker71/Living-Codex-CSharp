using System.Text.Json;
using CodexBootstrap.Core;

namespace CodexBootstrap.Modules;

/// <summary>
/// GitHub OAuth Provider - Real implementation for GitHub OAuth authentication
/// Handles GitHub OAuth flow including code-to-token exchange and user info fetching
/// </summary>
public class GitHubOAuthProvider : IIdentityProvider
{
    private readonly ICodexLogger _logger;
    private readonly HttpClient _httpClient;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _redirectUri;

    public GitHubOAuthProvider(ICodexLogger logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        _clientId = Environment.GetEnvironmentVariable("GITHUB_CLIENT_ID") ?? "";
        _clientSecret = Environment.GetEnvironmentVariable("GITHUB_CLIENT_SECRET") ?? "";
        _redirectUri = Environment.GetEnvironmentVariable("GITHUB_REDIRECT_URI") ?? "http://localhost:5000/identity/callback/github";
    }

    public string ProviderName => "github";
    public bool IsEnabled => false; // Disabled by default

    public async Task<object> InitiateLogin(string? returnUrl = null)
    {
        try
        {
            if (!IsEnabled)
            {
                return new { success = false, error = "GitHub OAuth not configured" };
            }

            var state = Guid.NewGuid().ToString("N");
            var scope = "user:email";

            var authUrl = $"https://github.com/login/oauth/authorize?" +
                         $"client_id={Uri.EscapeDataString(_clientId)}&" +
                         $"redirect_uri={Uri.EscapeDataString(_redirectUri)}&" +
                         $"scope={Uri.EscapeDataString(scope)}&" +
                         $"state={state}";

            if (!string.IsNullOrEmpty(returnUrl))
            {
                authUrl += $"&return_url={Uri.EscapeDataString(returnUrl)}";
            }

            _logger.Info($"GitHub OAuth: Initiated login with state {state}");

            return new
            {
                success = true,
                provider = "github",
                loginUrl = authUrl,
                state = state,
                message = "GitHub OAuth login initiated successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"GitHub OAuth: Error initiating login: {ex.Message}", ex);
            return new { success = false, error = "Failed to initiate GitHub OAuth login" };
        }
    }

    public async Task<IdentityCallbackResponse> HandleCallbackAsync(string code, string state, string? returnUrl = null)
    {
        try
        {
            if (!IsEnabled)
            {
                return new IdentityCallbackResponse("github", false, Error: "GitHub OAuth not configured");
            }

            // Exchange code for access token
            var tokenResponse = await ExchangeCodeForTokenAsync(code);
            if (tokenResponse == null)
            {
                return new IdentityCallbackResponse("github", false, Error: "Failed to exchange code for token");
            }

            // Get user info using access token
            var userInfo = await GetUserInfoFromTokenAsync(tokenResponse.AccessToken);
            if (userInfo == null)
            {
                return new IdentityCallbackResponse("github", false, Error: "Failed to get user info");
            }

            _logger.Info($"GitHub OAuth: Successfully authenticated user {userInfo.Login}");

            return new IdentityCallbackResponse("github", true, Token: tokenResponse.AccessToken);
        }
        catch (Exception ex)
        {
            _logger.Error($"GitHub OAuth: Error handling callback: {ex.Message}", ex);
            return new IdentityCallbackResponse("github", false, Error: "GitHub OAuth callback failed");
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
            _logger.Error($"GitHub OAuth: Error getting user info: {ex.Message}", ex);
            return null;
        }
    }

    private async Task<GitHubTokenResponse?> ExchangeCodeForTokenAsync(string code)
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
            var response = await _httpClient.PostAsync("https://github.com/login/oauth/access_token", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.Error($"GitHub OAuth: Token exchange failed: {responseContent}");
                return null;
            }

            // GitHub returns form-encoded data, not JSON
            var formData = System.Web.HttpUtility.ParseQueryString(responseContent);
            var accessToken = formData["access_token"];
            var tokenType = formData["token_type"];
            var scope = formData["scope"];

            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.Error($"GitHub OAuth: No access token in response: {responseContent}");
                return null;
            }

            return new GitHubTokenResponse(accessToken, tokenType ?? "bearer", scope);
        }
        catch (Exception ex)
        {
            _logger.Error($"GitHub OAuth: Error exchanging code for token: {ex.Message}", ex);
            return null;
        }
    }

    private async Task<GitHubUserInfo?> GetUserInfoFromTokenAsync(string accessToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Add("User-Agent", "CodexBootstrap/1.0");

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.Error($"GitHub OAuth: User info request failed: {responseContent}");
                return null;
            }

            var userData = JsonSerializer.Deserialize<GitHubUserInfo>(responseContent);
            return userData;
        }
        catch (Exception ex)
        {
            _logger.Error($"GitHub OAuth: Error getting user info: {ex.Message}", ex);
            return null;
        }
    }
}

// GitHub OAuth response types
public record GitHubTokenResponse(
    string AccessToken,
    string TokenType,
    string? Scope
);

public record GitHubUserInfo(
    int Id,
    string Login,
    string Name,
    string? Email,
    string? AvatarUrl,
    string? Bio,
    string? Company,
    string? Location,
    string? Blog,
    int PublicRepos,
    int Followers,
    int Following,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
