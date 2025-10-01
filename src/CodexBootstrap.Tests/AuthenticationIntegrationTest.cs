using System.Net.Http;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CodexBootstrap.Tests
{
    /// <summary>
    /// Simple integration test for authentication functionality
    /// Tests user registration and login without complex database setup
    /// </summary>
    public class AuthenticationIntegrationTest : IClassFixture<TestServerFixture>
    {
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public AuthenticationIntegrationTest(TestServerFixture fixture)
        {
            _client = fixture.HttpClient;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        [Fact]
        public async Task UserRegistration_ShouldSucceed_WithValidData()
        {
            // Arrange
            var request = new
            {
                username = "testuser" + Guid.NewGuid().ToString("N")[..8],
                email = $"test{Guid.NewGuid().ToString("N")[..8]}@example.com",
                password = "TestPassword123!",
                displayName = "Test User"
            };

            var content = new StringContent(
                JsonSerializer.Serialize(request, _jsonOptions),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await _client.PostAsync("/auth/register", content);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent, _jsonOptions);
            
            result.Should().NotBeNull();
            result.Should().ContainKey("success");
            result["success"].ToString().Should().Be("True");
        }

        [Fact]
        public async Task UserLogin_ShouldSucceed_WithValidCredentials()
        {
            // First register a user
            var username = "logintest" + Guid.NewGuid().ToString("N")[..8];
            var email = $"logintest{Guid.NewGuid().ToString("N")[..8]}@example.com";
            var password = "TestPassword123!";

            var registerRequest = new
            {
                username = username,
                email = email,
                password = password,
                displayName = "Login Test User"
            };

            var registerContent = new StringContent(
                JsonSerializer.Serialize(registerRequest, _jsonOptions),
                Encoding.UTF8,
                "application/json");

            var registerResponse = await _client.PostAsync("/auth/register", registerContent);
            registerResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

            // Now try to login
            var loginRequest = new
            {
                usernameOrEmail = username,
                password = password
            };

            var loginContent = new StringContent(
                JsonSerializer.Serialize(loginRequest, _jsonOptions),
                Encoding.UTF8,
                "application/json");

            // Act
            var loginResponse = await _client.PostAsync("/auth/login", loginContent);

            // Assert
            loginResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            var responseContent = await loginResponse.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent, _jsonOptions);
            
            result.Should().NotBeNull();
            result.Should().ContainKey("success");
            result["success"].ToString().Should().Be("True");
            result.Should().ContainKey("token");
        }

        [Fact]
        public async Task UserLogin_ShouldFail_WithInvalidCredentials()
        {
            // Arrange
            var request = new
            {
                usernameOrEmail = "nonexistentuser",
                password = "wrongpassword"
            };

            var content = new StringContent(
                JsonSerializer.Serialize(request, _jsonOptions),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await _client.PostAsync("/auth/login", content);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent, _jsonOptions);
            
            result.Should().NotBeNull();
            result.Should().ContainKey("success");
            result["success"].ToString().Should().Be("False");
        }
    }
}







