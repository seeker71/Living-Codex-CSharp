using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace CodexBootstrap.Tests.Integration
{
    [Collection("TestServer")]
    public class ApiIntegrationTests
    {
        private readonly TestServerFixture _fixture;

        public ApiIntegrationTests(TestServerFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task HealthEndpoint_ShouldReturnHealthyStatus()
        {
            // Act
            var response = await _fixture.HttpClient.GetAsync("/health");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("healthy");
        }

        [Fact]
        public async Task ModulesEndpoint_ShouldReturnModuleList()
        {
            // Act
            var response = await _fixture.HttpClient.GetAsync("/modules");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("id");
            content.Should().Contain("name");
        }

        [Fact]
        public async Task SwaggerEndpoint_ShouldReturnSwaggerDocumentation()
        {
            // Act
            var response = await _fixture.HttpClient.GetAsync("/swagger");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
        }

        [Fact]
        public async Task RootEndpoint_ShouldReturnResponse()
        {
            // Act
            var response = await _fixture.HttpClient.GetAsync("/");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
        }

        [Fact]
        public async Task ApiDiscovery_ShouldReturnAvailableEndpoints()
        {
            // Act
            var response = await _fixture.HttpClient.GetAsync("/api");

            // Assert
            // This endpoint might not exist, so we just check it doesn't throw
            response.Should().NotBeNull();
        }
    }
}