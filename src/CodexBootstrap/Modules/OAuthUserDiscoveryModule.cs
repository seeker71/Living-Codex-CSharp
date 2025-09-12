using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.Authentication.Facebook;
using AspNet.Security.OAuth.Twitter;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
// Removed GeoCoordinate dependency - using custom Haversine implementation
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

// OAuth and User Discovery response types
public record OAuthProviderInfo(string Provider, string DisplayName, string ClientId, bool IsEnabled);
public record OAuthProvidersResponse(List<OAuthProviderInfo> Providers, int Count);
public record OAuthLoginResponse(string Provider, string RedirectUrl, string State);
public record OAuthCallbackResponse(string Provider, bool Success, string? UserId = null, string? Error = null);

public record UserDiscoveryRequest(
    string? Interests = null,
    string? Contributions = null,
    string? Location = null,
    double? RadiusKm = null,
    string? ConceptId = null,
    string? OntologyLevel = null,
    int? Limit = 50
);

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

public record UserDiscoveryResult(
    List<UserProfile> Users,
    int TotalCount,
    string QueryType,
    Dictionary<string, object> SearchMetadata = null
);

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

public record ConceptContributorsResponse(
    string ConceptId,
    List<ConceptContributor> Contributors,
    int TotalCount,
    Dictionary<string, int> ContributionTypeCounts
);

[MetaNode(Id = "codex.oauth-discovery", Name = "OAuth User Discovery Module", Description = "OAuth authentication and advanced user discovery system")]
public sealed class OAuthUserDiscoveryModule : IModule
{
    private readonly NodeRegistry _registry;
    private readonly CodexBootstrap.Core.ILogger _logger;
    private readonly HttpClient _httpClient;

    public OAuthUserDiscoveryModule(NodeRegistry registry, CodexBootstrap.Core.ILogger logger, HttpClient httpClient)
    {
        _registry = registry;
        _logger = logger;
        _httpClient = httpClient;
    }

    public Node GetModuleNode()
    {
        return NodeStorage.CreateModuleNode(
            id: "codex.oauth-discovery",
            name: "OAuth User Discovery Module",
            version: "1.0.0",
            description: "OAuth authentication with Google, Microsoft, GitHub, Facebook, Twitter and advanced user discovery",
            capabilities: new[] { "oauth", "authentication", "user-discovery", "geo-location", "concept-matching" },
            tags: new[] { "oauth", "auth", "discovery", "users", "geo", "concepts" },
            specReference: "codex.spec.oauth-discovery"
        );
    }

    // OAuth Provider Management
    public OAuthProvidersResponse GetOAuthProviders()
    {
        var providers = new List<OAuthProviderInfo>
        {
            new("Google", "Google", Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") ?? "", !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID"))),
            new("Microsoft", "Microsoft", Environment.GetEnvironmentVariable("MICROSOFT_CLIENT_ID") ?? "", !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MICROSOFT_CLIENT_ID"))),
            new("GitHub", "GitHub", Environment.GetEnvironmentVariable("GITHUB_CLIENT_ID") ?? "", !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_CLIENT_ID"))),
            new("Facebook", "Facebook", Environment.GetEnvironmentVariable("FACEBOOK_CLIENT_ID") ?? "", !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FACEBOOK_CLIENT_ID"))),
            new("Twitter", "Twitter", Environment.GetEnvironmentVariable("TWITTER_CLIENT_ID") ?? "", !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TWITTER_CLIENT_ID")))
        };

        return new OAuthProvidersResponse(providers, providers.Count);
    }

    // User Discovery by Interests
    public async Task<UserDiscoveryResult> DiscoverUsersByInterests(string interests, int limit = 50)
    {
        try
        {
            var interestList = interests.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(i => i.Trim().ToLowerInvariant())
                .ToList();

            var users = _registry.GetNodesByType("codex.user")
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
                .Where(u => u.Interests?.Any(i => interestList.Any(interest => i.Contains(interest, StringComparison.OrdinalIgnoreCase))) == true)
                .Take(limit)
                .ToList();

            return new UserDiscoveryResult(users, users.Count, "interests");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error discovering users by interests: {ex.Message}");
            return new UserDiscoveryResult(new List<UserProfile>(), 0, "interests");
        }
    }

    // User Discovery by Geo-location
    public async Task<UserDiscoveryResult> DiscoverUsersByLocation(string location, double radiusKm = 50, int limit = 50)
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

            var users = _registry.GetNodesByType("codex.user")
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
                .Where(u => CalculateDistance(coordinates.Value.Latitude, coordinates.Value.Longitude, u.Latitude, u.Longitude) <= radiusKm)
                .OrderBy(u => CalculateDistance(coordinates.Value.Latitude, coordinates.Value.Longitude, u.Latitude, u.Longitude))
                .Take(limit)
                .ToList();

            return new UserDiscoveryResult(users, users.Count, "location", 
                new Dictionary<string, object> 
                { 
                    ["searchLocation"] = location,
                    ["searchCoordinates"] = coordinates,
                    ["radiusKm"] = radiusKm
                });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error discovering users by location: {ex.Message}");
            return new UserDiscoveryResult(new List<UserProfile>(), 0, "location");
        }
    }

