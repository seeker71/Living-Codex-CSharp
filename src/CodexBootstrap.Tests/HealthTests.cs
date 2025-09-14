using Xunit;
using FluentAssertions;

namespace CodexBootstrap.Tests;

public class HealthTests : TestBase
{
    public HealthTests() : base() { }

    [Fact]
    public async Task HealthEndpoint_ShouldReturnHealthyStatus()
    {
        // Act
        var health = await GetJsonAsync<dynamic>("/health");

        // Assert
        health.Should().NotBeNull();
        var healthString = health.ToString();
        healthString.Should().Contain("healthy");
    }

    [Fact]
    public async Task HealthEndpoint_ShouldContainModuleMetrics()
    {
        // Act
        var health = await GetJsonAsync<dynamic>("/health");

        // Assert
        var healthString = health.ToString();
        healthString.Should().Contain("moduleCount");
        healthString.Should().Contain("registrationMetrics");
    }

    [Fact]
    public async Task HealthEndpoint_ShouldHaveReasonableModuleCount()
    {
        // Act
        var health = await GetJsonAsync<dynamic>("/health");

        // Assert
        var healthString = health.ToString();
        healthString.Should().Contain("moduleCount");
        
        // Should have at least 40 modules (we know we have 49+)
        var moduleCountMatch = System.Text.RegularExpressions.Regex.Match(healthString, @"""moduleCount"":\s*(\d+)");
        if (moduleCountMatch.Success)
        {
            var moduleCount = int.Parse(moduleCountMatch.Groups[1].Value);
            moduleCount.Should().BeGreaterThan(40);
        }
    }
}
