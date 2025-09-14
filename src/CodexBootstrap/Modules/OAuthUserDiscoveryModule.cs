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
[MetaNode(Id = "codex.oauth.provider-info", Name = "OAuth Provider Info", Description = "Information about an OAuth provider")]
public record OAuthProviderInfo(string Provider, string DisplayName, string ClientId, bool IsEnabled);

[MetaNode(Id = "codex.oauth.providers-response", Name = "OAuth Providers Response", Description = "Response containing available OAuth providers")]
public record OAuthProvidersResponse(List<OAuthProviderInfo> Providers, int Count);

[MetaNode(Id = "codex.oauth.login-response", Name = "OAuth Login Response", Description = "Response for OAuth login initiation")]
public record OAuthLoginResponse(string Provider, string RedirectUrl, string State);

[MetaNode(Id = "codex.oauth.callback-response", Name = "OAuth Callback Response", Description = "Response for OAuth callback processing")]
public record OAuthCallbackResponse(string Provider, bool Success, string? UserId = null, string? Error = null);

[MetaNode(Id = "codex.oauth.user-discovery-request", Name = "User Discovery Request", Description = "Request for discovering users by various criteria")]
public record UserDiscoveryRequest(
    List<string>? Interests = null,
    List<string>? Contributions = null,
    string? Location = null,
    double? RadiusKm = null,
    string? ConceptId = null,
    string? OntologyLevel = null,
    int? Limit = 50
);

[MetaNode(Id = "codex.oauth.user-profile", Name = "User Profile", Description = "User profile information")]
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

[MetaNode(Id = "codex.oauth.validation-request", Name = "OAuth Validation Request", Description = "Request for OAuth validation with secret")]
public record OAuthValidationRequest(
    string Provider,
    string Secret,
    string UserId,
    string Email,
    string Name
);

[MetaNode(Id = "codex.oauth.session-data", Name = "Session Data", Description = "Session data stored in cookie")]
public record SessionData(
    string UserId,
    string Provider,
    string Email,
    string Name,
    DateTimeOffset ExpiresAt
);

[MetaNode(Id = "codex.oauth.user-discovery-result", Name = "User Discovery Result", Description = "Result of user discovery operation")]
public record UserDiscoveryResult(
    List<UserProfile> Users,
    int TotalCount,
    string QueryType,
    Dictionary<string, object> SearchMetadata = null
);

[MetaNode(Id = "codex.oauth.concept-contributor", Name = "Concept Contributor", Description = "Information about a concept contributor")]
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

