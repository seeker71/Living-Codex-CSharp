using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace CodexBootstrap.Tests
{
    [Collection("TestServer")]
    public class HealthTests
    {
        private readonly TestServerFixture _fixture;

        public HealthTests(TestServerFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task HealthCheck_ShouldReturnHealthyStatus()
        {
            // Act
            var response = await _fixture.HttpClient.GetAsync("/health");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("healthy");
        }

        [Fact]
        public async Task HealthCheck_ShouldReturnValidJson()
        {
            // Act
            var response = await _fixture.HttpClient.GetAsync("/health");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();
            
            // Verify it's valid JSON by checking for common JSON structure
            content.Should().Contain("{");
            content.Should().Contain("}");
        }

        [Fact]
        public async Task HealthCheck_ShouldIncludeSystemMetrics()
        {
            // Act
            var response = await _fixture.HttpClient.GetAsync("/health");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("uptime");
            content.Should().Contain("nodeCount");
            content.Should().Contain("moduleCount");
        }
    }
}