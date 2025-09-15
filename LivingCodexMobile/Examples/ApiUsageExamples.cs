using LivingCodexMobile.Services;

namespace LivingCodexMobile.Examples;

/// <summary>
/// Examples of how to use the generic API service for one-liner API calls
/// </summary>
public class ApiUsageExamples
{
    private readonly IApiService _apiService;

    public ApiUsageExamples(IApiService apiService)
    {
        _apiService = apiService;
    }

    // Example 1: Simple GET request
    public async Task<List<NewsItem>> GetNewsFeed(string userId)
    {
        var response = await _apiService.GetAsync<NewsResponse>($"/news/feed/{userId}");
        return response?.Items ?? new List<NewsItem>();
    }

    // Example 2: POST request with request/response types
    public async Task<Concept> CreateConcept(CreateConceptRequest request)
    {
        return await _apiService.PostAsync<CreateConceptRequest, Concept>("/concepts", request);
    }

    // Example 3: PUT request
    public async Task<Concept> UpdateConcept(string conceptId, UpdateConceptRequest request)
    {
        return await _apiService.PutAsync<UpdateConceptRequest, Concept>($"/concepts/{conceptId}", request);
    }

    // Example 4: DELETE request
    public async Task DeleteConcept(string conceptId)
    {
        await _apiService.DeleteAsync<object>($"/concepts/{conceptId}");
    }

    // Example 5: PATCH request
    public async Task<Concept> PatchConcept(string conceptId, PatchConceptRequest request)
    {
        return await _apiService.PatchAsync<PatchConceptRequest, Concept>($"/concepts/{conceptId}", request);
    }

    // Example 6: Search with query parameters
    public async Task<List<NewsItem>> SearchNews(string query)
    {
        var response = await _apiService.GetAsync<NewsResponse>($"/news/search?query={Uri.EscapeDataString(query)}");
        return response?.Items ?? new List<NewsItem>();
    }

    // Example 7: Authentication
    public async Task<User> AuthenticateUser(LoginRequest request)
    {
        return await _apiService.PostAsync<LoginRequest, User>("/identity/login", request);
    }

    // Example 8: Get user profile
    public async Task<User> GetUserProfile(string userId)
    {
        return await _apiService.GetAsync<User>($"/identity/users/{userId}");
    }

    // Example 9: Get available OAuth providers
    public async Task<List<OAuthProviderInfo>> GetOAuthProviders()
    {
        var response = await _apiService.GetAsync<IdentityProvidersResponseDto>("/identity/providers");
        return response?.providers?.Select(p => new OAuthProviderInfo(p.Provider, p.DisplayName, p.ClientId, p.IsEnabled)).ToList() ?? new List<OAuthProviderInfo>();
    }

    // Example 10: Health check
    public async Task<bool> CheckHealth()
    {
        try
        {
            await _apiService.GetAsync<object>("/health");
            return true;
        }
        catch
        {
            return false;
        }
    }
}

// Example request/response types
public record CreateConceptRequest(string Name, string Description, List<string> Tags);
public record UpdateConceptRequest(string Name, string Description, List<string> Tags);
public record PatchConceptRequest(string? Name, string? Description, List<string>? Tags);
public record LoginRequest(string Username, string Password);
public record NewsResponse(List<NewsItem> Items, int TotalCount, DateTime LastUpdated);
public record IdentityProvidersResponseDto(List<IdentityProviderInfoDto> providers);
public record IdentityProviderInfoDto(string Provider, string DisplayName, string ClientId, bool IsEnabled);