    // Find Contributors for Concepts
    public async Task<ConceptContributorsResponse> FindConceptContributors(string conceptId, string? ontologyLevel = null, int limit = 100)
    {
        try
        {
            var contributors = new List<ConceptContributor>();
            
            // Get concept node
            if (!_registry.TryGet(conceptId, out var conceptNode))
            {
                return new ConceptContributorsResponse(conceptId, new List<ConceptContributor>(), 0, new Dictionary<string, int>());
            }

            // Find users who have contributed to this concept or related concepts
            var relatedConcepts = GetRelatedConcepts(conceptId, ontologyLevel);
            
            foreach (var userId in GetUsersWithContributions(relatedConcepts))
            {
                if (_registry.TryGet(userId, out var userNode))
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
            _logger.Error($"Error finding concept contributors: {ex.Message}");
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
                _logger.Warn("OPENCAGE_API_KEY not configured, using fallback geocoding");
                return await FallbackGeocode(location);
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
            _logger.Error($"Error geocoding location '{location}': {ex.Message}");
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
        // Get concepts related through edges - simplified for now
        var relatedConcepts = new List<string> { conceptId };
        
        // TODO: Implement proper edge traversal when GetEdges is available
        // For now, return just the concept itself
        return relatedConcepts;
    }

    private List<string> GetUsersWithContributions(List<string> conceptIds)
    {
        var userIds = new HashSet<string>();
        
        // TODO: Implement proper edge traversal when GetEdges is available
        // For now, return empty list
        return userIds.ToList();
    }

    private double CalculateContributionScore(string userId, string conceptId, List<string> relatedConcepts)
    {
        // TODO: Implement proper edge traversal when GetEdges is available
        // For now, return basic score
        return 0.1; // Basic scoring
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

    public void Register(NodeRegistry registry)
    {
        registry.Upsert(GetModuleNode());

        // Register API nodes
        var oauthProvidersApi = NodeStorage.CreateApiNode("codex.oauth-discovery", "oauth-providers", "/oauth/providers", "Get available OAuth providers");
        var discoverUsersApi = NodeStorage.CreateApiNode("codex.oauth-discovery", "discover-users", "/users/discover", "Discover users by interests, location, or contributions");
        var conceptContributorsApi = NodeStorage.CreateApiNode("codex.oauth-discovery", "concept-contributors", "/concepts/{conceptId}/contributors", "Find contributors for a concept");
        
        registry.Upsert(oauthProvidersApi);
        registry.Upsert(discoverUsersApi);
        registry.Upsert(conceptContributorsApi);

        // Register edges
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.oauth-discovery", "oauth-providers"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.oauth-discovery", "discover-users"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.oauth-discovery", "concept-contributors"));
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        router.Register("codex.oauth-discovery", "oauth-providers", args =>
        {
            var result = GetOAuthProviders();
            return Task.FromResult<object>(result);
        });

        router.Register("codex.oauth-discovery", "discover-users", args =>
        {
            try
            {
                if (args == null || !args.HasValue) return Task.FromResult<object>(new ErrorResponse("Missing request parameters"));

                var request = JsonSerializer.Deserialize<UserDiscoveryRequest>(args.Value.GetRawText());
                if (request == null) return Task.FromResult<object>(new ErrorResponse("Invalid request format"));

                Task<UserDiscoveryResult> discoveryTask = request switch
                {
                    { Interests: not null } => DiscoverUsersByInterests(request.Interests, request.Limit ?? 50),
                    { Location: not null } => DiscoverUsersByLocation(request.Location, request.RadiusKm ?? 50, request.Limit ?? 50),
                    _ => Task.FromResult(new UserDiscoveryResult(new List<UserProfile>(), 0, "invalid"))
                };

                return discoveryTask.ContinueWith(t => (object)t.Result);
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>(new ErrorResponse($"Error discovering users: {ex.Message}"));
            }
        });

        router.Register("codex.oauth-discovery", "concept-contributors", args =>
        {
            try
            {
                if (args == null || !args.HasValue) return Task.FromResult<object>(new ErrorResponse("Missing request parameters"));

                var conceptId = args.Value.TryGetProperty("conceptId", out var idElement) ? idElement.GetString() : null;
                var ontologyLevel = args.Value.TryGetProperty("ontologyLevel", out var levelElement) ? levelElement.GetString() : null;
                var limit = args.Value.TryGetProperty("limit", out var limitElement) ? limitElement.GetInt32() : 100;

                if (string.IsNullOrEmpty(conceptId)) return Task.FromResult<object>(new ErrorResponse("Concept ID is required"));

                var result = FindConceptContributors(conceptId, ontologyLevel, limit);
                return Task.FromResult<object>(result);
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>(new ErrorResponse($"Error finding concept contributors: {ex.Message}"));
            }
        });
    }

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // OAuth providers endpoint
        app.MapGet("/oauth/providers", () =>
        {
            var result = GetOAuthProviders();
            return Results.Ok(result);
        });

        // User discovery endpoint
        app.MapPost("/users/discover", async (UserDiscoveryRequest request) =>
        {
            try
            {
                UserDiscoveryResult result = request switch
                {
                    { Interests: not null } => await DiscoverUsersByInterests(request.Interests, request.Limit ?? 50),
                    { Location: not null } => await DiscoverUsersByLocation(request.Location, request.RadiusKm ?? 50, request.Limit ?? 50),
                    _ => new UserDiscoveryResult(new List<UserProfile>(), 0, "invalid")
                };

                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error discovering users: {ex.Message}");
            }
        });

        // Concept contributors endpoint
        app.MapGet("/concepts/{conceptId}/contributors", async (string conceptId, string? ontologyLevel, int? limit) =>
        {
            try
            {
                var result = await FindConceptContributors(conceptId, ontologyLevel, limit ?? 100);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error finding concept contributors: {ex.Message}");
            }
        });

        // OAuth challenge endpoints
        app.MapGet("/oauth/challenge/{provider}", (string provider) =>
        {
            var challengeUrl = $"/oauth/{provider.ToLowerInvariant()}";
            return Results.Redirect(challengeUrl);
        });

        // OAuth callback endpoints
        app.MapGet("/oauth/callback/{provider}", async (string provider, HttpContext context) =>
        {
            var result = await context.AuthenticateAsync(provider);
            if (result.Succeeded)
            {
                var user = result.Principal;
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
                
                if (!string.IsNullOrEmpty(userId))
                {
                    // Store user in node registry
                    await StoreOAuthUser(userId, user, provider);
                    return Results.Ok(new OAuthCallbackResponse(provider, true, userId));
                }
            }
            
            return Results.BadRequest(new OAuthCallbackResponse(provider, false, Error: "Authentication failed"));
        });
    }

    private async Task StoreOAuthUser(string userId, ClaimsPrincipal principal, string provider)
    {
        try
        {
            var userNode = new Node(
                Id: $"user-{userId}",
                TypeId: "codex.user",
                State: ContentState.Ice,
                Locale: "en",
                Title: principal.FindFirst(ClaimTypes.Name)?.Value ?? principal.FindFirst("name")?.Value ?? "Unknown User",
                Description: $"User authenticated via {provider}",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(new
                    {
                        provider = provider,
                        authenticatedAt = DateTime.UtcNow,
                        claims = principal.Claims.ToDictionary(c => c.Type, c => c.Value)
                    }),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["displayName"] = principal.FindFirst(ClaimTypes.Name)?.Value ?? principal.FindFirst("name")?.Value ?? "Unknown",
                    ["email"] = principal.FindFirst(ClaimTypes.Email)?.Value ?? principal.FindFirst("email")?.Value ?? "",
                    ["avatarUrl"] = principal.FindFirst("picture")?.Value ?? principal.FindFirst("avatar_url")?.Value ?? "",
                    ["provider"] = provider,
                    ["authenticatedAt"] = DateTime.UtcNow.ToString("O"),
                    ["interests"] = "",
                    ["contributions"] = "",
                    ["location"] = ""
                }
            );

            _registry.Upsert(userNode);
            _logger.Info($"Stored OAuth user {userId} from {provider}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error storing OAuth user {userId}: {ex.Message}");
        }
    }
}
