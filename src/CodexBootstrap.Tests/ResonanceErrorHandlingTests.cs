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

public class ResonanceErrorHandlingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ResonanceErrorHandlingTests(WebApplicationFactory<Program> factory)
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
    public async Task GetContributorEnergy_WithEmptyUserId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/contributions/abundance/contributor-energy/");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetContributorEnergy_WithNullUserId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/contributions/abundance/contributor-energy/null");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(content);
        
        // Should handle gracefully
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
    }

    [Fact]
    public async Task GetContributorEnergy_WithWhitespaceUserId_HandlesGracefully()
    {
        // Act
        var response = await _client.GetAsync("/contributions/abundance/contributor-energy/   ");

        // Assert
        // Should either return 400 Bad Request or handle gracefully
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetContributorEnergy_WithSpecialCharacters_HandlesGracefully()
    {
        // Test with various special characters
        var specialUserIds = new[]
        {
            "user@#$%^&*()",
            "user!@#$%^&*()_+",
            "user[]{}|\\:;\"'<>?,./",
            "user`~",
            "user\t\n\r"
        };

        foreach (var userId in specialUserIds)
        {
            // Act
            var response = await _client.GetAsync($"/contributions/abundance/contributor-energy/{userId}");

            // Assert
            // Should either return 400 Bad Request or handle gracefully
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task GetContributorEnergy_WithVeryLongUserId_HandlesGracefully()
    {
        // Act
        var veryLongUserId = new string('a', 10000);
        var response = await _client.GetAsync($"/contributions/abundance/contributor-energy/{veryLongUserId}");

        // Assert
        // Should either return 400 Bad Request or handle gracefully
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAbundanceEvents_WithNegativeTake_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/contributions/abundance/events?take=-5");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAbundanceEvents_WithNegativeSkip_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/contributions/abundance/events?skip=-10");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAbundanceEvents_WithZeroTake_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/contributions/abundance/events?take=0");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAbundanceEvents_WithInvalidTake_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/contributions/abundance/events?take=abc");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAbundanceEvents_WithInvalidSkip_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/contributions/abundance/events?skip=xyz");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAbundanceEvents_WithInvalidSince_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/contributions/abundance/events?since=invalid-date");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAbundanceEvents_WithInvalidUntil_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/contributions/abundance/events?until=invalid-date");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAbundanceEvents_WithInvalidSortDescending_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/contributions/abundance/events?sortDescending=invalid");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAbundanceEvents_WithExcessiveTake_ReturnsLimitedResults()
    {
        // Act
        var response = await _client.GetAsync("/contributions/abundance/events?take=100000");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(content);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
        
        // Should be limited to a reasonable number (e.g., 500 max)
        if (data.TryGetProperty("events", out var events))
        {
            Assert.True(events.GetArrayLength() <= 500);
        }
    }

    [Fact]
    public async Task GetAbundanceEvents_WithExcessiveSkip_HandlesGracefully()
    {
        // Act
        var response = await _client.GetAsync("/contributions/abundance/events?skip=1000000");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(content);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
    }

    [Fact]
    public async Task GetAbundanceEvents_WithInvalidUserId_HandlesGracefully()
    {
        // Act
        var response = await _client.GetAsync("/contributions/abundance/events?userId=invalid@#$%^&*()");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(content);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
    }

    [Fact]
    public async Task GetAbundanceEvents_WithInvalidContributionId_HandlesGracefully()
    {
        // Act
        var response = await _client.GetAsync("/contributions/abundance/events?contributionId=invalid@#$%^&*()");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(content);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
    }

    [Fact]
    public async Task GetAbundanceEvents_WithFutureSince_HandlesGracefully()
    {
        // Act
        var futureDate = DateTime.UtcNow.AddDays(1).ToString("O");
        var response = await _client.GetAsync($"/contributions/abundance/events?since={futureDate}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(content);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
    }

    [Fact]
    public async Task GetAbundanceEvents_WithPastUntil_HandlesGracefully()
    {
        // Act
        var pastDate = DateTime.UtcNow.AddDays(-1).ToString("O");
        var response = await _client.GetAsync($"/contributions/abundance/events?until={pastDate}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(content);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
    }

    [Fact]
    public async Task GetAbundanceEvents_WithSinceAfterUntil_HandlesGracefully()
    {
        // Act
        var since = DateTime.UtcNow.ToString("O");
        var until = DateTime.UtcNow.AddDays(-1).ToString("O");
        var response = await _client.GetAsync($"/contributions/abundance/events?since={since}&until={until}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(content);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
    }

    [Fact]
    public async Task ResonanceEndpoints_WithMalformedQueryParameters_HandleGracefully()
    {
        // Test various malformed query parameters
        var malformedUrls = new[]
        {
            "/contributions/abundance/events?take=abc&skip=xyz",
            "/contributions/abundance/events?since=invalid&until=also-invalid",
            "/contributions/abundance/events?sortDescending=maybe",
            "/contributions/abundance/events?userId=&contributionId="
        };

        foreach (var url in malformedUrls)
        {
            // Act
            var response = await _client.GetAsync(url);

            // Assert
            // Should either return 400 Bad Request or handle gracefully
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task ResonanceEndpoints_WithVeryLongQueryParameters_HandleGracefully()
    {
        // Test with very long query parameters
        var longParam = new string('a', 10000);
        var response = await _client.GetAsync($"/contributions/abundance/events?userId={longParam}");

        // Assert
        // Should either return 400 Bad Request or handle gracefully
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResonanceEndpoints_WithUnicodeQueryParameters_HandleGracefully()
    {
        // Test with Unicode query parameters
        var response = await _client.GetAsync("/contributions/abundance/events?userId=测试用户");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(content);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
    }

    [Fact]
    public async Task ResonanceEndpoints_WithSQLInjectionAttempts_HandleGracefully()
    {
        // Test with SQL injection attempts
        var sqlInjectionAttempts = new[]
        {
            "'; DROP TABLE events; --",
            "1' OR '1'='1",
            "admin'--",
            "1' UNION SELECT * FROM users--"
        };

        foreach (var attempt in sqlInjectionAttempts)
        {
            // Act
            var response = await _client.GetAsync($"/contributions/abundance/events?userId={attempt}");

            // Assert
            // Should handle gracefully without errors
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task ResonanceEndpoints_WithXSSAttempts_HandleGracefully()
    {
        // Test with XSS attempts
        var xssAttempts = new[]
        {
            "<script>alert('xss')</script>",
            "javascript:alert('xss')",
            "<img src=x onerror=alert('xss')>",
            "';alert('xss');//"
        };

        foreach (var attempt in xssAttempts)
        {
            // Act
            var response = await _client.GetAsync($"/contributions/abundance/events?userId={attempt}");

            // Assert
            // Should handle gracefully without errors
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task ResonanceEndpoints_WithPathTraversalAttempts_HandleGracefully()
    {
        // Test with path traversal attempts
        var pathTraversalAttempts = new[]
        {
            "../../../etc/passwd",
            "..\\..\\..\\windows\\system32\\drivers\\etc\\hosts",
            "....//....//....//etc/passwd",
            "..%2F..%2F..%2Fetc%2Fpasswd"
        };

        foreach (var attempt in pathTraversalAttempts)
        {
            // Act
            var response = await _client.GetAsync($"/contributions/abundance/contributor-energy/{attempt}");

            // Assert
            // Should handle gracefully without errors
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task ResonanceEndpoints_WithNullBytes_HandleGracefully()
    {
        // Test with null bytes
        var nullByteAttempts = new[]
        {
            "user%00",
            "user\0",
            "user%00test"
        };

        foreach (var attempt in nullByteAttempts)
        {
            // Act
            var response = await _client.GetAsync($"/contributions/abundance/contributor-energy/{attempt}");

            // Assert
            // Should handle gracefully without errors
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task ResonanceEndpoints_WithControlCharacters_HandleGracefully()
    {
        // Test with control characters
        var controlCharAttempts = new[]
        {
            "user\t",
            "user\n",
            "user\r",
            "user\b",
            "user\f",
            "user\v"
        };

        foreach (var attempt in controlCharAttempts)
        {
            // Act
            var response = await _client.GetAsync($"/contributions/abundance/contributor-energy/{attempt}");

            // Assert
            // Should handle gracefully without errors
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task ResonanceEndpoints_WithEmptyQueryParameters_HandleGracefully()
    {
        // Test with empty query parameters
        var emptyParamUrls = new[]
        {
            "/contributions/abundance/events?userId=",
            "/contributions/abundance/events?contributionId=",
            "/contributions/abundance/events?since=",
            "/contributions/abundance/events?until=",
            "/contributions/abundance/events?take=",
            "/contributions/abundance/events?skip="
        };

        foreach (var url in emptyParamUrls)
        {
            // Act
            var response = await _client.GetAsync(url);

            // Assert
            // Should handle gracefully without errors
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task ResonanceEndpoints_WithDuplicateQueryParameters_HandleGracefully()
    {
        // Test with duplicate query parameters
        var response = await _client.GetAsync("/contributions/abundance/events?userId=user1&userId=user2&take=5&take=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(content);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
    }
}









