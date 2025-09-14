using LivingCodexMobile.Models;

namespace LivingCodexMobile.Services;

public interface IAuthenticationService
{
    bool IsAuthenticated { get; }
    User? CurrentUser { get; }
    
    event EventHandler<bool> AuthenticationStateChanged;
    event EventHandler<User> UserLoggedIn;
    event EventHandler UserLoggedOut;
    
    Task<bool> LoginAsync(string provider, string accessToken);
    Task<bool> LoginWithGoogleAsync();
    Task<bool> LoginWithMicrosoftAsync();
    Task LogoutAsync();
    Task<User?> GetCurrentUserAsync();
    Task<bool> ValidateTokenAsync();
    Task<OAuthProvidersResponse> GetAvailableProvidersAsync();
}

public record OAuthProvidersResponse(List<OAuthProviderInfo> Providers, int Count);
public record OAuthProviderInfo(string Provider, string DisplayName, string ClientId, bool IsEnabled);