[MetaNode(Id = "codex.oauth.concept-contributors-response", Name = "Concept Contributors Response", Description = "Response containing concept contributors")]
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
    private readonly CodexBootstrap.Core.ICodexLogger _logger;
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, OAuthState> _oauthStates = new();
    private readonly string _baseUrl;

    public OAuthUserDiscoveryModule(NodeRegistry registry, CodexBootstrap.Core.ICodexLogger logger, HttpClient httpClient)
    {
        _registry = registry;
        _logger = logger;
        _httpClient = httpClient;
        _baseUrl = Environment.GetEnvironmentVariable("BASE_URL") ?? "http://localhost:5002";
    }

    // Parameterless constructor for module loader
    public OAuthUserDiscoveryModule() : this(new NodeRegistry(), new Log4NetLogger(typeof(OAuthUserDiscoveryModule)), new HttpClient())
    {
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
    [ApiRoute("GET", "/oauth/providers", "Get OAuth Providers", "Get available OAuth providers", "codex.oauth-discovery")]
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
    [ApiRoute("POST", "/users/discover", "Discover Users", "Discover users by interests, location, or contributions", "codex.oauth-discovery")]
    public async Task<object> DiscoverUsers([ApiParameter("request", "User discovery request", Required = true, Location = "body")] UserDiscoveryRequest request)
    {
        try
        {
            UserDiscoveryResult result = request switch
            {
                { Interests: not null } => await DiscoverUsersByInterests(request.Interests, request.Limit ?? 50),
                { Location: not null } => await DiscoverUsersByLocation(request.Location, request.RadiusKm ?? 50, request.Limit ?? 50),
                _ => new UserDiscoveryResult(new List<UserProfile>(), 0, "invalid")
            };

            return result;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error discovering users: {ex.Message}");
            return new ErrorResponse($"Error discovering users: {ex.Message}");
        }
    }

    // User Discovery by Interests (helper method)
    public async Task<UserDiscoveryResult> DiscoverUsersByInterests(List<string> interests, int limit = 50)
    {
        try
        {
            var interestList = interests
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
    [ApiRoute("GET", "/concepts/{conceptId}/contributors", "Find Concept Contributors", "Find contributors, subscribers, and investors for a concept", "codex.oauth-discovery")]
    public async Task<ConceptContributorsResponse> FindConceptContributors([ApiParameter("conceptId", "ID of the concept to find contributors for", Required = true, Location = "path")] string conceptId, [ApiParameter("ontologyLevel", "Ontology level to search within", Required = false, Location = "query")] string? ontologyLevel = null, [ApiParameter("limit", "Maximum number of contributors to return", Required = false, Location = "query")] int limit = 100)
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

    // OAuth Login Endpoints
    [ApiRoute("GET", "/oauth/google/login", "Google OAuth Login", "Initiate Google OAuth login", "codex.oauth-discovery")]
    public async Task<object> GoogleLoginAsync([ApiParameter("returnUrl", "URL to return to after login", Required = false, Location = "query")] string? returnUrl = null)
    {
        try
        {
            var state = Guid.NewGuid().ToString();
            var redirectUrl = $"/oauth/google/callback?state={state}";
            
            if (!string.IsNullOrEmpty(returnUrl))
            {
                redirectUrl += $"&returnUrl={Uri.EscapeDataString(returnUrl)}";
            }

            // Store state for validation
            _oauthStates[state] = new OAuthState(
                State: state,
                Provider: "google",
                CreatedAt: DateTime.UtcNow,
                ReturnUrl: returnUrl
            );

            return new OAuthLoginResponse("google", redirectUrl, state);
        }
        catch (Exception ex)
        {
            _logger.Error($"Google OAuth login initiation error: {ex.Message}", ex);
            return new { success = false, error = "Failed to initiate Google login" };
        }
    }

    [ApiRoute("GET", "/oauth/google/callback", "Google OAuth Callback", "Handle Google OAuth callback", "codex.oauth-discovery")]
    public async Task<object> GoogleCallbackAsync(
        [ApiParameter("code", "Authorization code from Google", Required = true, Location = "query")] string code,
        [ApiParameter("state", "State parameter for validation", Required = true, Location = "query")] string state,
        [ApiParameter("returnUrl", "URL to return to after login", Required = false, Location = "query")] string? returnUrl = null)
    {
        try
        {
            // Validate state
            if (!_oauthStates.TryGetValue(state, out var oauthState) || 
                oauthState.CreatedAt < DateTime.UtcNow.AddMinutes(-10))
            {
                return new OAuthCallbackResponse("google", false, Error: "Invalid or expired state");
            }

            // Exchange code for token
            var tokenResponse = await ExchangeCodeForTokenAsync(code, "google");
            if (tokenResponse == null)
            {
                return new OAuthCallbackResponse("google", false, Error: "Failed to exchange code for token");
            }

            // Get user info from Google
            var userInfo = await GetGoogleUserInfoAsync(tokenResponse.AccessToken);
            if (userInfo == null)
            {
                return new OAuthCallbackResponse("google", false, Error: "Failed to get user info from Google");
            }

            // Create or update user in our system
            var userId = await CreateOrUpdateOAuthUserAsync("google", userInfo);

            // Clean up state
            _oauthStates.Remove(state);

            return new OAuthCallbackResponse("google", true, userId);
        }
        catch (Exception ex)
        {
            _logger.Error($"Google OAuth callback error: {ex.Message}", ex);
            return new OAuthCallbackResponse("google", false, Error: "OAuth callback failed");
        }
    }

    [ApiRoute("GET", "/news/feed/{userId}", "Get User News Feed", "Get personalized news feed for a user", "codex.oauth-discovery")]
    public async Task<object> GetUserNewsFeedAsync([ApiParameter("userId", "User ID", Required = true, Location = "path")] string userId)
    {
        try
        {
            // Get user node
            var userNode = _registry.GetNode($"user.{userId}");
            if (userNode == null)
            {
                return new { success = false, error = "User not found" };
            }

            // Create a personalized news feed showcasing platform capabilities
            var newsFeed = new List<NewsItem>
            {
                new NewsItem
                {
                    Id = "welcome-1",
                    Title = "Welcome to Living Codex! 🌟",
                    Content = "You've just joined a revolutionary platform that connects consciousness, knowledge, and collective intelligence. Explore the infinite possibilities of fractal-based knowledge systems.",
                    Source = "Living Codex Platform",
                    Url = "https://livingcodex.com/welcome",
                    PublishedAt = DateTimeOffset.UtcNow,
                    Tags = new[] { "welcome", "platform", "consciousness" },
                    Metadata = new Dictionary<string, object> { ["type"] = "welcome", ["priority"] = 1 }
                },
                new NewsItem
                {
                    Id = "feature-1",
                    Title = "Discover the U-CORE Framework",
                    Content = "The Universal Consciousness Resonance Engine (U-CORE) powers our platform. Learn how fractal nodes and harmonic resonance create living knowledge systems.",
                    Source = "Living Codex Platform",
                    Url = "https://livingcodex.com/ucore",
                    PublishedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
                    Tags = new[] { "ucore", "framework", "knowledge" },
                    Metadata = new Dictionary<string, object> { ["type"] = "feature", ["priority"] = 2 }
                },
                new NewsItem
                {
                    Id = "concept-1",
                    Title = "Explore Concept Resonance",
                    Content = "Find concepts that resonate with your interests. Our AI-powered system matches you with knowledge that amplifies your consciousness and growth.",
                    Source = "Living Codex Platform",
                    Url = "https://livingcodex.com/concepts",
                    PublishedAt = DateTimeOffset.UtcNow.AddMinutes(-10),
                    Tags = new[] { "concepts", "resonance", "ai" },
                    Metadata = new Dictionary<string, object> { ["type"] = "concept", ["priority"] = 3 }
                },
                new NewsItem
                {
                    Id = "community-1",
                    Title = "Connect with Like-Minded Souls",
                    Content = "Discover users with similar interests, locations, and contributions. Build meaningful connections in our conscious community.",
                    Source = "Living Codex Platform",
                    Url = "https://livingcodex.com/community",
                    PublishedAt = DateTimeOffset.UtcNow.AddMinutes(-15),
                    Tags = new[] { "community", "connection", "discovery" },
                    Metadata = new Dictionary<string, object> { ["type"] = "community", ["priority"] = 4 }
                },
                new NewsItem
                {
                    Id = "news-1",
                    Title = "Latest Platform Updates",
                    Content = "We've just released new features including real-time collaboration, advanced concept mapping, and enhanced OAuth integration. Stay tuned for more!",
                    Source = "Living Codex Platform",
                    Url = "https://livingcodex.com/updates",
                    PublishedAt = DateTimeOffset.UtcNow.AddMinutes(-20),
                    Tags = new[] { "updates", "features", "collaboration" },
                    Metadata = new Dictionary<string, object> { ["type"] = "update", ["priority"] = 5 }
                }
            };

            return new
            {
                success = true,
                userId = userId,
                feed = newsFeed,
                totalItems = newsFeed.Count,
                lastUpdated = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting news feed for user {userId}: {ex.Message}", ex);
            return new { success = false, error = "Failed to get news feed" };
        }
    }

    [ApiRoute("POST", "/oauth/validate", "Validate OAuth User", "Validate OAuth user and return user info", "codex.oauth-discovery")]
    public async Task<object> ValidateOAuthUserAsync([ApiParameter("request", "OAuth validation request", Required = true, Location = "body")] OAuthValidationRequest request)
    {
        try
        {
            // Validate the OAuth user
            var user = await ValidateOAuthUserAsync(request.Provider, request.UserId, request.Email, request.Name);
            
            if (user != null)
            {
                return new
                {
                    success = true,
                    user = new
                    {
                        id = user.Id,
                        username = user.Username,
                        email = user.Email,
                        displayName = user.DisplayName,
                        avatarUrl = user.AvatarUrl,
                        createdAt = user.CreatedAt,
                        isActive = user.IsActive
                    }
                };
            }
            else
            {
                return new { success = false, error = "Invalid OAuth user" };
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"OAuth validation error: {ex.Message}", ex);
            return new { success = false, error = "OAuth validation failed" };
        }
    }

    // OAuth Helper Methods
    private async Task<TokenResponse?> ExchangeCodeForTokenAsync(string code, string provider)
    {
        try
        {
            var clientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
            var clientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");
            
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                _logger.Error("Google OAuth credentials not configured");
                return null;
            }

            var tokenUrl = "https://oauth2.googleapis.com/token";
            var parameters = new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["code"] = code,
                ["grant_type"] = "authorization_code",
                ["redirect_uri"] = $"{_baseUrl}/oauth/google/callback"
            };

            var content = new FormUrlEncodedContent(parameters);
            var response = await _httpClient.PostAsync(tokenUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var tokenData = JsonSerializer.Deserialize<TokenResponse>(responseContent);
                return tokenData;
            }
            else
            {
                _logger.Error($"Token exchange failed: {responseContent}");
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error exchanging code for token: {ex.Message}", ex);
            return null;
        }
    }

    private async Task<GoogleUserInfo?> GetGoogleUserInfoAsync(string accessToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v2/userinfo");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<GoogleUserInfo>(content);
            }
            else
            {
                _logger.Error($"Failed to get Google user info: {content}");
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting Google user info: {ex.Message}", ex);
            return null;
        }
    }

    private async Task<string> CreateOrUpdateOAuthUserAsync(string provider, GoogleUserInfo userInfo)
    {
        try
        {
            var userId = $"oauth-{provider}-{userInfo.Id}";
            var existingUser = _registry.GetNode($"user.{userId}");

            if (existingUser == null)
            {
                // Create new user node
                var userNode = new Node(
                    Id: $"user.{userId}",
                    TypeId: "codex.user",
                    State: ContentState.Ice,
                    Locale: "en",
                    Title: userInfo.Name ?? "OAuth User",
                    Description: $"User authenticated via {provider}",
                    Content: new ContentRef(
                        MediaType: "application/json",
                        InlineJson: JsonSerializer.Serialize(new
                        {
                            provider = provider,
                            providerId = userInfo.Id,
                            email = userInfo.Email,
                            name = userInfo.Name,
                            picture = userInfo.Picture,
                            createdAt = DateTime.UtcNow,
                            isActive = true
                        }),
                        InlineBytes: null,
                        ExternalUri: null
                    ),
                    Meta: new Dictionary<string, object>
                    {
                        ["username"] = userInfo.Email?.Split('@')[0] ?? userId,
                        ["email"] = userInfo.Email ?? "",
                        ["displayName"] = userInfo.Name ?? "OAuth User",
                        ["avatarUrl"] = userInfo.Picture ?? "",
                        ["provider"] = provider,
                        ["providerId"] = userInfo.Id,
                        ["createdAt"] = DateTime.UtcNow,
                        ["updatedAt"] = DateTime.UtcNow,
                        ["status"] = "active",
                        ["isActive"] = true
                    }
                );

                _registry.Upsert(userNode);
                _logger.Info($"Created new OAuth user: {userId}");
            }
            else
            {
                // Update existing user
                existingUser.Meta["updatedAt"] = DateTime.UtcNow;
                existingUser.Meta["avatarUrl"] = userInfo.Picture ?? existingUser.Meta.GetValueOrDefault("avatarUrl", "");
                _registry.Upsert(existingUser);
                _logger.Info($"Updated existing OAuth user: {userId}");
            }

            return userId;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error creating/updating OAuth user: {ex.Message}", ex);
            throw;
        }
    }

    private async Task<OAuthUser?> ValidateOAuthUserAsync(string provider, string userId, string email, string name)
    {
        try
        {
            var userNodeId = $"user.oauth-{provider}-{userId}";
            var userNode = _registry.GetNode(userNodeId);

            if (userNode != null)
            {
                return new OAuthUser(
                    Id: userNodeId,
                    Username: userNode.Meta.GetValueOrDefault("username", "").ToString() ?? "",
                    Email: userNode.Meta.GetValueOrDefault("email", "").ToString() ?? "",
                    DisplayName: userNode.Meta.GetValueOrDefault("displayName", "").ToString() ?? "",
                    AvatarUrl: userNode.Meta.GetValueOrDefault("avatarUrl", "").ToString(),
                    CreatedAt: (DateTime)userNode.Meta.GetValueOrDefault("createdAt", DateTime.UtcNow),
                    IsActive: (bool)userNode.Meta.GetValueOrDefault("isActive", true)
                );
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error validating OAuth user: {ex.Message}", ex);
            return null;
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
        // Get concepts related through edges
        var relatedConcepts = new List<string> { conceptId };
        
        try
        {
            // Find edges from this concept
            var outgoingEdges = _registry.GetEdges(fromId: conceptId);
            foreach (var edge in outgoingEdges)
            {
                if (!relatedConcepts.Contains(edge.ToId))
                {
                    relatedConcepts.Add(edge.ToId);
                }
            }
            
            // Find edges to this concept
            var incomingEdges = _registry.GetEdges(toId: conceptId);
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
                    if (_registry.TryGet(conceptId, out var node))
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
                var contributionEdges = _registry.GetEdges(toId: conceptId, edgeType: "contribution");
                foreach (var edge in contributionEdges)
                {
                    if (_registry.TryGet(edge.FromId, out var userNode) && 
                        userNode.TypeId == "user")
                    {
                        userIds.Add(edge.FromId);
                    }
                }
                
                // Find users who are subscribed to this concept
                var subscriptionEdges = _registry.GetEdges(toId: conceptId, edgeType: "subscription");
                foreach (var edge in subscriptionEdges)
                {
                    if (_registry.TryGet(edge.FromId, out var userNode) && 
                        userNode.TypeId == "user")
                    {
                        userIds.Add(edge.FromId);
                    }
                }
                
                // Find users who have invested in this concept
                var investmentEdges = _registry.GetEdges(toId: conceptId, edgeType: "investment");
                foreach (var edge in investmentEdges)
                {
                    if (_registry.TryGet(edge.FromId, out var userNode) && 
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
                var contributionEdges = _registry.GetEdges(fromId: userId, toId: relatedConceptId, edgeType: "contribution");
                score += contributionEdges.Count() * 1.0;
                
                // Check for subscriptions (lower weight)
                var subscriptionEdges = _registry.GetEdges(fromId: userId, toId: relatedConceptId, edgeType: "subscription");
                score += subscriptionEdges.Count() * 0.3;
                
                // Check for investments (higher weight)
                var investmentEdges = _registry.GetEdges(fromId: userId, toId: relatedConceptId, edgeType: "investment");
                score += investmentEdges.Count() * 2.0;
                
                // Check for comments/feedback (medium weight)
                var feedbackEdges = _registry.GetEdges(fromId: userId, toId: relatedConceptId, edgeType: "feedback");
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

        router.Register("codex.oauth-discovery", "discover-users", async args =>
        {
            try
            {
                if (!args.HasValue) return new ErrorResponse("Missing request body");
                var request = JsonSerializer.Deserialize<UserDiscoveryRequest>(args.Value.GetRawText());
                if (request == null) return new ErrorResponse("Invalid request");

                return await DiscoverUsers(request);
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Error discovering users: {ex.Message}");
            }
        });

        router.Register("codex.oauth-discovery", "concept-contributors", async args =>
        {
            try
            {
                if (!args.HasValue) return new ErrorResponse("Missing request parameters");

                var conceptId = args.Value.TryGetProperty("conceptId", out var idElement) ? idElement.GetString() : null;
                var ontologyLevel = args.Value.TryGetProperty("ontologyLevel", out var levelElement) ? levelElement.GetString() : null;
                var limit = args.Value.TryGetProperty("limit", out var limitElement) ? limitElement.GetInt32() : 100;

                if (string.IsNullOrEmpty(conceptId)) return new ErrorResponse("Concept ID is required");

                var result = await FindConceptContributors(conceptId, ontologyLevel, limit);
                return result;
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Error finding concept contributors: {ex.Message}");
            }
        });
    }

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // OAuth challenge endpoints
        app.MapGet("/oauth/challenge/{provider}", (string provider) =>
        {
            var challengeUrl = $"/oauth/{provider.ToLowerInvariant()}";
            return Results.Redirect(challengeUrl);
        });

        // OAuth test endpoints (for development)
        app.MapGet("/oauth/test", async (HttpContext context) =>
        {
            var providers = new List<OAuthProviderInfo>();
            
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID")))
            {
                providers.Add(new OAuthProviderInfo("google", "Google", Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID")!, true));
            }
            
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MICROSOFT_CLIENT_ID")))
            {
                providers.Add(new OAuthProviderInfo("microsoft", "Microsoft", Environment.GetEnvironmentVariable("MICROSOFT_CLIENT_ID")!, true));
            }
            
            return Results.Ok(new OAuthProvidersResponse(providers, providers.Count));
        });

        // Debug endpoint to check environment variables
        app.MapGet("/oauth/debug", async (HttpContext context) =>
        {
            var debugInfo = new
            {
                GoogleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID"),
                GoogleClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET"),
                MicrosoftClientId = Environment.GetEnvironmentVariable("MICROSOFT_CLIENT_ID"),
                MicrosoftClientSecret = Environment.GetEnvironmentVariable("MICROSOFT_CLIENT_SECRET")
            };
            
            return Results.Ok(debugInfo);
        });

        // User profile endpoint
        app.MapGet("/oauth/profile", async (HttpContext context) =>
        {
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                return Results.Unauthorized();
            }

            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
                        context.User.FindFirst("sub")?.Value;
            var email = context.User.FindFirst(ClaimTypes.Email)?.Value;
            var name = context.User.FindFirst(ClaimTypes.Name)?.Value;
            var picture = context.User.FindFirst("picture")?.Value;

            return Results.Ok(new
            {
                userId,
                email,
                name,
                picture,
                isAuthenticated = true
            });
        });

        // Simplified OAuth validation endpoint
        app.MapPost("/oauth/validate", async (HttpContext context, OAuthValidationRequest request) =>
        {
            try
            {
                // Validate the OAuth secret (in production, this would be more secure)
                var expectedSecret = Environment.GetEnvironmentVariable($"{request.Provider.ToUpper()}_CLIENT_SECRET");
                if (string.IsNullOrEmpty(expectedSecret) || request.Secret != expectedSecret)
                {
                    return Results.Unauthorized();
                }

                // Create session cookie with user info
                var sessionData = new
                {
                    UserId = request.UserId,
                    Provider = request.Provider,
                    Email = request.Email,
                    Name = request.Name,
                    ExpiresAt = DateTimeOffset.UtcNow.AddHours(24)
                };

                var sessionJson = JsonSerializer.Serialize(sessionData);
                var sessionBytes = System.Text.Encoding.UTF8.GetBytes(sessionJson);
                var sessionBase64 = Convert.ToBase64String(sessionBytes);

                // Set secure session cookie
                context.Response.Cookies.Append("session", sessionBase64, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = false, // Set to true in production with HTTPS
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddHours(24),
                    Path = "/"
                });

                // Store user in node registry
                await StoreOAuthUser(request.UserId, request.Provider, request.Email, request.Name);

                return Results.Ok(new OAuthCallbackResponse(request.Provider, true, request.UserId));
            }
            catch (Exception ex)
            {
                _logger.Error($"OAuth validation error: {ex.Message}", ex);
                return Results.BadRequest(new OAuthCallbackResponse(request.Provider, false, Error: $"OAuth validation failed: {ex.Message}"));
            }
        });

        // Session validation middleware for non-GET requests
        app.MapPost("/oauth/validate-session", async (HttpContext context) =>
        {
            try
            {
                if (!context.Request.Cookies.TryGetValue("session", out var sessionCookie))
                {
                    return Results.Unauthorized();
                }

                var sessionBytes = Convert.FromBase64String(sessionCookie);
                var sessionJson = System.Text.Encoding.UTF8.GetString(sessionBytes);
                var sessionData = JsonSerializer.Deserialize<SessionData>(sessionJson);

                if (sessionData == null || sessionData.ExpiresAt < DateTimeOffset.UtcNow)
                {
                    return Results.Unauthorized();
                }

                return Results.Ok(new { 
                    userId = sessionData.UserId, 
                    provider = sessionData.Provider,
                    email = sessionData.Email,
                    name = sessionData.Name,
                    isAuthenticated = true 
                });
            }
            catch (Exception ex)
            {
                _logger.Error($"Session validation error: {ex.Message}", ex);
                return Results.Unauthorized();
            }
        });
    }

    private async Task StoreOAuthUser(string userId, string provider, string email, string name)
    {
        try
        {
            var userNode = new Node(
                Id: $"user-{userId}",
                TypeId: "codex.user",
                State: ContentState.Ice,
                Locale: "en",
                Title: name ?? "Unknown User",
                Description: $"User authenticated via {provider}",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(new
                    {
                        provider = provider,
                        authenticatedAt = DateTime.UtcNow,
                        email = email,
                        name = name
                    }),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["displayName"] = name ?? "Unknown",
                    ["email"] = email ?? "",
                    ["avatarUrl"] = "",
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

// OAuth Data Structures
[MetaNode(Id = "codex.oauth.token-response", Name = "Token Response", Description = "OAuth token response")]
public record TokenResponse(string AccessToken, string TokenType, int ExpiresIn, string? RefreshToken = null, string? Scope = null);

[MetaNode(Id = "codex.oauth.google-user-info", Name = "Google User Info", Description = "User information from Google OAuth")]
public record GoogleUserInfo(string Id, string Email, string Name, string? Picture = null, bool? VerifiedEmail = null);

[MetaNode(Id = "codex.oauth.oauth-user", Name = "OAuth User", Description = "OAuth user information")]
public record OAuthUser(string Id, string Username, string Email, string DisplayName, string? AvatarUrl, DateTime CreatedAt, bool IsActive);

[MetaNode(Id = "codex.oauth.oauth-state", Name = "OAuth State", Description = "OAuth state for validation")]
public record OAuthState(string State, string Provider, DateTime CreatedAt, string? ReturnUrl = null);

// Note: OAuthValidationRequest and NewsItem are already defined in other modules
