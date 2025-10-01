using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using CodexBootstrap.Core;
using Xunit;
using System.Net.Http;
using System.Text;

namespace CodexBootstrap.Tests;

public class PortalsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public PortalsApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Ensure we're using in-memory storage for tests
                services.Configure<Microsoft.Extensions.Hosting.HostOptions>(options =>
                {
                    options.BackgroundServiceExceptionBehavior = Microsoft.Extensions.Hosting.BackgroundServiceExceptionBehavior.Ignore;
                });
            });
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task ListPortals_ReturnsPortalsList()
    {
        // Act
        var response = await _client.GetAsync("/portal/list");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(data.TryGetProperty("portals", out var portals));
        Assert.True(data.TryGetProperty("count", out var count));
        
        Assert.True(success.GetBoolean());
        Assert.True(count.GetInt32() >= 0);
        Assert.True(portals.GetArrayLength() >= 0);
    }

    [Fact]
    public async Task ListTemporalPortals_ReturnsTemporalPortalsList()
    {
        // Act
        var response = await _client.GetAsync("/temporal/portal/list");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(data.TryGetProperty("portals", out var portals));
        Assert.True(data.TryGetProperty("count", out var count));
        
        Assert.True(success.GetBoolean());
        Assert.True(count.GetInt32() >= 0);
        Assert.True(portals.GetArrayLength() >= 0);
    }

    [Fact]
    public async Task ConnectPortal_WithValidUrl_ReturnsSuccess()
    {
        // Arrange
        var request = new
        {
            name = "Test Portal",
            description = "A test portal connection",
            url = "https://example.com",
            portalType = "website"
        };

        // Act
        var response = await _client.PostAsync("/portal/connect", 
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(data.TryGetProperty("portalId", out var portalId));
        Assert.True(data.TryGetProperty("portal", out var portal));
        Assert.True(data.TryGetProperty("message", out var message));
        
        Assert.True(success.GetBoolean());
        Assert.False(string.IsNullOrEmpty(portalId.GetString()));
        Assert.Equal("Portal connection established", message.GetString());
        
        // Verify portal structure
        Assert.True(portal.TryGetProperty("portalId", out var portalIdInPortal));
        Assert.True(portal.TryGetProperty("name", out var name));
        Assert.True(portal.TryGetProperty("description", out var description));
        Assert.True(portal.TryGetProperty("portalType", out var portalType));
        Assert.True(portal.TryGetProperty("url", out var url));
        Assert.True(portal.TryGetProperty("status", out var status));
        Assert.True(portal.TryGetProperty("capabilities", out var capabilities));
        
        Assert.Equal("Test Portal", name.GetString());
        Assert.Equal("A test portal connection", description.GetString());
        Assert.Equal("website", portalType.GetString());
        Assert.Equal("https://example.com", url.GetString());
    }

    [Fact]
    public async Task ConnectPortal_WithValidEntityId_ReturnsSuccess()
    {
        // Arrange
        var request = new
        {
            name = "Entity Portal",
            description = "A portal to an entity",
            entityId = "entity-123",
            portalType = "entity"
        };

        // Act
        var response = await _client.PostAsync("/portal/connect", 
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(data.TryGetProperty("portalId", out var portalId));
        Assert.True(data.TryGetProperty("portal", out var portal));
        
        Assert.True(success.GetBoolean());
        Assert.False(string.IsNullOrEmpty(portalId.GetString()));
        
        // Verify portal structure
        Assert.True(portal.TryGetProperty("entityId", out var entityId));
        Assert.Equal("entity-123", entityId.GetString());
    }

    [Fact]
    public async Task ConnectPortal_WithConfiguration_ReturnsSuccess()
    {
        // Arrange
        var request = new
        {
            name = "Configured Portal",
            description = "A portal with configuration",
            url = "https://api.example.com",
            portalType = "api",
            configuration = new
            {
                apiKey = "test-key",
                timeout = 30,
                retries = 3,
                headers = new Dictionary<string, string>
                {
                    ["User-Agent"] = "Living-Codex/1.0",
                    ["Accept"] = "application/json"
                }
            }
        };

        // Act
        var response = await _client.PostAsync("/portal/connect", 
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
    }

    [Fact]
    public async Task ConnectPortal_WithDifferentPortalTypes_ReturnsSuccess()
    {
        // Test with different portal types
        var portalTypes = new string[] { "website", "api", "database", "file", "entity", "service" };
        
        foreach (var portalType in portalTypes)
        {
            var request = new
            {
                name = $"Portal Type: {portalType}",
                description = $"A portal of type {portalType}",
                url = $"https://{portalType}.example.com",
                portalType = portalType
            };

            // Act
            var response = await _client.PostAsync("/portal/connect", 
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            Assert.True(data.TryGetProperty("success", out var success));
            Assert.True(success.GetBoolean());
        }
    }

    [Fact]
    public async Task ConnectTemporalPortal_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var request = new
        {
            temporalType = "past",
            targetMoment = "2020-01-01T00:00:00Z",
            consciousnessLevel = 1.0
        };

        // Act
        var response = await _client.PostAsync("/temporal/portal/connect", 
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(data.TryGetProperty("portalId", out var portalId));
        Assert.True(data.TryGetProperty("portal", out var portal));
        Assert.True(data.TryGetProperty("message", out var message));
        
        Assert.True(success.GetBoolean());
        Assert.False(string.IsNullOrEmpty(portalId.GetString()));
        Assert.Equal("Temporal portal connection established", message.GetString());
        
        // Verify portal structure
        Assert.True(portal.TryGetProperty("temporalType", out var temporalType));
        Assert.True(portal.TryGetProperty("targetMoment", out var targetMoment));
        Assert.True(portal.TryGetProperty("consciousnessLevel", out var consciousnessLevel));
        Assert.True(portal.TryGetProperty("status", out var status));
        Assert.True(portal.TryGetProperty("resonance", out var resonance));
        
        Assert.Equal("past", temporalType.GetString());
        Assert.Equal(1.0, consciousnessLevel.GetDouble());
        Assert.Equal("connected", status.GetString());
        Assert.True(resonance.GetDouble() >= 0);
    }

    [Fact]
    public async Task ConnectTemporalPortal_WithDifferentTemporalTypes_ReturnsSuccess()
    {
        // Test with different temporal types
        var temporalTypes = new string[] { "past", "present", "future", "timeline", "dimension" };
        
        foreach (var temporalType in temporalTypes)
        {
            var request = new
            {
                temporalType = temporalType,
                targetMoment = "2020-01-01T00:00:00Z",
                consciousnessLevel = 1.0
            };

            // Act
            var response = await _client.PostAsync("/temporal/portal/connect", 
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            Assert.True(data.TryGetProperty("success", out var success));
            Assert.True(success.GetBoolean());
        }
    }

    [Fact]
    public async Task ConnectTemporalPortal_WithDifferentConsciousnessLevels_ReturnsSuccess()
    {
        // Test with different consciousness levels
        var consciousnessLevels = new double[] { 0.1, 0.5, 1.0, 1.5, 2.0, 5.0 };
        
        foreach (var consciousnessLevel in consciousnessLevels)
        {
            var request = new
            {
                temporalType = "past",
                targetMoment = "2020-01-01T00:00:00Z",
                consciousnessLevel = consciousnessLevel
            };

            // Act
            var response = await _client.PostAsync("/temporal/portal/connect", 
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            Assert.True(data.TryGetProperty("success", out var success));
            Assert.True(success.GetBoolean());
        }
    }

    [Fact]
    public async Task ExplorePortal_WithValidPortalId_ReturnsSuccess()
    {
        // First create a portal
        var portalRequest = new
        {
            name = "Test Portal for Exploration",
            description = "A portal for testing exploration",
            url = "https://example.com",
            portalType = "website"
        };

        var portalResponse = await _client.PostAsync("/portal/connect", 
            new StringContent(JsonSerializer.Serialize(portalRequest), Encoding.UTF8, "application/json"));
        
        Assert.Equal(HttpStatusCode.OK, portalResponse.StatusCode);
        var portalContent = await portalResponse.Content.ReadAsStringAsync();
        var portalData = JsonSerializer.Deserialize<JsonElement>(portalContent);
        var portalId = portalData.GetProperty("portalId").GetString();

        // Now explore the portal
        var explorationRequest = new
        {
            portalId = portalId,
            userId = "test-user-123",
            explorationType = "fractal",
            depth = 3,
            maxBranches = 10
        };

        // Act
        var response = await _client.PostAsync("/portal/explore", 
            new StringContent(JsonSerializer.Serialize(explorationRequest), Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(data.TryGetProperty("explorationId", out var explorationId));
        Assert.True(data.TryGetProperty("exploration", out var exploration));
        Assert.True(data.TryGetProperty("message", out var message));
        
        Assert.True(success.GetBoolean());
        Assert.False(string.IsNullOrEmpty(explorationId.GetString()));
        Assert.Equal("Portal exploration initiated", message.GetString());
    }

    [Fact]
    public async Task ExploreTemporalPortal_WithValidPortalId_ReturnsSuccess()
    {
        // First create a temporal portal
        var temporalPortalRequest = new
        {
            temporalType = "past",
            targetMoment = "2020-01-01T00:00:00Z",
            consciousnessLevel = 1.0
        };

        var temporalPortalResponse = await _client.PostAsync("/temporal/portal/connect", 
            new StringContent(JsonSerializer.Serialize(temporalPortalRequest), Encoding.UTF8, "application/json"));
        
        Assert.Equal(HttpStatusCode.OK, temporalPortalResponse.StatusCode);
        var temporalPortalContent = await temporalPortalResponse.Content.ReadAsStringAsync();
        var temporalPortalData = JsonSerializer.Deserialize<JsonElement>(temporalPortalContent);
        var portalId = temporalPortalData.GetProperty("portalId").GetString();

        // Now explore the temporal portal
        var explorationRequest = new
        {
            portalId = portalId,
            userId = "test-user-123",
            explorationType = "consciousness_mapping",
            depth = 5,
            maxBranches = 3
        };

        // Act
        var response = await _client.PostAsync("/temporal/explore", 
            new StringContent(JsonSerializer.Serialize(explorationRequest), Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(data.TryGetProperty("explorationId", out var explorationId));
        Assert.True(data.TryGetProperty("exploration", out var exploration));
        Assert.True(data.TryGetProperty("message", out var message));
        
        Assert.True(success.GetBoolean());
        Assert.False(string.IsNullOrEmpty(explorationId.GetString()));
        Assert.Equal("Temporal exploration initiated", message.GetString());
    }

    [Fact]
    public async Task ExplorePortal_WithDifferentExplorationTypes_ReturnsSuccess()
    {
        // First create a portal
        var portalRequest = new
        {
            name = "Test Portal for Exploration Types",
            description = "A portal for testing different exploration types",
            url = "https://example.com",
            portalType = "website"
        };

        var portalResponse = await _client.PostAsync("/portal/connect", 
            new StringContent(JsonSerializer.Serialize(portalRequest), Encoding.UTF8, "application/json"));
        
        Assert.Equal(HttpStatusCode.OK, portalResponse.StatusCode);
        var portalContent = await portalResponse.Content.ReadAsStringAsync();
        var portalData = JsonSerializer.Deserialize<JsonElement>(portalContent);
        var portalId = portalData.GetProperty("portalId").GetString();

        // Test with different exploration types
        var explorationTypes = new string[] { "fractal", "linear", "spiral", "network", "tree" };
        
        foreach (var explorationType in explorationTypes)
        {
            var explorationRequest = new
            {
                portalId = portalId,
                userId = "test-user-123",
                explorationType = explorationType,
                depth = 3,
                maxBranches = 10
            };

            // Act
            var response = await _client.PostAsync("/portal/explore", 
                new StringContent(JsonSerializer.Serialize(explorationRequest), Encoding.UTF8, "application/json"));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            Assert.True(data.TryGetProperty("success", out var success));
            Assert.True(success.GetBoolean());
        }
    }

    [Fact]
    public async Task ExplorePortal_WithDifferentDepths_ReturnsSuccess()
    {
        // First create a portal
        var portalRequest = new
        {
            name = "Test Portal for Different Depths",
            description = "A portal for testing different exploration depths",
            url = "https://example.com",
            portalType = "website"
        };

        var portalResponse = await _client.PostAsync("/portal/connect", 
            new StringContent(JsonSerializer.Serialize(portalRequest), Encoding.UTF8, "application/json"));
        
        Assert.Equal(HttpStatusCode.OK, portalResponse.StatusCode);
        var portalContent = await portalResponse.Content.ReadAsStringAsync();
        var portalData = JsonSerializer.Deserialize<JsonElement>(portalContent);
        var portalId = portalData.GetProperty("portalId").GetString();

        // Test with different depths
        var depths = new int[] { 1, 2, 3, 5, 10, 20 };
        
        foreach (var depth in depths)
        {
            var explorationRequest = new
            {
                portalId = portalId,
                userId = "test-user-123",
                explorationType = "fractal",
                depth = depth,
                maxBranches = 10
            };

            // Act
            var response = await _client.PostAsync("/portal/explore", 
                new StringContent(JsonSerializer.Serialize(explorationRequest), Encoding.UTF8, "application/json"));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            Assert.True(data.TryGetProperty("success", out var success));
            Assert.True(success.GetBoolean());
        }
    }

    [Fact]
    public async Task ExplorePortal_WithDifferentMaxBranches_ReturnsSuccess()
    {
        // First create a portal
        var portalRequest = new
        {
            name = "Test Portal for Different Max Branches",
            description = "A portal for testing different max branches",
            url = "https://example.com",
            portalType = "website"
        };

        var portalResponse = await _client.PostAsync("/portal/connect", 
            new StringContent(JsonSerializer.Serialize(portalRequest), Encoding.UTF8, "application/json"));
        
        Assert.Equal(HttpStatusCode.OK, portalResponse.StatusCode);
        var portalContent = await portalResponse.Content.ReadAsStringAsync();
        var portalData = JsonSerializer.Deserialize<JsonElement>(portalContent);
        var portalId = portalData.GetProperty("portalId").GetString();

        // Test with different max branches
        var maxBranches = new int[] { 1, 3, 5, 10, 20, 50 };
        
        foreach (var maxBranch in maxBranches)
        {
            var explorationRequest = new
            {
                portalId = portalId,
                userId = "test-user-123",
                explorationType = "fractal",
                depth = 3,
                maxBranches = maxBranch
            };

            // Act
            var response = await _client.PostAsync("/portal/explore", 
                new StringContent(JsonSerializer.Serialize(explorationRequest), Encoding.UTF8, "application/json"));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            Assert.True(data.TryGetProperty("success", out var success));
            Assert.True(success.GetBoolean());
        }
    }

    [Fact]
    public async Task ExplorePortal_WithFilters_ReturnsSuccess()
    {
        // First create a portal
        var portalRequest = new
        {
            name = "Test Portal for Filters",
            description = "A portal for testing exploration filters",
            url = "https://example.com",
            portalType = "website"
        };

        var portalResponse = await _client.PostAsync("/portal/connect", 
            new StringContent(JsonSerializer.Serialize(portalRequest), Encoding.UTF8, "application/json"));
        
        Assert.Equal(HttpStatusCode.OK, portalResponse.StatusCode);
        var portalContent = await portalResponse.Content.ReadAsStringAsync();
        var portalData = JsonSerializer.Deserialize<JsonElement>(portalContent);
        var portalId = portalData.GetProperty("portalId").GetString();

        // Test with filters
        var explorationRequest = new
        {
            portalId = portalId,
            userId = "test-user-123",
            explorationType = "fractal",
            depth = 3,
            maxBranches = 10,
            filters = new
            {
                contentType = "text/html",
                maxDepth = 5,
                includeImages = true,
                excludeScripts = true
            }
        };

        // Act
        var response = await _client.PostAsync("/portal/explore", 
            new StringContent(JsonSerializer.Serialize(explorationRequest), Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
    }

    [Fact]
    public async Task ConnectPortal_WithConcurrentRequests_HandlesGracefully()
    {
        // Test concurrent portal creation
        var tasks = new List<Task<HttpResponseMessage>>();
        
        for (int i = 0; i < 10; i++)
        {
            var request = new
            {
                name = $"Concurrent Portal {i}",
                description = $"A concurrent test portal {i}",
                url = $"https://example{i}.com",
                portalType = "website"
            };
            
            tasks.Add(_client.PostAsync("/portal/connect", 
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")));
        }

        // Wait for all requests to complete
        var responses = await Task.WhenAll(tasks);

        // Assert all requests succeeded
        foreach (var response in responses)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task ConnectTemporalPortal_WithConcurrentRequests_HandlesGracefully()
    {
        // Test concurrent temporal portal creation
        var tasks = new List<Task<HttpResponseMessage>>();
        
        for (int i = 0; i < 10; i++)
        {
            var request = new
            {
                temporalType = "past",
                targetMoment = $"202{i}-01-01T00:00:00Z",
                consciousnessLevel = 1.0 + (i * 0.1)
            };
            
            tasks.Add(_client.PostAsync("/temporal/portal/connect", 
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")));
        }

        // Wait for all requests to complete
        var responses = await Task.WhenAll(tasks);

        // Assert all requests succeeded
        foreach (var response in responses)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task ConnectPortal_WithUnicodeCharacters_HandlesGracefully()
    {
        // Test with Unicode characters
        var request = new
        {
            name = "门户测试 - 中文标题",
            description = "这是一个测试门户连接。它探索心灵和机器智能的本质。",
            url = "https://example.com",
            portalType = "website"
        };

        // Act
        var response = await _client.PostAsync("/portal/connect", 
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
    }

    [Fact]
    public async Task ConnectPortal_WithVeryLongStrings_HandlesGracefully()
    {
        // Test with very long strings
        var longName = new string('a', 1000);
        var longDescription = new string('b', 5000);
        
        var request = new
        {
            name = longName,
            description = longDescription,
            url = "https://example.com",
            portalType = "website"
        };

        // Act
        var response = await _client.PostAsync("/portal/connect", 
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
    }
}
