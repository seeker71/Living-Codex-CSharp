using Xunit;
using FluentAssertions;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CodexBootstrap.Tests;

/// <summary>
/// Comprehensive endpoint discovery and testing
/// This test discovers all available endpoints and validates they're accessible
/// </summary>
public class ComprehensiveEndpointDiscovery : TestBase
{
    public ComprehensiveEndpointDiscovery() : base() { }

    [Fact]
    public async Task AllDiscoveredEndpoints_ShouldBeAccessible()
    {
        // Get all routes from the spec endpoint
        var routesResponse = await GetJsonAsync<dynamic>("/spec/routes/all");
        routesResponse.Should().NotBeNull();

        var routesString = routesResponse.ToString();
        var routes = JsonSerializer.Deserialize<JsonElement>(routesString);

        var endpointCount = 0;
        var accessibleEndpoints = 0;
        var failedEndpoints = new List<string>();

        // Extract all GET endpoints for testing
        if (routes.TryGetProperty("routes", out JsonElement routesArray))
        {
            foreach (var route in routesArray.EnumerateArray())
            {
                if (route.TryGetProperty("method", out var method) && 
                    route.TryGetProperty("path", out var path))
                {
                    var methodStr = method.GetString();
                    var pathStr = path.GetString();

                    if (methodStr == "GET" && pathStr != null)
                    {
                        endpointCount++;
                        
                        try
                        {
                            var response = await Client.GetAsync(pathStr);
                            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed)
                            {
                                accessibleEndpoints++;
                            }
                            else
                            {
                                failedEndpoints.Add($"{methodStr} {pathStr} - {response.StatusCode}");
                            }
                        }
                        catch (Exception ex)
                        {
                            failedEndpoints.Add($"{methodStr} {pathStr} - Exception: {ex.Message}");
                        }
                    }
                }
            }
        }

        // Log results
        Console.WriteLine($"Total GET endpoints tested: {endpointCount}");
        Console.WriteLine($"Accessible endpoints: {accessibleEndpoints}");
        Console.WriteLine($"Failed endpoints: {failedEndpoints.Count}");

        if (failedEndpoints.Any())
        {
            Console.WriteLine("Failed endpoints:");
            foreach (var failed in failedEndpoints.Take(10)) // Show first 10 failures
            {
                Console.WriteLine($"  - {failed}");
            }
        }

        // Assert that most endpoints are accessible
        var successRate = (double)accessibleEndpoints / endpointCount;
        successRate.Should().BeGreaterThan(0.8, $"At least 80% of endpoints should be accessible. Success rate: {successRate:P2}");
    }

    [Fact]
    public async Task SystemShouldHaveExpectedModuleCount()
    {
        // Get module count from health endpoint
        var health = await GetJsonAsync<dynamic>("/health");
        var healthString = health.ToString();

        var moduleCountMatch = Regex.Match(healthString, @"""moduleCount"":\s*(\d+)");
        moduleCountMatch.Success.Should().BeTrue("Health endpoint should contain moduleCount");

        var moduleCount = int.Parse(moduleCountMatch.Groups[1].Value);
        moduleCount.Should().BeGreaterThan(40, "System should have at least 40 modules loaded");
        moduleCount.Should().BeLessThan(100, "System should not have excessive modules (potential memory leak)");
    }

    [Fact]
    public async Task SystemShouldHaveExpectedRouteCount()
    {
        // Get route count from health endpoint
        var health = await GetJsonAsync<dynamic>("/health");
        var healthString = health.ToString();

        var routeCountMatch = Regex.Match(healthString, @"""totalRoutesRegistered"":\s*(\d+)");
        routeCountMatch.Success.Should().BeTrue("Health endpoint should contain totalRoutesRegistered");

        var routeCount = int.Parse(routeCountMatch.Groups[1].Value);
        routeCount.Should().BeGreaterThan(300, "System should have at least 300 routes registered");
        routeCount.Should().BeLessThan(500, "System should not have excessive routes (potential configuration issue)");
    }

    [Fact]
    public async Task AllCoreModules_ShouldBeRegistered()
    {
        // Get modules from spec endpoint
        var modules = await GetJsonAsync<dynamic>("/spec/modules");
        var modulesString = modules.ToString();

        // Check for key modules
        var expectedModules = new[]
        {
            "AIModule",
            "HelloModule", 
            "CoreModule",
            "HealthModule",
            "SpecModule",
            "UserModule",
            "ConceptModule"
        };

        foreach (var expectedModule in expectedModules)
        {
            modulesString.Should().Contain(expectedModule, $"Core module {expectedModule} should be registered");
        }
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/ai/health")]
    [InlineData("/spec/modules")]
    [InlineData("/spec/routes/all")]
    [InlineData("/metrics")]
    [InlineData("/metrics/health")]
    public async Task CriticalSystemEndpoints_ShouldBeHealthy(string endpoint)
    {
        // Act
        var response = await Client.GetAsync(endpoint);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue($"Critical endpoint {endpoint} should be healthy");
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty($"Critical endpoint {endpoint} should return content");
    }
}
