using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// User Discovery Module - Advanced user discovery by interests, location, and concept relationships
/// Provides sophisticated user discovery capabilities including interest matching, geo-location discovery,
/// and concept contributor discovery with generic relationship support
/// </summary>
[MetaNode(Id = "codex.user-discovery", Name = "User Discovery Module", Description = "Advanced user discovery system with interest matching, geo-location, and concept relationships")]
public sealed class UserDiscoveryModule : ModuleBase
{
    private readonly HttpClient _httpClient;

    public override string Name => "User Discovery Module";
    public override string Description => "Advanced user discovery system with interest matching, geo-location, and concept relationships";
    public override string Version => "1.0.0";

    public UserDiscoveryModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient) 
        : base(registry, logger)
    {
        _httpClient = httpClient;
    }

    // Parameterless constructor for module loader

    /// <summary>
    /// Gets the registry to use - now always the unified registry
    /// </summary>
    private INodeRegistry Registry => _registry;

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.user-discovery",
            name: Name,
            version: Version,
            description: Description,
            tags: new[] { "discovery", "users", "geo", "concepts", "relationships", "matching" },
            capabilities: new[] { 
                "user-discovery", "interest-matching", "geo-location", "concept-relationships", 
                "contributor-discovery", "ontology-search", "relationship-queries" 
            },
            spec: "codex.spec.user-discovery"
        );
    }

    public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
    {
        // API handlers are registered via attribute-based routing
        _logger.Info("User Discovery Module API handlers registered");
    }

    public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // HTTP endpoints are registered via attribute-based routing
        _logger.Info("User Discovery Module HTTP endpoints registered");
    }

    // User Discovery by Interests
    [ApiRoute("POST", "/users/discover", "Discover Users", "Discover users by interests, location, or contributions", "codex.user-discovery")]
    public async Task<object> DiscoverUsers([ApiParameter("body", "User discovery request", Required = true, Location = "body")] UserDiscoveryRequest request)
    {
        try
        {
            var skip = request.Skip ?? 0;
            var limit = request.Limit ?? 50;

            UserDiscoveryResult result = request switch
            {
                { Interests: not null } => await DiscoverUsersByInterests(request.Interests, skip, limit),
                { Location: not null } => await DiscoverUsersByLocation(request.Location, request.RadiusKm ?? 50, skip, limit),
                { ConceptId: not null } => await DiscoverUsersByConcept(request.ConceptId, request.OntologyLevel, skip, limit),
                _ => new UserDiscoveryResult(new List<UserProfile>(), 0, "invalid")
            };

            return result;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error discovering users: {ex.Message}", ex);
            return new ErrorResponse($"Error discovering users: {ex.Message}");
        }
    }

    // User Discovery by Interests (helper method)
    public async Task<UserDiscoveryResult> DiscoverUsersByInterests(List<string> interests, int skip = 0, int limit = 50)
    {
        try
        {
            var interestList = interests
                .Select(i => i.Trim().ToLowerInvariant())
                .ToList();

            var query = Registry.GetNodesByType("codex.user")
                .Where(u => u.Meta?.ContainsKey("interests") == true)
                .Select(u => new UserProfile(
                    UserId: u.Id,
                    DisplayName: u.Meta?.GetValueOrDefault("displayName")?.ToString() ?? u.Title ?? "Unknown",
                    Email: u.Meta?.GetValueOrDefault("email")?.ToString() ?? "",
                    AvatarUrl: u.Meta?.GetValueOrDefault("avatarUrl")?.ToString(),
                    Location: u.Meta?.GetValueOrDefault("location")?.ToString(),
                    Latitude: u.Meta?.GetValueOrDefault("latitude")?.ToString() != null ? double.Parse(u.Meta["latitude"].ToString()) : null,
                    Longitude: u.Meta?.GetValueOrDefault("longitude")?.ToString() != null ? double.Parse(u.Meta["longitude"].ToString()) : null,
                    Interests: ParseStringList(u.Meta?.GetValueOrDefault("interests")?.ToString()),
                    Contributions: ParseStringList(u.Meta?.GetValueOrDefault("contributions")?.ToString()),
                    Metadata: u.Meta?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, object>()
                ))
                .Where(u => u.Interests?.Any(i => interestList.Any(interest => i.Contains(interest, StringComparison.OrdinalIgnoreCase))) == true);

            var total = query.Count();
            var users = query.Skip(skip).Take(limit).ToList();

            return new UserDiscoveryResult(users, total, "interests");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error discovering users by interests: {ex.Message}", ex);
            return new UserDiscoveryResult(new List<UserProfile>(), 0, "interests");
        }
    }

    // User Discovery by Geo-location
    public async Task<UserDiscoveryResult> DiscoverUsersByLocation(string location, double radiusKm = 50, int skip = 0, int limit = 50)
    {
        try
        {
            // Geocode the input location
            var coordinates = await GeocodeLocation(location);
            if (coordinates == null)
            {
                return new UserDiscoveryResult(new List<UserProfile>(), 0, "location", 
                    new Dictionary<string, object> { ["error"] = "Could not geocode location" });
            }

            var query = Registry.GetNodesByType("codex.user")
                .Where(u => u.Meta?.ContainsKey("latitude") == true && u.Meta?.ContainsKey("longitude") == true)
                .Select(u => new UserProfile(
                    UserId: u.Id,
                    DisplayName: u.Meta?.GetValueOrDefault("displayName")?.ToString() ?? u.Title ?? "Unknown",
                    Email: u.Meta?.GetValueOrDefault("email")?.ToString() ?? "",
                    AvatarUrl: u.Meta?.GetValueOrDefault("avatarUrl")?.ToString(),
                    Location: u.Meta?.GetValueOrDefault("location")?.ToString(),
                    Latitude: double.Parse(u.Meta["latitude"].ToString()),
                    Longitude: double.Parse(u.Meta["longitude"].ToString()),
                    Interests: ParseStringList(u.Meta?.GetValueOrDefault("interests")?.ToString()),
                    Contributions: ParseStringList(u.Meta?.GetValueOrDefault("contributions")?.ToString()),
                    Metadata: u.Meta?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, object>()
                ))
                .Where(u => CalculateDistance(coordinates.Value.Latitude, coordinates.Value.Longitude, u.Latitude, u.Longitude) <= radiusKm);

            var total = query.Count();
            var users = query
                .OrderBy(u => CalculateDistance(coordinates.Value.Latitude, coordinates.Value.Longitude, u.Latitude, u.Longitude))
                .Skip(skip)
                .Take(limit)
                .ToList();

            return new UserDiscoveryResult(users, total, "location", 
                new Dictionary<string, object> 
                { 
                    ["searchLocation"] = location,
                    ["searchCoordinates"] = coordinates,
                    ["radiusKm"] = radiusKm
                });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error discovering users by location: {ex.Message}", ex);
            return new UserDiscoveryResult(new List<UserProfile>(), 0, "location");
        }
    }

    // User Discovery by Concept
    public async Task<UserDiscoveryResult> DiscoverUsersByConcept(string conceptId, string? ontologyLevel, int skip = 0, int limit = 50)
    {
        try
        {
            var contributors = await FindConceptContributors(conceptId, ontologyLevel, int.MaxValue);
            var users = contributors.Contributors.Select(c => new UserProfile(
                UserId: c.UserId,
                DisplayName: c.DisplayName,
                Email: c.Email,
                AvatarUrl: c.AvatarUrl,
                Location: c.Location,
                Latitude: null, // Would need to be fetched from user profile
                Longitude: null,
                Interests: c.RelevantInterests,
                Contributions: new List<string> { c.ContributionType },
                Metadata: new Dictionary<string, object> 
                { 
                    ["contributionScore"] = c.ContributionScore,
                    ["contributionType"] = c.ContributionType
                }
            )).ToList();

            var total = users.Count;
            var paged = users.Skip(skip).Take(limit).ToList();

            return new UserDiscoveryResult(paged, total, "concept", 
                new Dictionary<string, object> 
                { 
                    ["conceptId"] = conceptId,
                    ["ontologyLevel"] = ontologyLevel,
                    ["contributionTypeCounts"] = contributors.ContributionTypeCounts
                });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error discovering users by concept: {ex.Message}", ex);
            return new UserDiscoveryResult(new List<UserProfile>(), 0, "concept");
        }
    }

    // Find Contributors for Concepts
    [ApiRoute("GET", "/concepts/{conceptId}/contributors", "Find Concept Contributors", "Find contributors, subscribers, and investors for a concept", "codex.user-discovery")]
    public async Task<ConceptContributorsResponse> FindConceptContributors(
        [ApiParameter("path", "ID of the concept to find contributors for", Required = true, Location = "path")] string conceptId, 
        [ApiParameter("query", "Ontology level to search within", Required = false, Location = "query")] string? ontologyLevel = null, 
        [ApiParameter("query", "Maximum number of contributors to return", Required = false, Location = "query")] int limit = 100)
    {
        try
        {
            var contributors = new List<ConceptContributor>();
            
            // Get concept node
            if (!Registry.TryGet(conceptId, out var conceptNode))
            {
                return new ConceptContributorsResponse(conceptId, new List<ConceptContributor>(), 0, new Dictionary<string, int>());
            }

            // Find users who have contributed to this concept or related concepts
            var relatedConcepts = GetRelatedConcepts(conceptId, ontologyLevel);
            
            foreach (var userId in GetUsersWithContributions(relatedConcepts))
            {
                if (Registry.TryGet(userId, out var userNode))
                {
                    var contributionScore = CalculateContributionScore(userId, conceptId, relatedConcepts);
                    var contributionType = DetermineContributionType(contributionScore);
                    
                    contributors.Add(new ConceptContributor(
                        UserId: userId,
                        DisplayName: userNode.Meta?.GetValueOrDefault("displayName")?.ToString() ?? userNode.Title ?? "Unknown",
                        Email: userNode.Meta?.GetValueOrDefault("email")?.ToString() ?? "",
                        ContributionType: contributionType,
                        ContributionScore: contributionScore,
                        AvatarUrl: userNode.Meta?.GetValueOrDefault("avatarUrl")?.ToString(),
                        Location: userNode.Meta?.GetValueOrDefault("location")?.ToString(),
                        RelevantInterests: ParseStringList(userNode.Meta?.GetValueOrDefault("interests")?.ToString())
                    ));
                }
            }

            contributors = contributors
                .OrderByDescending(c => c.ContributionScore)
                .Take(limit)
                .ToList();

            var typeCounts = contributors
                .GroupBy(c => c.ContributionType)
                .ToDictionary(g => g.Key, g => g.Count());

            return new ConceptContributorsResponse(conceptId, contributors, contributors.Count, typeCounts);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error finding concept contributors: {ex.Message}", ex);
            return new ConceptContributorsResponse(conceptId, new List<ConceptContributor>(), 0, new Dictionary<string, int>());
        }
    }

    // Geocoding using external service
    private async Task<(double Latitude, double Longitude)?> GeocodeLocation(string location)
    {
        try
        {
            // Using OpenCage Geocoding API (free tier available)
            var apiKey = Environment.GetEnvironmentVariable("OPENCAGE_API_KEY");
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.Warn("OPENCAGE_API_KEY not configured, geocoding unavailable");
                return null;
            }

            var encodedLocation = Uri.EscapeDataString(location);
            var url = $"https://api.opencagedata.com/geocode/v1/json?q={encodedLocation}&key={apiKey}&limit=1";
            
            var response = await _httpClient.GetStringAsync(url);
            var json = JsonDocument.Parse(response);
            
            if (json.RootElement.TryGetProperty("results", out var results) && results.GetArrayLength() > 0)
            {
                var result = results[0];
                if (result.TryGetProperty("geometry", out var geometry))
                {
                    var lat = geometry.GetProperty("lat").GetDouble();
                    var lng = geometry.GetProperty("lng").GetDouble();
                    return (lat, lng);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error geocoding location '{location}': {ex.Message}", ex);
        }

        return await FallbackGeocode(location);
    }

    // Fallback geocoding for common locations
    private async Task<(double Latitude, double Longitude)?> FallbackGeocode(string location)
    {
        // Simple fallback for major cities
        var majorCities = new Dictionary<string, (double, double)>(StringComparer.OrdinalIgnoreCase)
        {
            ["new york"] = (40.7128, -74.0060),
            ["london"] = (51.5074, -0.1278),
            ["paris"] = (48.8566, 2.3522),
            ["tokyo"] = (35.6762, 139.6503),
            ["san francisco"] = (37.7749, -122.4194),
            ["los angeles"] = (34.0522, -118.2437),
            ["chicago"] = (41.8781, -87.6298),
            ["boston"] = (42.3601, -71.0589),
            ["seattle"] = (47.6062, -122.3321),
            ["austin"] = (30.2672, -97.7431)
        };

        var normalizedLocation = location.ToLowerInvariant().Trim();
        if (majorCities.TryGetValue(normalizedLocation, out var coords))
        {
            return coords;
        }

        return null;
    }

    // Calculate distance between two coordinates using Haversine formula
    private double CalculateDistance(double lat1, double lon1, double? lat2, double? lon2)
    {
        if (lat2 == null || lon2 == null) return double.MaxValue;
        
        const double R = 6371; // Earth's radius in kilometers
        var dLat = ToRadians(lat2.Value - lat1);
        var dLon = ToRadians(lon2.Value - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2.Value)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private double ToRadians(double degrees)
    {
        return degrees * (Math.PI / 180);
    }

    // Helper methods
    private List<string> ParseStringList(string? value)
    {
        if (string.IsNullOrEmpty(value)) return new List<string>();
        return value.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .ToList();
    }

    private List<string> GetRelatedConcepts(string conceptId, string? ontologyLevel)
    {
        // Get concepts related through edges
        var relatedConcepts = new List<string> { conceptId };
        
        try
        {
            // Find edges from this concept
            var outgoingEdges = Registry.GetEdgesFrom(conceptId);
            foreach (var edge in outgoingEdges)
            {
                if (!relatedConcepts.Contains(edge.ToId))
                {
                    relatedConcepts.Add(edge.ToId);
                }
            }
            
            // Find edges to this concept
            var incomingEdges = Registry.GetEdgesTo(conceptId);
            foreach (var edge in incomingEdges)
            {
                if (!relatedConcepts.Contains(edge.FromId))
                {
                    relatedConcepts.Add(edge.FromId);
                }
            }
            
            // Filter by ontology level if specified
            if (!string.IsNullOrEmpty(ontologyLevel))
            {
                relatedConcepts = relatedConcepts.Where(conceptId => 
                {
                    if (Registry.TryGet(conceptId, out var node))
                    {
                        return node.Meta?.GetValueOrDefault("ontologyLevel")?.ToString() == ontologyLevel;
                    }
                    return false;
                }).ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting related concepts for {conceptId}: {ex.Message}", ex);
        }
        
        return relatedConcepts;
    }

    private List<string> GetUsersWithContributions(List<string> conceptIds)
    {
        var userIds = new HashSet<string>();
        
        try
        {
            foreach (var conceptId in conceptIds)
            {
                // Find users who have contributed to this concept
                var contributionEdges = Registry.GetEdgesTo(conceptId).Where(e => e.Role == "contribution");
                foreach (var edge in contributionEdges)
                {
                    if (Registry.TryGet(edge.FromId, out var userNode) && 
                        userNode.TypeId == "user")
                    {
                        userIds.Add(edge.FromId);
                    }
                }
                
                // Find users who are subscribed to this concept
                var subscriptionEdges = Registry.GetEdgesTo(conceptId).Where(e => e.Role == "subscription");
                foreach (var edge in subscriptionEdges)
                {
                    if (Registry.TryGet(edge.FromId, out var userNode) && 
                        userNode.TypeId == "user")
                    {
                        userIds.Add(edge.FromId);
                    }
                }
                
                // Find users who have invested in this concept
                var investmentEdges = Registry.GetEdgesTo(conceptId).Where(e => e.Role == "investment");
                foreach (var edge in investmentEdges)
                {
                    if (Registry.TryGet(edge.FromId, out var userNode) && 
                        userNode.TypeId == "user")
                    {
                        userIds.Add(edge.FromId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting users with contributions: {ex.Message}", ex);
        }
        
        return userIds.ToList();
    }

    private double CalculateContributionScore(string userId, string conceptId, List<string> relatedConcepts)
    {
        double score = 0.0;
        
        try
        {
            foreach (var relatedConceptId in relatedConcepts)
            {
                // Check for direct contributions
                var contributionEdges = Registry.GetEdgesFrom(userId).Where(e => e.ToId == relatedConceptId && e.Role == "contribution");
                score += contributionEdges.Count() * 1.0;
                
                // Check for subscriptions (lower weight)
                var subscriptionEdges = Registry.GetEdgesFrom(userId).Where(e => e.ToId == relatedConceptId && e.Role == "subscription");
                score += subscriptionEdges.Count() * 0.3;
                
                // Check for investments (higher weight)
                var investmentEdges = Registry.GetEdgesFrom(userId).Where(e => e.ToId == relatedConceptId && e.Role == "investment");
                score += investmentEdges.Count() * 2.0;
                
                // Check for comments/feedback (medium weight)
                var feedbackEdges = Registry.GetEdgesFrom(userId).Where(e => e.ToId == relatedConceptId && e.Role == "feedback");
                score += feedbackEdges.Count() * 0.5;
            }
            
            // Normalize by number of related concepts
            if (relatedConcepts.Count > 0)
            {
                score = score / relatedConcepts.Count;
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error calculating contribution score for user {userId}: {ex.Message}", ex);
        }
        
        return Math.Max(score, 0.1); // Minimum score
    }

    private string DetermineContributionType(double score)
    {
        return score switch
        {
            >= 1.0 => "investor",
            >= 0.5 => "contributor",
            _ => "subscriber"
        };
    }
}

// Response types for user discovery
[MetaNode(Id = "codex.user-discovery.request", Name = "User Discovery Request", Description = "Request for discovering users by various criteria")]
public record UserDiscoveryRequest(
    List<string>? Interests = null,
    List<string>? Contributions = null,
    string? Location = null,
    double? RadiusKm = null,
    string? ConceptId = null,
    string? OntologyLevel = null,
    int? Limit = 50,
    int? Skip = 0
);

[MetaNode(Id = "codex.user-discovery.profile", Name = "User Profile", Description = "User profile information")]
public record UserProfile(
    string UserId,
    string DisplayName,
    string Email,
    string? AvatarUrl,
    string? Location,
    double? Latitude,
    double? Longitude,
    List<string>? Interests,
    List<string>? Contributions,
    Dictionary<string, object>? Metadata
);

[MetaNode(Id = "codex.user-discovery.result", Name = "User Discovery Result", Description = "Result of user discovery operation")]
public record UserDiscoveryResult(
    List<UserProfile> Users,
    int TotalCount,
    string QueryType,
    Dictionary<string, object>? SearchMetadata = null
);

[MetaNode(Id = "codex.user-discovery.concept-contributor", Name = "Concept Contributor", Description = "Information about a concept contributor")]
public record ConceptContributor(
    string UserId,
    string DisplayName,
    string Email,
    string ContributionType, // "contributor", "subscriber", "investor"
    double ContributionScore,
    string? AvatarUrl,
    string? Location,
    List<string>? RelevantInterests
);

[MetaNode(Id = "codex.user-discovery.concept-contributors-response", Name = "Concept Contributors Response", Description = "Response containing concept contributors")]
public record ConceptContributorsResponse(
    string ConceptId,
    List<ConceptContributor> Contributors,
    int TotalCount,
    Dictionary<string, int> ContributionTypeCounts
);
