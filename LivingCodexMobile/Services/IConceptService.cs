using LivingCodexMobile.Models;

namespace LivingCodexMobile.Services;

public interface IConceptService
{
    Task<List<Concept>> GetConceptsAsync(ConceptQuery? query = null);
    Task<Concept?> GetConceptAsync(string conceptId);
    Task<Concept> CreateConceptAsync(CreateConceptRequest request);
    Task<Concept> UpdateConceptAsync(string conceptId, UpdateConceptRequest request);
    Task<bool> DeleteConceptAsync(string conceptId);
    Task<List<Concept>> SearchConceptsAsync(ConceptSearchRequest request);
    Task<List<Concept>> DiscoverConceptsAsync(ConceptDiscoveryRequest request);
    Task<ConceptRelationship> RelateConceptsAsync(ConceptRelateRequest request);
    Task<List<Concept>> GetRelatedConceptsAsync(string conceptId);
    Task<List<Concept>> GetConceptsByInterestAsync(string userId);
    Task<bool> MarkConceptInterestAsync(string userId, string conceptId, bool interested);
    Task<ConceptQualityAssessment> AssessConceptQualityAsync(string conceptId);
    Task<List<Concept>> GetTrendingConceptsAsync(int limit = 10);
    Task<List<Concept>> GetRecommendedConceptsAsync(string userId, int limit = 10);
}
