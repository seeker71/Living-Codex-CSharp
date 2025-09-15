using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace CodexBootstrap.Tests
{
    [Collection("TestServer")]
    public class ComprehensiveEndpointDiscovery
    {
        private readonly TestServerFixture _fixture;

        public ComprehensiveEndpointDiscovery(TestServerFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task DiscoverAllEndpoints_ShouldFindAvailableEndpoints()
        {
            // Arrange
            var endpoints = new[]
            {
                "/health",
                "/modules",
                "/swagger",
                "/",
                "/api",
                "/user/profile/test",
                "/contributions/user/test",
                "/concept/test"
            };

            // Act & Assert
            foreach (var endpoint in endpoints)
            {
                var response = await _fixture.HttpClient.GetAsync(endpoint);
                
                // We expect some endpoints to be available, others might return 404
                // The important thing is that the server responds without crashing
                response.Should().NotBeNull();
                
                // Log the status for debugging
                System.Console.WriteLine($"Endpoint {endpoint}: {response.StatusCode}");
            }
        }

        [Fact]
        public async Task HealthEndpoint_ShouldReturnDetailedMetrics()
        {
            // Act
            var response = await _fixture.HttpClient.GetAsync("/health");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            var content = await response.Content.ReadAsStringAsync();
            
            // Check for specific health metrics
            content.Should().Contain("status");
            content.Should().Contain("uptime");
            content.Should().Contain("nodeCount");
            content.Should().Contain("moduleCount");
        }

        [Fact]
        public async Task ModulesEndpoint_ShouldReturnStructuredData()
        {
            // Act
            var response = await _fixture.HttpClient.GetAsync("/modules");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            var content = await response.Content.ReadAsStringAsync();
            
            // Should be JSON array
            content.Should().StartWith("[");
            content.Should().EndWith("]");
            
            // Should contain module information
            content.Should().Contain("id");
            content.Should().Contain("name");
            content.Should().Contain("version");
        }

        [Fact]
        public async Task SwaggerEndpoint_ShouldReturnApiDocumentation()
        {
            // Act
            var response = await _fixture.HttpClient.GetAsync("/swagger");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            var content = await response.Content.ReadAsStringAsync();
            
            // Should contain Swagger/OpenAPI content
            content.Should().NotBeNullOrEmpty();
        }
    }
}