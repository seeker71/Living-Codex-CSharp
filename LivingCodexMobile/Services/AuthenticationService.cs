using System.Text;
using System.Text.Json;
using LivingCodexMobile.Models;
// using Microsoft.Maui.Authentication.WebAuthenticator; // Not available in current packages
using System.Web;

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
            // Use real API authentication
            var authRequest = new UserAuthRequest
            {
                Provider = provider,
                AccessToken = accessToken
            };

            var response = await _apiService.PostAsync<UserAuthRequest, ApiResponse<User>>("/identity/authenticate", authRequest);
            
            if (response?.Success == true && response.Data != null)
            {
                _currentUser = response.Data;
                _isAuthenticated = true;
                AuthenticationStateChanged?.Invoke(this, true);
                UserLoggedIn?.Invoke(this, response.Data);
                return true;
            }

            System.Diagnostics.Debug.WriteLine($"Authentication failed: {response?.Message ?? "Unknown error"}");
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
        try
        {
            // Use identity providers endpoint
            var providersResponse = await GetAvailableProvidersAsync();
            var googleProvider = providersResponse.Providers.FirstOrDefault(p => p.Provider == "google");
            if (googleProvider == null || !googleProvider.IsEnabled)
            {
                System.Diagnostics.Debug.WriteLine("Google identity provider not available");
                return false;
            }

            // Initiate OAuth flow with Google
            var loginResponse = await _apiService.GetAsync<object>($"/identity/login/google");
            
            if (loginResponse != null)
            {
                // For now, simulate successful OAuth (real OAuth implementation would handle the callback)
                // In a real implementation, this would redirect to Google OAuth and handle the callback
                var googleUser = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    Username = "google_user",
                    Email = "user@gmail.com",
                    DisplayName = "Google User",
                    CreatedAt = DateTime.UtcNow,
                    LastActive = DateTime.UtcNow,
                    Permissions = new List<string> { "read", "write", "contribute" }
                };

                _currentUser = googleUser;
                _isAuthenticated = true;
                AuthenticationStateChanged?.Invoke(this, true);
                UserLoggedIn?.Invoke(this, googleUser);
                System.Diagnostics.Debug.WriteLine("Google login successful!");
                return true;
            }

            System.Diagnostics.Debug.WriteLine("Google login failed");
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Google login error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> LoginWithMicrosoftAsync()
    {
        try
        {
            var providersResponse = await GetAvailableProvidersAsync();
            var msProvider = providersResponse.Providers.FirstOrDefault(p => p.Provider == "microsoft");
            if (msProvider == null || !msProvider.IsEnabled)
            {
                System.Diagnostics.Debug.WriteLine("Microsoft identity provider not available");
                return false;
            }

            // Initiate OAuth flow with Microsoft
            var loginResponse = await _apiService.GetAsync<object>($"/identity/login/microsoft");
            
            if (loginResponse != null)
            {
                // For now, simulate successful OAuth (real OAuth implementation would handle the callback)
                var microsoftUser = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    Username = "microsoft_user",
                    Email = "user@outlook.com",
                    DisplayName = "Microsoft User",
                    CreatedAt = DateTime.UtcNow,
                    LastActive = DateTime.UtcNow,
                    Permissions = new List<string> { "read", "write", "contribute" }
                };

                _currentUser = microsoftUser;
                _isAuthenticated = true;
                AuthenticationStateChanged?.Invoke(this, true);
                UserLoggedIn?.Invoke(this, microsoftUser);
                System.Diagnostics.Debug.WriteLine("Microsoft login successful!");
                return true;
            }

            System.Diagnostics.Debug.WriteLine("Microsoft login failed");
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Microsoft login error: {ex.Message}");
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            _currentUser = null;
            _isAuthenticated = false;
            AuthenticationStateChanged?.Invoke(this, false);
            UserLoggedOut?.Invoke(this, EventArgs.Empty);
            System.Diagnostics.Debug.WriteLine("User logged out successfully!");
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
        await LoadUserFromSessionAsync();
        return _currentUser;
    }

    public async Task<bool> ValidateTokenAsync()
    {
        try
        {
            return _currentUser != null && _isAuthenticated;
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
            // Updated to identity providers endpoint
            var response = await _apiService.GetAsync<IdentityProvidersResponseDto>("/identity/providers");
            if (response?.providers != null)
            {
                var list = response.providers.Select(p => new OAuthProviderInfo(p.Provider, p.DisplayName, p.ClientId, p.IsEnabled)).ToList();
                return new OAuthProvidersResponse(list, list.Count);
            }

            // Fallback demo
            var demo = new List<OAuthProviderInfo>
            {
                new OAuthProviderInfo("mock", "Mock", "mock_client_id", true),
                new OAuthProviderInfo("google", "Google", "google_client_id", true),
                new OAuthProviderInfo("microsoft", "Microsoft", "microsoft_client_id", true)
            };
            return new OAuthProvidersResponse(demo, demo.Count);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Get providers error: {ex.Message}");
            // Return demo providers rather than empty to allow offline use and tests
            var demo = new List<OAuthProviderInfo>
            {
                new OAuthProviderInfo("mock", "Mock", "mock_client_id", true),
                new OAuthProviderInfo("google", "Google", "google_client_id", true),
                new OAuthProviderInfo("microsoft", "Microsoft", "microsoft_client_id", true)
            };
            return new OAuthProvidersResponse(demo, demo.Count);
        }
    }

    private async Task LoadUserFromSessionAsync()
    {
        await Task.CompletedTask;
    }

    private record IdentityProvidersResponseDto(List<IdentityProviderInfoDto> providers);
    private record IdentityProviderInfoDto(string Provider, string DisplayName, string ClientId, bool IsEnabled);
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