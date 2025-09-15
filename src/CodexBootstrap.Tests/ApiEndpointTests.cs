using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace CodexBootstrap.Tests
{
    [Collection("TestServer")]
    public class ApiEndpointTests
    {
        private readonly TestServerFixture _fixture;

        public ApiEndpointTests(TestServerFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task Health_ShouldReturnHealthyStatus()
        {
            // Act
            var response = await _fixture.HttpClient.GetAsync("/health");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("healthy");
        }

        [Fact]
        public async Task Modules_ShouldReturnModuleList()
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
        public async Task Swagger_ShouldReturnDocumentation()
        {
            // Act
            var response = await _fixture.HttpClient.GetAsync("/swagger");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
        }

        [Fact]
        public async Task Root_ShouldReturnResponse()
        {
            // Act
            var response = await _fixture.HttpClient.GetAsync("/");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
        }

        [Fact]
        public async Task UserEndpoints_ShouldBeAccessible()
        {
            // Act
            var response = await _fixture.HttpClient.GetAsync("/user/profile/test");

            // Assert
            // This might return 404 or 400, but should not return 500
            response.StatusCode.Should().NotBe(System.Net.HttpStatusCode.InternalServerError);
        }
    }
}