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
            Console.WriteLine($"[HealthTests] Starting HealthCheck_ShouldReturnHealthyStatus at {DateTime.UtcNow:HH:mm:ss.fff}");
            
            // Act
            Console.WriteLine($"[HealthTests] Making GET request to /health...");
            var response = await _fixture.HttpClient.GetAsync("/health");
            Console.WriteLine($"[HealthTests] Response received: {response.StatusCode}");

            // Assert
            Console.WriteLine($"[HealthTests] Asserting response is successful...");
            response.IsSuccessStatusCode.Should().BeTrue();
            
            Console.WriteLine($"[HealthTests] Reading response content...");
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[HealthTests] Content length: {content.Length} characters");
            
            Console.WriteLine($"[HealthTests] Asserting content contains 'healthy'...");
            content.Should().Contain("healthy");
            
            Console.WriteLine($"[HealthTests] HealthCheck_ShouldReturnHealthyStatus completed at {DateTime.UtcNow:HH:mm:ss.fff}");
        }

        [Fact]
        public async Task HealthCheck_ShouldReturnValidJson()
        {
            Console.WriteLine($"[HealthTests] Starting HealthCheck_ShouldReturnValidJson at {DateTime.UtcNow:HH:mm:ss.fff}");
            
            // Act
            Console.WriteLine($"[HealthTests] Making GET request to /health...");
            var response = await _fixture.HttpClient.GetAsync("/health");
            Console.WriteLine($"[HealthTests] Response received: {response.StatusCode}");

            // Assert
            Console.WriteLine($"[HealthTests] Asserting response is successful...");
            response.IsSuccessStatusCode.Should().BeTrue();
            
            Console.WriteLine($"[HealthTests] Reading response content...");
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[HealthTests] Content length: {content.Length} characters");
            
            Console.WriteLine($"[HealthTests] Asserting content is not null or empty...");
            content.Should().NotBeNullOrEmpty();
            
            // Verify it's valid JSON by checking for common JSON structure
            Console.WriteLine($"[HealthTests] Asserting content contains JSON structure...");
            content.Should().Contain("{");
            content.Should().Contain("}");
            
            Console.WriteLine($"[HealthTests] HealthCheck_ShouldReturnValidJson completed at {DateTime.UtcNow:HH:mm:ss.fff}");
        }

        [Fact]
        public async Task HealthCheck_ShouldIncludeSystemMetrics()
        {
            Console.WriteLine($"[HealthTests] Starting HealthCheck_ShouldIncludeSystemMetrics at {DateTime.UtcNow:HH:mm:ss.fff}");
            
            // Act
            Console.WriteLine($"[HealthTests] Making GET request to /health...");
            var response = await _fixture.HttpClient.GetAsync("/health");
            Console.WriteLine($"[HealthTests] Response received: {response.StatusCode}");

            // Assert
            Console.WriteLine($"[HealthTests] Asserting response is successful...");
            response.IsSuccessStatusCode.Should().BeTrue();
            
            Console.WriteLine($"[HealthTests] Reading response content...");
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[HealthTests] Content length: {content.Length} characters");
            
            Console.WriteLine($"[HealthTests] Asserting content contains system metrics...");
            content.Should().Contain("uptime");
            content.Should().Contain("nodeCount");
            content.Should().Contain("moduleCount");
            
            Console.WriteLine($"[HealthTests] HealthCheck_ShouldIncludeSystemMetrics completed at {DateTime.UtcNow:HH:mm:ss.fff}");
        }
    }
}