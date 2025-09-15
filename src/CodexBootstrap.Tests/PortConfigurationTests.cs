using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace CodexBootstrap.Tests
{
    [Collection("TestServer")]
    public class PortConfigurationTests
    {
        private readonly TestServerFixture _fixture;

        public PortConfigurationTests(TestServerFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task Server_ShouldStartOnPort()
        {
            // Test that the server is running on the assigned port
            var response = await _fixture.HttpClient.GetAsync("/health");
            response.IsSuccessStatusCode.Should().BeTrue("Health endpoint should be accessible");
        }

        [Fact]
        public async Task Modules_ShouldBeAvailableOnPort()
        {
            // Test that modules endpoint is accessible
            var response = await _fixture.HttpClient.GetAsync("/modules");
            response.IsSuccessStatusCode.Should().BeTrue("Modules endpoint should be accessible");
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("id", "Modules endpoint should return module data");
        }

        [Fact]
        public async Task Swagger_ShouldBeAvailableOnPort()
        {
            // Test that swagger endpoint is accessible
            var response = await _fixture.HttpClient.GetAsync("/swagger");
            response.IsSuccessStatusCode.Should().BeTrue("Swagger endpoint should be accessible");
        }
    }
}