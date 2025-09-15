using LivingCodexMobile.Models;
using LivingCodexMobile.Services;

namespace LivingCodexMobile.Examples;

/// <summary>
/// Examples of how to use the concept service for concept discovery and management
/// </summary>
public class ConceptServiceExamples
{
    private readonly IConceptService _conceptService;

    public ConceptServiceExamples(IConceptService conceptService)
    {
        _conceptService = conceptService;
    }

    // Example 1: Get all concepts
    public async Task<List<Concept>> GetAllConceptsAsync()
    {
        return await _conceptService.GetConceptsAsync();
    }

    // Example 2: Get concepts by domain
    public async Task<List<Concept>> GetConceptsByDomainAsync(string domain)
    {
        var query = new ConceptQuery { Domain = domain };
        return await _conceptService.GetConceptsAsync(query);
    }

    // Example 3: Search concepts
    public async Task<List<Concept>> SearchConceptsAsync(string searchTerm)
    {
        var request = new ConceptSearchRequest
        {
            SearchTerm = searchTerm,
            Take = 50
        };
        return await _conceptService.SearchConceptsAsync(request);
    }

    // Example 4: Create a new concept
    public async Task<Concept> CreateConceptAsync(string name, string description, string domain)
    {
        var request = new CreateConceptRequest(
            name,
            description,
            domain,
            5,
            new List<string> { "example", "mobile" },
            null
        );
        return await _conceptService.CreateConceptAsync(request);
    }

    // Example 5: Update a concept
    public async Task<Concept> UpdateConceptAsync(string conceptId, string newDescription)
    {
        var request = new UpdateConceptRequest(
            Name: null,
            Description: newDescription,
            Domain: null,
            Complexity: null,
            Tags: null,
            Metadata: null
        );
        return await _conceptService.UpdateConceptAsync(conceptId, request);
    }

    // Example 6: Discover concepts from content
    public async Task<List<Concept>> DiscoverConceptsFromContentAsync(string content)
    {
        var request = new ConceptDiscoveryRequest(
            content,
            "text/plain",
            null,
            10
        );
        return await _conceptService.DiscoverConceptsAsync(request);
    }

    // Example 7: Create concept relationships
    public async Task<ConceptRelationship> RelateConceptsAsync(string fromConceptId, string toConceptId, string relationshipType)
    {
        var request = new ConceptRelateRequest(
            fromConceptId,
            toConceptId,
            relationshipType,
            1.0
        );
        return await _conceptService.RelateConceptsAsync(request);
    }

    // Example 8: Get related concepts
    public async Task<List<Concept>> GetRelatedConceptsAsync(string conceptId)
    {
        return await _conceptService.GetRelatedConceptsAsync(conceptId);
    }

    // Example 9: Mark concept interest
    public async Task<bool> MarkConceptInterestAsync(string userId, string conceptId, bool interested)
    {
        return await _conceptService.MarkConceptInterestAsync(userId, conceptId, interested);
    }

    // Example 10: Get user's interested concepts
    public async Task<List<Concept>> GetUserInterestedConceptsAsync(string userId)
    {
        return await _conceptService.GetConceptsByInterestAsync(userId);
    }

    // Example 11: Get trending concepts
    public async Task<List<Concept>> GetTrendingConceptsAsync()
    {
        return await _conceptService.GetTrendingConceptsAsync(20);
    }

    // Example 12: Get recommended concepts
    public async Task<List<Concept>> GetRecommendedConceptsAsync(string userId)
    {
        return await _conceptService.GetRecommendedConceptsAsync(userId, 20);
    }

    // Example 13: Assess concept quality
    public async Task<ConceptQualityAssessment> AssessConceptQualityAsync(string conceptId)
    {
        return await _conceptService.AssessConceptQualityAsync(conceptId);
    }

    // Example 14: Advanced concept search with filters
    public async Task<List<Concept>> AdvancedConceptSearchAsync()
    {
        var request = new ConceptSearchRequest
        {
            SearchTerm = "artificial intelligence",
            Domains = new[] { "technology", "science" },
            Complexities = new[] { 3, 4, 5 },
            Tags = new[] { "AI", "machine learning" },
            SortBy = "resonance",
            SortDescending = true,
            Take = 25
        };
        return await _conceptService.SearchConceptsAsync(request);
    }

    // Example 15: Concept discovery workflow
    public async Task<ConceptDiscoveryResult> DiscoverConceptsWorkflowAsync(string content, string userId)
    {
        var result = new ConceptDiscoveryResult { IsLoading = true };

        try
        {
            // Discover concepts from content
            var discoveredConcepts = await _conceptService.DiscoverConceptsAsync(new ConceptDiscoveryRequest(
                content,
                "text/plain",
                null,
                10
            ));

            result.DiscoveredConcepts = discoveredConcepts;

            // Get related concepts for each discovered concept
            var relatedConcepts = new List<Concept>();
            foreach (var concept in discoveredConcepts.Take(3))
            {
                var related = await _conceptService.GetRelatedConceptsAsync(concept.Id);
                relatedConcepts.AddRange(related);
            }
            result.RelatedConcepts = relatedConcepts.DistinctBy(c => c.Id).ToList();

            // Get recommended concepts for the user
            var recommendedConcepts = await _conceptService.GetRecommendedConceptsAsync(userId, 10);
            result.RecommendedConcepts = recommendedConcepts;

            result.IsLoading = false;
        }
        catch (Exception ex)
        {
            result.IsLoading = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    // Example 16: Concept interest management
    public async Task<ConceptInterestSummary> ManageConceptInterestsAsync(string userId, List<string> conceptIds)
    {
        var summary = new ConceptInterestSummary
        {
            UserId = userId,
            TotalConcepts = conceptIds.Count,
            InterestedConcepts = new List<string>(),
            NotInterestedConcepts = new List<string>()
        };

        foreach (var conceptId in conceptIds)
        {
            try
            {
                // Check if user is already interested
                var interestedConcepts = await _conceptService.GetConceptsByInterestAsync(userId);
                var isAlreadyInterested = interestedConcepts.Any(c => c.Id == conceptId);

                if (!isAlreadyInterested)
                {
                    // Mark as interested
                    var success = await _conceptService.MarkConceptInterestAsync(userId, conceptId, true);
                    if (success)
                    {
                        summary.InterestedConcepts.Add(conceptId);
                    }
                    else
                    {
                        summary.NotInterestedConcepts.Add(conceptId);
                    }
                }
                else
                {
                    summary.InterestedConcepts.Add(conceptId);
                }
            }
            catch (Exception ex)
            {
                summary.NotInterestedConcepts.Add(conceptId);
                summary.Errors.Add($"Failed to process {conceptId}: {ex.Message}");
            }
        }

        return summary;
    }
}

// Helper classes
public class ConceptDiscoveryResult
{
    public List<Concept> DiscoveredConcepts { get; set; } = new();
    public List<Concept> RelatedConcepts { get; set; } = new();
    public List<Concept> RecommendedConcepts { get; set; } = new();
    public bool IsLoading { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ConceptInterestSummary
{
    public string UserId { get; set; } = string.Empty;
    public int TotalConcepts { get; set; }
    public List<string> InterestedConcepts { get; set; } = new();
    public List<string> NotInterestedConcepts { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}
