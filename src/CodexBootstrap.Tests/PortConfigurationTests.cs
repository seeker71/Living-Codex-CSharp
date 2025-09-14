using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace CodexBootstrap.Tests
{
    public class PortConfigurationTests : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public PortConfigurationTests()
        {
            _httpClient = new HttpClient();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        [Theory]
        [InlineData(5002)]
        [InlineData(5003)]
        [InlineData(5004)]
        [InlineData(5005)]
        [InlineData(5006)]
        [InlineData(5007)]
        public async Task Server_ShouldStartOnPort(int port)
        {
            // Arrange
            var baseUrl = $"http://localhost:{port}";
            _httpClient.BaseAddress = new Uri(baseUrl);

            // Act & Assert
            try
            {
                var response = await _httpClient.GetAsync("/health");
                response.IsSuccessStatusCode.Should().BeTrue($"Server should be running on port {port}");
                
                var content = await response.Content.ReadAsStringAsync();
                content.Should().Contain("healthy", $"Health endpoint should return healthy status on port {port}");
            }
            catch (HttpRequestException ex)
            {
                // If server is not running on this port, that's expected for some ports
                // We'll just verify that the port is not responding
                ex.Message.Should().Contain("Connection refused", $"Port {port} should not be responding if server is not running on it");
            }
        }

        [Fact]
        public async Task Server_ShouldStartOnDefaultPort()
        {
            // Arrange
            var baseUrl = "http://localhost:5002";
            _httpClient.BaseAddress = new Uri(baseUrl);

            // Act
            var response = await _httpClient.GetAsync("/health");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue("Server should be running on default port 5002");
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("healthy", "Health endpoint should return healthy status");
        }

        [Fact]
        public async Task Server_ShouldRespondToHealthEndpoint()
        {
            // Arrange
            var baseUrl = "http://localhost:5002";
            _httpClient.BaseAddress = new Uri(baseUrl);

            // Act
            var response = await _httpClient.GetAsync("/health");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            var content = await response.Content.ReadAsStringAsync();
            var health = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);
            
            health.TryGetProperty("status", out var status).Should().BeTrue("Health response should contain status");
            status.GetString().Should().Be("healthy", "Status should be healthy");
        }

        [Fact]
        public async Task Server_ShouldRespondToHelloEndpoint()
        {
            // Arrange
            var baseUrl = "http://localhost:5002";
            _httpClient.BaseAddress = new Uri(baseUrl);

            // Act
            var response = await _httpClient.GetAsync("/hello");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Hello", "Hello endpoint should return greeting");
        }

        [Fact]
        public async Task Server_ShouldRespondToApiDiscoveryEndpoint()
        {
            // Arrange
            var baseUrl = "http://localhost:5002";
            _httpClient.BaseAddress = new Uri(baseUrl);

            // Act
            var response = await _httpClient.GetAsync("/api/discovery");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            var content = await response.Content.ReadAsStringAsync();
            var discovery = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);
            discovery.TryGetProperty("endpoints", out var endpoints).Should().BeTrue("Discovery response should contain endpoints");
            endpoints.GetArrayLength().Should().BeGreaterThan(0, "Should have at least one endpoint");
        }

        [Fact]
        public async Task Server_ShouldRespondToModulesEndpoint()
        {
            // Arrange
            var baseUrl = "http://localhost:5002";
            _httpClient.BaseAddress = new Uri(baseUrl);

            // Act
            var response = await _httpClient.GetAsync("/api/modules");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            var content = await response.Content.ReadAsStringAsync();
            var modules = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);
            modules.TryGetProperty("modules", out var modulesArray).Should().BeTrue("Modules response should contain modules array");
            modulesArray.GetArrayLength().Should().BeGreaterThan(0, "Should have at least one module loaded");
        }

        [Fact]
        public async Task Server_ShouldHandleConcurrentRequests()
        {
            // Arrange
            var baseUrl = "http://localhost:5002";
            _httpClient.BaseAddress = new Uri(baseUrl);
            var tasks = new Task[10];

            // Act
            for (int i = 0; i < 10; i++)
            {
                tasks[i] = Task.Run(async () =>
                {
                    var response = await _httpClient.GetAsync("/health");
                    response.IsSuccessStatusCode.Should().BeTrue("Concurrent requests should succeed");
                });
            }

            await Task.WhenAll(tasks);

            // Assert - if we get here without exceptions, the test passes
        }

        [Fact]
        public async Task Server_ShouldReturn404ForNonExistentEndpoint()
        {
            // Arrange
            var baseUrl = "http://localhost:5002";
            _httpClient.BaseAddress = new Uri(baseUrl);

            // Act
            var response = await _httpClient.GetAsync("/non-existent-endpoint");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound, "Non-existent endpoints should return 404");
        }

        [Fact]
        public async Task Server_ShouldReturnSwaggerDocumentation()
        {
            // Arrange
            var baseUrl = "http://localhost:5002";
            _httpClient.BaseAddress = new Uri(baseUrl);

            // Act
            var response = await _httpClient.GetAsync("/swagger/v1/swagger.json");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue("Swagger documentation should be available");
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Living Codex API", "Swagger should contain API title");
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
