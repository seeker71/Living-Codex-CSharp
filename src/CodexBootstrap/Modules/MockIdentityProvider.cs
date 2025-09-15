using System.Text.Json;
using CodexBootstrap.Core;

namespace CodexBootstrap.Modules;

/// <summary>
/// Mock Identity Provider - Generic identity provider for testing
/// Implements standard identity provider interface for testing purposes
/// </summary>
public class MockIdentityProvider : IIdentityProvider
{
    private readonly ICodexLogger _logger;
    private readonly Dictionary<string, OAuthState> _oauthStates = new();
    private readonly Dictionary<string, string> _accessTokens = new();
    private readonly Dictionary<string, MockUser> _mockUsers = new();

    public string ProviderName => "Mock Provider";
    public bool IsEnabled => true;

    public MockIdentityProvider(ICodexLogger logger)
    {
        _logger = logger;
        InitializeMockUsers();
    }

    private void InitializeMockUsers()
    {
        _mockUsers["testuser1"] = new MockUser("testuser1", "testuser1@example.com", "Test User 1", "Test User 1", "testuser1");
        _mockUsers["testuser2"] = new MockUser("testuser2", "testuser2@example.com", "Test User 2", "Test User 2", "testuser2");
        _mockUsers["testuser3"] = new MockUser("testuser3", "testuser3@example.com", "Test User 3", "Test User 3", "testuser3");
        _mockUsers["admin"] = new MockUser("admin", "admin@example.com", "Admin User", "Admin User", "admin");
    }

    /// <summary>
    /// Initiate login flow (generic implementation)
    /// </summary>
    public async Task<object> InitiateLogin(string? returnUrl = null)
    {
        try
        {
            await Task.Delay(10); // Simulate async operation
            
            var state = Guid.NewGuid().ToString("N");
            var returnUrlParam = !string.IsNullOrEmpty(returnUrl) ? $"&return_url={Uri.EscapeDataString(returnUrl)}" : "";
            
            // Store state for validation
            _oauthStates[state] = new OAuthState
            {
                State = state,
                CreatedAt = DateTime.UtcNow,
                ReturnUrl = returnUrl
            };

            var loginUrl = $"/identity/callback/mock?code=mock_auth_code_{state}&state={state}{returnUrlParam}";
            
            _logger.Info($"Mock Identity: Initiated login with state {state}");
            
            return new
            {
                success = true,
                loginUrl = loginUrl,
                state = state,
                message = "Mock login initiated successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Mock Identity login initiation error: {ex.Message}", ex);
            return new { success = false, error = "Failed to initiate Mock login" };
        }
    }

    /// <summary>
    /// Handle callback (generic implementation)
    /// </summary>
    public async Task<IdentityCallbackResponse> HandleCallbackAsync(string code, string state, string? returnUrl = null)
    {
        try
        {
            // For testing, we'll be more lenient with state validation
            // In a real implementation, you'd validate the state properly
            if (!_oauthStates.TryGetValue(state, out var oauthState))
            {
                // Create a mock state for testing
                oauthState = new OAuthState
                {
                    State = state,
                    CreatedAt = DateTime.UtcNow,
                    ReturnUrl = returnUrl
                };
                _oauthStates[state] = oauthState;
            }

            // Exchange code for token
            var tokenResponse = await ExchangeCodeForTokenAsync(code, "mock");
            if (tokenResponse == null)
            {
                _logger.Error($"Mock Identity: Failed to exchange code {code} for token");
                return new IdentityCallbackResponse("mock", false, Error: "Failed to exchange code for token");
            }

            // Extract access token from response
            var accessToken = tokenResponse.GetType().GetProperty("AccessToken")?.GetValue(tokenResponse)?.ToString();
            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.Error($"Mock Identity: No access token in response");
                return new IdentityCallbackResponse("mock", false, Error: "No access token in response");
            }

            // Get user info
            var userInfo = await GetUserInfoAsync(accessToken);
            if (userInfo == null)
            {
                _logger.Error($"Mock Identity: Failed to get user info for token {accessToken}");
                return new IdentityCallbackResponse("mock", false, Error: "Failed to get user info");
            }

            // Clean up state
            _oauthStates.Remove(state);

            _logger.Info($"Mock Identity: Successfully authenticated user {((MockUser)userInfo).Id}");

            return new IdentityCallbackResponse("mock", true, accessToken);
        }
        catch (Exception ex)
        {
            _logger.Error($"Mock Identity callback error: {ex.Message}", ex);
            return new IdentityCallbackResponse("mock", false, Error: "Identity callback failed");
        }
    }

    /// <summary>
    /// Exchange authorization code for access token (generic implementation)
    /// </summary>
    public async Task<object?> ExchangeCodeForTokenAsync(string code, string provider)
    {
        await Task.Delay(100); // Simulate network delay
        
        // Generate a mock access token
        var accessToken = $"mock_access_token_{Guid.NewGuid():N}";
        var refreshToken = $"mock_refresh_token_{Guid.NewGuid():N}";
        
        // Store the token for later validation
        _accessTokens[accessToken] = code;
        
        _logger.Info($"Mock Identity: Exchanged code {code} for token {accessToken}");
        
        return new
        {
            AccessToken = accessToken,
            TokenType = "Bearer",
            ExpiresIn = 3600,
            RefreshToken = refreshToken,
            Scope = "openid profile email"
        };
    }

    /// <summary>
    /// Get user info using access token (generic implementation)
    /// </summary>
    public async Task<object?> GetUserInfoAsync(string accessToken)
    {
        await Task.Delay(50); // Simulate network delay
        
        // For testing, we'll accept any access token
        // In a real implementation, you'd validate the token properly
        if (string.IsNullOrEmpty(accessToken))
        {
            _logger.Warn($"Mock Identity: Empty access token");
            return null;
        }

        // Return a random mock user
        var random = new Random();
        var users = _mockUsers.Values.ToList();
        var selectedUser = users[random.Next(users.Count)];
        
        _logger.Info($"Mock Identity: Retrieved user info for {selectedUser.Email}");
        
        return selectedUser;
    }

    /// <summary>
    /// Get mock user by ID
    /// </summary>
    public MockUser? GetMockUser(string userId)
    {
        return _mockUsers.TryGetValue(userId, out var user) ? user : null;
    }

    /// <summary>
    /// Get all mock users
    /// </summary>
    public List<MockUser> GetAllMockUsers()
    {
        return _mockUsers.Values.ToList();
    }

    /// <summary>
    /// Validate user (generic implementation)
    /// </summary>
    public async Task<bool> ValidateUserAsync(MockUser user)
    {
        await Task.Delay(10); // Simulate validation delay
        
        if (user == null || string.IsNullOrEmpty(user.Id) || string.IsNullOrEmpty(user.Email))
        {
            return false;
        }

        _logger.Info($"Mock Identity: Validated user {user.Email}");
        return true;
    }

    /// <summary>
    /// Get identity providers list
    /// </summary>
    public List<IdentityProviderInfo> GetIdentityProviders()
    {
        return new List<IdentityProviderInfo>
        {
            new("mock", "Mock Identity Provider", "mock-client-id", true)
        };
    }
}

// Mock User and State Classes
public class MockUser
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;

    public MockUser() { }

    public MockUser(string id, string email, string name, string displayName, string username)
    {
        Id = id;
        Email = email;
        Name = name;
        DisplayName = displayName;
        Username = username;
    }
}

public class OAuthState
{
    public string State { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? ReturnUrl { get; set; }
}
