using LivingCodexMobile.Models;

namespace LivingCodexMobile.Services;

public class ConceptService : IConceptService
{
    private readonly IApiService _apiService;

    public ConceptService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<List<Concept>> GetConceptsAsync(ConceptQuery? query = null)
    {
        try
        {
            var queryParams = query != null ? BuildQueryString(query) : "";
            var response = await _apiService.GetAsync<ConceptListResponse>($"/concepts{queryParams}");
            return response?.Concepts ?? new List<Concept>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Get concepts error: {ex.Message}");
            return new List<Concept>();
        }
    }

    public async Task<Concept?> GetConceptAsync(string conceptId)
    {
        try
        {
            var response = await _apiService.GetAsync<ConceptResponse>($"/concepts/{conceptId}");
            return response?.Concept;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Get concept error: {ex.Message}");
            return null;
        }
    }

    public async Task<Concept> CreateConceptAsync(CreateConceptRequest request)
    {
        try
        {
            var response = await _apiService.PostAsync<CreateConceptRequest, ConceptResponse>("/concept/create", request);
            return response?.Concept ?? throw new InvalidOperationException("Failed to create concept");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Create concept error: {ex.Message}");
            throw;
        }
    }

    public async Task<Concept> UpdateConceptAsync(string conceptId, UpdateConceptRequest request)
    {
        try
        {
            var response = await _apiService.PutAsync<UpdateConceptRequest, ConceptResponse>($"/concepts/{conceptId}", request);
            return response?.Concept ?? throw new InvalidOperationException("Failed to update concept");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Update concept error: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> DeleteConceptAsync(string conceptId)
    {
        try
        {
            await _apiService.DeleteAsync<object>($"/concepts/{conceptId}");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Delete concept error: {ex.Message}");
            return false;
        }
    }

    public async Task<List<Concept>> SearchConceptsAsync(ConceptSearchRequest request)
    {
        try
        {
            // Map to existing node search with concept type filter
            var nodeSearchRequest = new NodeSearchRequest
            {
                Query = request.SearchTerm ?? string.Empty,
                Filters = new Dictionary<string, object> { ["typeId"] = "codex.concept" },
                Limit = request.Take ?? 10,
                Skip = request.Skip ?? 0
            };
            var response = await _apiService.PostAsync<NodeSearchRequest, NodeSearchResponse>("/storage-endpoints/nodes/search", nodeSearchRequest);
            return response?.Nodes?.Select(n => MapNodeToConcept(n)).ToList() ?? new List<Concept>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Search concepts error: {ex.Message}");
            return new List<Concept>();
        }
    }

    public async Task<List<Concept>> DiscoverConceptsAsync(ConceptDiscoveryRequest request)
    {
        try
        {
            // Fallback: use node search against content text to discover concepts
            var nodeSearchRequest = new NodeSearchRequest
            {
                Query = request.Content,
                Filters = new Dictionary<string, object> { ["typeId"] = "codex.concept" },
                Limit = request.MaxConcepts ?? 10,
                Skip = 0
            };
            var response = await _apiService.PostAsync<NodeSearchRequest, NodeSearchResponse>("/storage-endpoints/nodes/search", nodeSearchRequest);
            return response?.Nodes?.Select(n => MapNodeToConcept(n)).ToList() ?? new List<Concept>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Discover concepts error: {ex.Message}");
            return new List<Concept>();
        }
    }

    public async Task<ConceptRelationship> RelateConceptsAsync(ConceptRelateRequest request)
    {
        try
        {
            // Map to existing edge creation for concept relationships
            var edgeRequest = new CreateEdgeRequest
            {
                FromId = request.FromConceptId,
                ToId = request.ToConceptId,
                Role = request.RelationshipType,
                Weight = request.Weight,
                Meta = request.Metadata
            };
            var response = await _apiService.PostAsync<CreateEdgeRequest, EdgeResponse>("/storage-endpoints/edges", edgeRequest);
            return new ConceptRelationship
            {
                FromConceptId = request.FromConceptId,
                ToConceptId = request.ToConceptId,
                RelationshipType = request.RelationshipType,
                Weight = request.Weight,
                Metadata = request.Metadata
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Relate concepts error: {ex.Message}");
            throw;
        }
    }

    public async Task<List<Concept>> GetRelatedConceptsAsync(string conceptId)
    {
        try
        {
            // Map to existing graph relationships endpoint
            var response = await _apiService.GetAsync<GraphRelationshipsResponse>($"/graph/relationships/{conceptId}");
            return response?.RelatedNodes?.Where(n => n.TypeId == "codex.concept").Select(n => MapNodeToConcept(n)).ToList() ?? new List<Concept>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Get related concepts error: {ex.Message}");
            return new List<Concept>();
        }
    }

    public async Task<List<Concept>> GetConceptsByInterestAsync(string userId)
    {
        try
        {
            // Map to existing user-concept relationships endpoint
            var response = await _apiService.GetAsync<UserConceptsResponse>($"/userconcept/user-concepts/{userId}");
            return response?.Concepts?.Select(c => MapNodeToConcept(c)).ToList() ?? new List<Concept>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Get concepts by interest error: {ex.Message}");
            return new List<Concept>();
        }
    }

    public async Task<bool> MarkConceptInterestAsync(string userId, string conceptId, bool interested)
    {
        try
        {
            // Map to existing user-concept link/unlink endpoints
            if (interested)
            {
                var linkRequest = new UserConceptLinkRequest { UserId = userId, ConceptId = conceptId, RelationshipType = "interest" };
                await _apiService.PostAsync<UserConceptLinkRequest, object>("/userconcept/link", linkRequest);
            }
            else
            {
                var unlinkRequest = new UserConceptUnlinkRequest { UserId = userId, ConceptId = conceptId };
                await _apiService.PostAsync<UserConceptUnlinkRequest, object>("/userconcept/unlink", unlinkRequest);
            }
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Mark concept interest error: {ex.Message}");
            return false;
        }
    }

    public async Task<ConceptQualityAssessment> AssessConceptQualityAsync(string conceptId)
    {
        try
        {
            // Map to existing contribution analysis endpoint
            var request = new ContributionAnalysisRequest
            {
                EntityId = conceptId,
                AnalysisType = "quality-assessment",
                Parameters = new Dictionary<string, object> { ["conceptId"] = conceptId }
            };
            var response = await _apiService.PostAsync<ContributionAnalysisRequest, ContributionAnalysisResponse>("/contributions/analyze", request);
            return new ConceptQualityAssessment
            {
                ConceptId = conceptId,
                OverallScore = response?.Analysis != null && response.Analysis.TryGetValue("qualityScore", out var scoreObj) && scoreObj is double scoreVal ? scoreVal : 0.0,
                AssessedAt = DateTime.UtcNow,
                Strengths = response?.Analysis != null && response.Analysis.TryGetValue("strengths", out var strengthsObj) && strengthsObj is List<string> strengths ? strengths : new List<string>(),
                Weaknesses = response?.Analysis != null && response.Analysis.TryGetValue("weaknesses", out var weaknessesObj) && weaknessesObj is List<string> weaknesses ? weaknesses : new List<string>(),
                Recommendation = response?.Analysis != null && response.Analysis.TryGetValue("recommendation", out var recObj) ? recObj?.ToString() ?? string.Empty : string.Empty,
                Metrics = response?.Analysis != null && response.Analysis.TryGetValue("metrics", out var metricsObj) && metricsObj is Dictionary<string, double> metrics ? metrics : new Dictionary<string, double>()
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Assess concept quality error: {ex.Message}");
            throw;
        }
    }

    public async Task<List<Concept>> GetTrendingConceptsAsync(int limit = 10)
    {
        try
        {
            // Map to existing news trending endpoint with concept filtering
            var response = await _apiService.GetAsync<TrendingTopicsResponse>($"/news/trending?limit={limit}");
            return response?.Topics?.Where(t => t.Type == "concept").Select(t => new Concept
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                Domain = t.Domain,
                Complexity = t.Complexity,
                Tags = t.Tags?.ToList() ?? new List<string>(),
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                Resonance = t.Resonance,
                Energy = t.Energy,
                IsInterested = false,
                InterestCount = t.MentionCount
            }).ToList() ?? new List<Concept>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Get trending concepts error: {ex.Message}");
            return new List<Concept>();
        }
    }

    public async Task<List<Concept>> GetRecommendedConceptsAsync(string userId, int limit = 10)
    {
        try
        {
            // Map to existing user discovery endpoint with concept recommendations
            var request = new UserDiscoveryRequest
            {
                UserId = userId,
                DiscoveryType = "recommendations",
                Limit = limit
            };
            var response = await _apiService.PostAsync<UserDiscoveryRequest, UserDiscoveryResult>("/users/discover", request);
            return response?.Concepts?.Select(c => MapNodeToConcept(c)).ToList() ?? new List<Concept>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Get recommended concepts error: {ex.Message}");
            return new List<Concept>();
        }
    }

    private string BuildQueryString(ConceptQuery query)
    {
        var parameters = new List<string>();
        
        if (!string.IsNullOrEmpty(query.Domain))
            parameters.Add($"domain={Uri.EscapeDataString(query.Domain)}");
        if (query.Complexity.HasValue)
            parameters.Add($"complexity={query.Complexity.Value}");
        if (!string.IsNullOrEmpty(query.SearchTerm))
            parameters.Add($"search={Uri.EscapeDataString(query.SearchTerm)}");
        if (query.Skip.HasValue)
            parameters.Add($"skip={query.Skip.Value}");
        if (query.Take.HasValue)
            parameters.Add($"take={query.Take.Value}");

        return parameters.Any() ? "?" + string.Join("&", parameters) : "";
    }

    private Concept MapNodeToConcept(Node node)
    {
        return new Concept
        {
            Id = node.Id,
            Name = node.Meta?.GetValueOrDefault("name")?.ToString() ?? node.Title ?? "Unknown",
            Description = node.Meta?.GetValueOrDefault("description")?.ToString() ?? node.Description ?? "",
            Domain = node.Meta?.GetValueOrDefault("domain")?.ToString() ?? "General",
            Complexity = int.TryParse(node.Meta?.GetValueOrDefault("complexity")?.ToString(), out var complexity) ? complexity : 0,
            Tags = node.Meta?.GetValueOrDefault("tags")?.ToString()?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList() ?? new List<string>(),
            CreatedAt = node.Meta?.GetValueOrDefault("createdAt") is DateTime created ? created : DateTime.UtcNow,
            UpdatedAt = node.Meta?.GetValueOrDefault("updatedAt") is DateTime updated ? updated : DateTime.UtcNow,
            Resonance = 0.75, // Default resonance
            Energy = 500.0, // Default energy
            IsInterested = false, // Default interest
            InterestCount = 0 // Default interest count
        };
    }
}
