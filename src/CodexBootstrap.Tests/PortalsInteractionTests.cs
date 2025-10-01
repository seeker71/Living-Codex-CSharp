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

public class PortalsInteractionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public PortalsInteractionTests(WebApplicationFactory<Program> factory)
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
    public async Task ConnectPortal_WithDifferentConfigurations_ReturnsSuccess()
    {
        // Test with different configuration structures
        var configurations = new object[]
        {
            new { },
            new { apiKey = "test-key", timeout = 30 },
            new { 
                authentication = new { type = "bearer", token = "test-token" },
                rateLimit = new { requests = 100, window = 60 },
                retries = 3
            },
            new { 
                headers = new Dictionary<string, string> { ["User-Agent"] = "Living-Codex/1.0" },
                cookies = new Dictionary<string, string> { ["session"] = "test-session" },
                proxy = new { host = "proxy.example.com", port = 8080 }
            }
        };
        
        foreach (var configuration in configurations)
        {
            var request = new
            {
                name = "Configuration Test Portal",
                description = "A portal for testing different configurations",
                url = "https://example.com",
                portalType = "website",
                configuration = configuration
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
    public async Task ConnectTemporalPortal_WithDifferentTargetMoments_ReturnsSuccess()
    {
        // Test with different target moments
        var targetMoments = new string[]
        {
            "2020-01-01T00:00:00Z",
            "2021-06-15T12:30:45Z",
            "2022-12-31T23:59:59Z",
            "2023-03-15T08:15:30Z",
            "2024-07-04T14:22:10Z"
        };
        
        foreach (var targetMoment in targetMoments)
        {
            var request = new
            {
                temporalType = "past",
                targetMoment = targetMoment,
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
    public async Task ExplorePortal_WithDifferentStartingPoints_ReturnsSuccess()
    {
        // First create a portal
        var portalRequest = new
        {
            name = "Test Portal for Starting Points",
            description = "A portal for testing different starting points",
            url = "https://example.com",
            portalType = "website"
        };

        var portalResponse = await _client.PostAsync("/portal/connect", 
            new StringContent(JsonSerializer.Serialize(portalRequest), Encoding.UTF8, "application/json"));
        
        Assert.Equal(HttpStatusCode.OK, portalResponse.StatusCode);
        var portalContent = await portalResponse.Content.ReadAsStringAsync();
        var portalData = JsonSerializer.Deserialize<JsonElement>(portalContent);
        var portalId = portalData.GetProperty("portalId").GetString();

        // Test with different starting points
        var startingPoints = new string[]
        {
            "https://example.com",
            "https://example.com/page1",
            "https://example.com/page2",
            "https://example.com/api",
            "https://example.com/docs"
        };
        
        foreach (var startingPoint in startingPoints)
        {
            var explorationRequest = new
            {
                portalId = portalId,
                userId = "test-user-123",
                explorationType = "fractal",
                depth = 3,
                maxBranches = 10,
                startingPoint = startingPoint
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
    public async Task ExploreTemporalPortal_WithDifferentTemporalFilters_ReturnsSuccess()
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

        // Test with different temporal filters
        var temporalFiltersList = new object[]
        {
            new { },
            new { timeRange = "2020-2021", events = new string[] { "pandemic", "election" } },
            new { consciousnessLevel = 1.5, resonance = 0.8 },
            new { 
                dimensions = new string[] { "past", "present", "future" },
                depth = 5,
                includeParallels = true
            }
        };
        
        foreach (var temporalFilters in temporalFiltersList)
        {
            var explorationRequest = new
            {
                portalId = portalId,
                userId = "test-user-123",
                explorationType = "consciousness_mapping",
                depth = 5,
                maxBranches = 3,
                temporalFilters = temporalFilters
            };

            // Act
            var response = await _client.PostAsync("/temporal/explore", 
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
    public async Task ConnectPortal_WithRapidSuccession_HandlesGracefully()
    {
        // Test rapid successive portal creation
        var tasks = new List<Task<HttpResponseMessage>>();
        
        for (int i = 0; i < 20; i++)
        {
            var request = new
            {
                name = $"Rapid Portal {i}",
                description = $"A rapidly created portal {i}",
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
    public async Task ConnectTemporalPortal_WithRapidSuccession_HandlesGracefully()
    {
        // Test rapid successive temporal portal creation
        var tasks = new List<Task<HttpResponseMessage>>();
        
        for (int i = 0; i < 20; i++)
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
    public async Task ConnectPortal_WithMixedConcurrentRequests_HandlesGracefully()
    {
        // Test mixed concurrent requests
        var tasks = new List<Task<HttpResponseMessage>>();
        
        // Mix of different request types
        for (int i = 0; i < 5; i++)
        {
            // Portal creation
            var portalRequest = new
            {
                name = $"Mixed Portal {i}",
                description = $"A mixed test portal {i}",
                url = $"https://example{i}.com",
                portalType = "website"
            };
            tasks.Add(_client.PostAsync("/portal/connect", 
                new StringContent(JsonSerializer.Serialize(portalRequest), Encoding.UTF8, "application/json")));
            
            // Temporal portal creation
            var temporalPortalRequest = new
            {
                temporalType = "past",
                targetMoment = $"202{i}-01-01T00:00:00Z",
                consciousnessLevel = 1.0
            };
            tasks.Add(_client.PostAsync("/temporal/portal/connect", 
                new StringContent(JsonSerializer.Serialize(temporalPortalRequest), Encoding.UTF8, "application/json")));
            
            // Portal listing
            tasks.Add(_client.GetAsync("/portal/list"));
            
            // Temporal portal listing
            tasks.Add(_client.GetAsync("/temporal/portal/list"));
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
    public async Task ConnectPortal_WithLongTermConsistency_ReturnsConsistentResults()
    {
        // Test consistency over multiple calls
        var request = new
        {
            name = "Consistency Test Portal",
            description = "A portal for testing consistency across multiple calls",
            url = "https://example.com",
            portalType = "website"
        };

        var responses = new List<JsonElement>();
        
        for (int i = 0; i < 5; i++)
        {
            // Act
            var response = await _client.PostAsync("/portal/connect", 
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));
            
            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
            responses.Add(data);
        }

        // All responses should have the same structure
        foreach (var data in responses)
        {
            Assert.True(data.TryGetProperty("success", out var success));
            Assert.True(data.TryGetProperty("portalId", out var portalId));
            Assert.True(data.TryGetProperty("portal", out var portal));
            Assert.True(data.TryGetProperty("message", out var message));
            Assert.True(success.GetBoolean());
            Assert.False(string.IsNullOrEmpty(portalId.GetString()));
            Assert.Equal("Portal connection established", message.GetString());
        }
    }

    [Fact]
    public async Task ConnectPortal_WithSpecialCharacters_HandlesGracefully()
    {
        // Test with special characters in various fields
        var specialCharTests = new object[]
        {
            new { name = "Portal@#$%^&*()", description = "Description!@#$%^&*()_+", url = "https://example.com", portalType = "website" },
            new { name = "Portal[]{}|\\:;\"'<>?,./", description = "Description`~", url = "https://example.com", portalType = "website" },
            new { name = "Portal\t\n\r", description = "Description\b\f\v", url = "https://example.com", portalType = "website" }
        };
        
        foreach (var test in specialCharTests)
        {
            var testObj = (dynamic)test;
            var request = new
            {
                name = testObj.name,
                description = testObj.description,
                url = testObj.url,
                portalType = testObj.portalType
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
    public async Task ConnectTemporalPortal_WithSpecialCharacters_HandlesGracefully()
    {
        // Test with special characters in temporal portal fields
        var specialCharTests = new object[]
        {
            new { temporalType = "past@#$%^&*()", targetMoment = "2020-01-01T00:00:00Z", consciousnessLevel = 1.0 },
            new { temporalType = "past[]{}|\\:;\"'<>?,./", targetMoment = "2020-01-01T00:00:00Z", consciousnessLevel = 1.0 },
            new { temporalType = "past\t\n\r", targetMoment = "2020-01-01T00:00:00Z", consciousnessLevel = 1.0 }
        };
        
        foreach (var test in specialCharTests)
        {
            var testObj = (dynamic)test;
            var request = new
            {
                temporalType = testObj.temporalType,
                targetMoment = testObj.targetMoment,
                consciousnessLevel = testObj.consciousnessLevel
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
    public async Task ConnectPortal_WithUnicodeCharacters_HandlesGracefully()
    {
        // Test with Unicode characters in various fields
        var unicodeTests = new object[]
        {
            new { name = "门户测试", description = "这是一个测试门户连接", url = "https://example.com", portalType = "website" },
            new { name = "ポータルテスト", description = "これはテストポータル接続です", url = "https://example.com", portalType = "website" },
            new { name = "اختبار البوابة", description = "هذا اختبار اتصال البوابة", url = "https://example.com", portalType = "website" }
        };
        
        foreach (var test in unicodeTests)
        {
            var testObj = (dynamic)test;
            var request = new
            {
                name = testObj.name,
                description = testObj.description,
                url = testObj.url,
                portalType = testObj.portalType
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
    public async Task ConnectTemporalPortal_WithUnicodeCharacters_HandlesGracefully()
    {
        // Test with Unicode characters in temporal portal fields
        var unicodeTests = new object[]
        {
            new { temporalType = "过去", targetMoment = "2020-01-01T00:00:00Z", consciousnessLevel = 1.0 },
            new { temporalType = "過去", targetMoment = "2020-01-01T00:00:00Z", consciousnessLevel = 1.0 },
            new { temporalType = "الماضي", targetMoment = "2020-01-01T00:00:00Z", consciousnessLevel = 1.0 }
        };
        
        foreach (var test in unicodeTests)
        {
            var testObj = (dynamic)test;
            var request = new
            {
                temporalType = testObj.temporalType,
                targetMoment = testObj.targetMoment,
                consciousnessLevel = testObj.consciousnessLevel
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
    public async Task ConnectPortal_WithVeryLongArrays_HandlesGracefully()
    {
        // Test with very long arrays in configuration
        var longHeaders = new Dictionary<string, string>();
        var longCookies = new Dictionary<string, string>();
        
        for (int i = 0; i < 100; i++)
        {
            longHeaders[$"header-{i}"] = $"value-{i}";
            longCookies[$"cookie-{i}"] = $"value-{i}";
        }
        
        var request = new
        {
            name = "Long Arrays Portal",
            description = "A portal with very long arrays",
            url = "https://example.com",
            portalType = "website",
            configuration = new
            {
                headers = longHeaders,
                cookies = longCookies
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
    public async Task ConnectPortal_WithEmptyArrays_HandlesGracefully()
    {
        // Test with empty arrays
        var request = new
        {
            name = "Empty Arrays Portal",
            description = "A portal with empty arrays",
            url = "https://example.com",
            portalType = "website",
            configuration = new
            {
                headers = new Dictionary<string, string>(),
                cookies = new Dictionary<string, string>(),
                filters = new string[0]
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
    public async Task ConnectPortal_WithNullValues_HandlesGracefully()
    {
        // Test with null values
        var request = new
        {
            name = "Null Values Portal",
            description = "A portal with null values",
            url = "https://example.com",
            portalType = "website",
            configuration = (object)null
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
    public async Task ConnectPortal_WithComplexNestedConfiguration_HandlesGracefully()
    {
        // Test with complex nested configuration
        var complexConfiguration = new
        {
            authentication = new
            {
                type = "oauth2",
                clientId = "test-client-id",
                clientSecret = "test-client-secret",
                scopes = new[] { "read", "write", "admin" },
                endpoints = new
                {
                    authorization = "https://auth.example.com/oauth/authorize",
                    token = "https://auth.example.com/oauth/token",
                    userInfo = "https://auth.example.com/oauth/userinfo"
                }
            },
            rateLimit = new
            {
                enabled = true,
                requests = 1000,
                window = 3600,
                burst = 100
            },
            retry = new
            {
                enabled = true,
                maxAttempts = 3,
                backoff = new
                {
                    type = "exponential",
                    baseDelay = 1000,
                    maxDelay = 10000
                }
            },
            monitoring = new
            {
                enabled = true,
                metrics = new[] { "response_time", "error_rate", "throughput" },
                alerts = new
                {
                    email = "admin@example.com",
                    webhook = "https://alerts.example.com/webhook"
                }
            }
        };
        
        var request = new
        {
            name = "Complex Configuration Portal",
            description = "A portal with complex nested configuration",
            url = "https://example.com",
            portalType = "api",
            configuration = complexConfiguration
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
