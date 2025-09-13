using System.Text;
using System.Text.Json;
using LivingCodexMobile.Models;

namespace LivingCodexMobile.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly IApiService _apiService;
    private User? _currentUser;
    private bool _isAuthenticated;

    public bool IsAuthenticated => _isAuthenticated;
    public User? CurrentUser => _currentUser;

    public event EventHandler<bool>? AuthenticationStateChanged;
    public event EventHandler<User>? UserLoggedIn;
    public event EventHandler? UserLoggedOut;

    public AuthenticationService(HttpClient httpClient, IApiService apiService)
    {
        _httpClient = httpClient;
        _apiService = apiService;
    }

    public async Task<bool> LoginAsync(string provider, string accessToken)
    {
        try
        {
            // For mobile OAuth, we'll use a simplified validation approach
            // In a real app, you'd validate the token with the OAuth provider
            var validationRequest = new OAuthValidationRequest
            {
                Provider = provider,
                Secret = accessToken, // In production, this should be a proper validation
                UserId = Guid.NewGuid().ToString(),
                Email = "user@example.com", // This would come from OAuth provider
                Name = "Mobile User"
            };

            var json = JsonSerializer.Serialize(validationRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/oauth/validate", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<OAuthCallbackResponse>(responseContent);
                
                if (result?.Success == true && !string.IsNullOrEmpty(result.UserId))
                {
                    await LoadUserProfileAsync(result.UserId);
                    return true;
                }
            }
            
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> LoginWithGoogleAsync()
    {
        // For mobile, we'll simulate Google OAuth
        // In a real implementation, you'd use Google's OAuth SDK
        return await LoginAsync("google", "mock_google_token");
    }

    public async Task<bool> LoginWithMicrosoftAsync()
    {
        // For mobile, we'll simulate Microsoft OAuth
        // In a real implementation, you'd use Microsoft's OAuth SDK
        return await LoginAsync("microsoft", "mock_microsoft_token");
    }

    public async Task LogoutAsync()
    {
        try
        {
            _currentUser = null;
            _isAuthenticated = false;
            
            // Clear any stored tokens or session data
            // In a real app, you'd clear secure storage
            
            AuthenticationStateChanged?.Invoke(this, false);
            UserLoggedOut?.Invoke(this, EventArgs.Empty);
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Logout error: {ex.Message}");
        }
    }

    public async Task<User?> GetCurrentUserAsync()
    {
        if (_currentUser != null)
            return _currentUser;

        // Try to load user from stored session
        await LoadUserFromSessionAsync();
        return _currentUser;
    }

    public async Task<bool> ValidateTokenAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/oauth/profile");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<OAuthProvidersResponse> GetAvailableProvidersAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/oauth/test");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<OAuthProvidersResponse>(content) ?? 
                       new OAuthProvidersResponse(new List<OAuthProviderInfo>(), 0);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Get providers error: {ex.Message}");
        }

        return new OAuthProvidersResponse(new List<OAuthProviderInfo>(), 0);
    }

    private async Task LoadUserProfileAsync(string userId)
    {
        try
        {
            var user = await _apiService.GetUserAsync(userId);
            if (user != null)
            {
                _currentUser = user;
                _isAuthenticated = true;
                
                AuthenticationStateChanged?.Invoke(this, true);
                UserLoggedIn?.Invoke(this, user);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Load user profile error: {ex.Message}");
        }
    }

    private async Task LoadUserFromSessionAsync()
    {
        // In a real app, you'd load from secure storage
        // For now, we'll just return null
        await Task.CompletedTask;
    }
}

public record OAuthValidationRequest
{
    public string Provider { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public record OAuthCallbackResponse
{
    public string Provider { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? UserId { get; set; }
    public string? Error { get; set; }
}
