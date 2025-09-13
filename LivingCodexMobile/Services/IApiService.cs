using LivingCodexMobile.Models;

namespace LivingCodexMobile.Services
{
    public interface IApiService
    {
        // Authentication
        Task<ApiResponse<User>> AuthenticateAsync(string username, string password);
        Task<ApiResponse<User>> CreateUserAsync(string username, string email, string password);
        Task<ApiResponse<User>> GetUserProfileAsync(string userId);
        Task<User?> GetUserAsync(string userId);
        Task LogoutAsync();

        // Concepts
        Task<ApiResponse<List<Concept>>> GetConceptsAsync();
        Task<ApiResponse<Concept>> GetConceptAsync(string conceptId);
        Task<ApiResponse<Concept>> CreateConceptAsync(Concept concept);
        Task<ApiResponse<Concept>> UpdateConceptAsync(string conceptId, Concept concept);
        Task<ApiResponse> DeleteConceptAsync(string conceptId);

        // Contributions
        Task<ApiResponse<List<Contribution>>> GetUserContributionsAsync(string userId);
        Task<ApiResponse<Contribution>> CreateContributionAsync(Contribution contribution);
        Task<ApiResponse<Contribution>> UpdateContributionAsync(string contributionId, Contribution contribution);
        Task<ApiResponse> DeleteContributionAsync(string contributionId);

        // Resonance and Energy
        Task<ApiResponse<double>> GetCollectiveEnergyAsync();
        Task<ApiResponse<double>> GetContributorEnergyAsync(string userId);
        Task<ApiResponse<List<Contribution>>> GetAbundanceEventsAsync();

        // Health Check
        Task<ApiResponse> HealthCheckAsync();
    }
}
