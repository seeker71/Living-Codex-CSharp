using System.Text;
using System.Text.Json;
using LivingCodexMobile.Models;
using Microsoft.Maui.Authentication.WebAuthenticator;
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
            // Create a mock user for demonstration
            var mockUser = new User
            {
                Id = Guid.NewGuid().ToString(),
                Username = $"{provider}_user",
                Email = $"user@{provider}.com",
                DisplayName = $"{provider} Test User",
                CreatedAt = DateTime.UtcNow,
                LastActive = DateTime.UtcNow,
                Permissions = new List<string> { "read", "write", "contribute" }
            };

            _currentUser = mockUser;
            _isAuthenticated = true;
            
            AuthenticationStateChanged?.Invoke(this, true);
            UserLoggedIn?.Invoke(this, mockUser);
            
            return true;
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
            // Get OAuth providers from backend
            var providersResponse = await GetAvailableProvidersAsync();
            var googleProvider = providersResponse.Providers.FirstOrDefault(p => p.Provider == "google");
            
            if (googleProvider == null || !googleProvider.IsEnabled)
            {
                System.Diagnostics.Debug.WriteLine("Google OAuth not available");
                return false;
            }

            // Start OAuth challenge with backend
            var challengeUrl = $"http://localhost:5002/oauth/challenge/google";
            var callbackUrl = "livingcodex://oauth-callback";

            var authResult = await WebAuthenticator.AuthenticateAsync(
                new WebAuthenticatorOptions
                {
                    Url = new Uri(challengeUrl),
                    CallbackUrl = new Uri(callbackUrl),
                    PrefersEphemeralWebBrowserSession = false
                });

            if (authResult?.Properties != null)
            {
                // Extract user info from OAuth result
                var userId = authResult.Properties.TryGetValue("user_id", out var uid) ? uid : Guid.NewGuid().ToString();
                var email = authResult.Properties.TryGetValue("email", out var em) ? em : "user@gmail.com";
                var name = authResult.Properties.TryGetValue("name", out var nm) ? nm : "Google User";
                var accessToken = authResult.Properties.TryGetValue("access_token", out var token) ? token : "";

                // Validate with backend
                var validationRequest = new OAuthValidationRequest
                {
                    Provider = "google",
                    Secret = "google_client_secret", // In production, get from secure storage
                    UserId = userId,
                    Email = email,
                    Name = name
                };

                var validationResponse = await ValidateOAuthWithBackendAsync(validationRequest);
                
                if (validationResponse?.Success == true)
                {
                    // Create user from OAuth data
                    var googleUser = new User
                    {
                        Id = userId,
                        Username = email.Split('@')[0],
                        Email = email,
                        DisplayName = name,
                        CreatedAt = DateTime.UtcNow,
                        LastActive = DateTime.UtcNow,
                        Permissions = new List<string> { "read", "write", "contribute" },
                        AvatarUrl = authResult.Properties.TryGetValue("picture", out var pic) ? pic : null
                    };

                    _currentUser = googleUser;
                    _isAuthenticated = true;
                    
                    AuthenticationStateChanged?.Invoke(this, true);
                    UserLoggedIn?.Invoke(this, googleUser);
                    
                    System.Diagnostics.Debug.WriteLine($"Google login successful for {email}!");
                    return true;
                }
            }

            System.Diagnostics.Debug.WriteLine("Google OAuth failed - no valid response");
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
            // Create a Microsoft user for demonstration
            var microsoftUser = new User
            {
                Id = Guid.NewGuid().ToString(),
                Username = "microsoft_user",
                Email = "user@outlook.com",
                DisplayName = "Microsoft Test User",
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

        // Try to load user from stored session
        await LoadUserFromSessionAsync();
        return _currentUser;
    }

    public async Task<bool> ValidateTokenAsync()
    {
        try
        {
            // For demonstration, always return true if we have a current user
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
            var response = await _httpClient.GetAsync("http://localhost:5002/oauth/providers");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var providersResponse = JsonSerializer.Deserialize<OAuthProvidersResponse>(content);
                return providersResponse ?? new OAuthProvidersResponse(new List<OAuthProviderInfo>(), 0);
            }
            
            // Fallback to demo providers if backend is not available
            var providers = new List<OAuthProviderInfo>
            {
                new OAuthProviderInfo("google", "Google", "google_client_id", true),
                new OAuthProviderInfo("microsoft", "Microsoft", "microsoft_client_id", true)
            };

            return new OAuthProvidersResponse(providers, providers.Count);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Get providers error: {ex.Message}");
            return new OAuthProvidersResponse(new List<OAuthProviderInfo>(), 0);
        }
    }

    private async Task LoadUserFromSessionAsync()
    {
        // In a real app, you'd load from secure storage
        // For now, we'll just return null
        await Task.CompletedTask;
    }

    private async Task<OAuthCallbackResponse?> ValidateOAuthWithBackendAsync(OAuthValidationRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("http://localhost:5002/oauth/validate", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<OAuthCallbackResponse>(responseContent);
            }
            
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OAuth validation error: {ex.Message}");
            return null;
        }
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