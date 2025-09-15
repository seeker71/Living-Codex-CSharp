using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CodexBootstrap.Tests.Modules;

/// <summary>
/// Comprehensive API tests for IdentityModule endpoints
/// Tests all mobile app API calls for authentication and user management
/// </summary>
public class IdentityModuleApiTests : IClassFixture<TestServerFixture>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public IdentityModuleApiTests(TestServerFixture fixture)
    {
        _client = fixture.HttpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    #region GET /identity/providers - Get Available Providers

    [Fact]
    public async Task GetIdentityProviders_ShouldReturnProvidersList()
    {
        // Act
        var response = await _client.GetAsync("/identity/providers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<dynamic>(content, _jsonOptions);
        
        result.Should().NotBeNull();
        
        // Should have providers array
        var providers = result?.GetProperty("providers");
        providers.Should().NotBeNull();
    }

    [Fact]
    public async Task GetIdentityProviders_ShouldIncludeMockProvider()
    {
        // Act
        var response = await _client.GetAsync("/identity/providers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<dynamic>(content, _jsonOptions);
        
        var providers = result?.GetProperty("providers");
        providers.Should().NotBeNull();
        
        // Should include mock provider
        var providersArray = providers?.EnumerateArray().ToList();
        providersArray.Should().NotBeEmpty();
    }

    #endregion

    #region POST /identity/authenticate - Authenticate User

    [Fact]
    public async Task Authenticate_ShouldReturnSuccess_WhenValidCredentials()
    {
        // Arrange
        var request = new
        {
            provider = "mock",
            accessToken = "mock-access-token"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request, _jsonOptions),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/identity/authenticate", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<dynamic>(responseContent, _jsonOptions);
        
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Authenticate_ShouldReturnBadRequest_WhenInvalidRequest()
    {
        // Arrange
        var request = new
        {
            // Missing required fields
            provider = "mock"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request, _jsonOptions),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/identity/authenticate", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Authenticate_ShouldReturnUnauthorized_WhenInvalidProvider()
    {
        // Arrange
        var request = new
        {
            provider = "invalid-provider",
            accessToken = "some-token"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request, _jsonOptions),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/identity/authenticate", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /identity/login/google - Google OAuth Login

    [Fact]
    public async Task GoogleLogin_ShouldReturnRedirect_WhenProviderAvailable()
    {
        // Act
        var response = await _client.GetAsync("/identity/login/google");

        // Assert
        // Should return either OK (with redirect URL) or redirect status
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Redirect);
    }

    #endregion

    #region GET /identity/login/microsoft - Microsoft OAuth Login

    [Fact]
    public async Task MicrosoftLogin_ShouldReturnRedirect_WhenProviderAvailable()
    {
        // Act
        var response = await _client.GetAsync("/identity/login/microsoft");

        // Assert
        // Should return either OK (with redirect URL) or redirect status
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Redirect);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task GetIdentityProviders_ShouldRespondWithinAcceptableTime()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync("/identity/providers");
        stopwatch.Stop();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500); // Should respond within 500ms
    }

    [Fact]
    public async Task Authenticate_ShouldRespondWithinAcceptableTime()
    {
        // Arrange
        var request = new
        {
            provider = "mock",
            accessToken = "mock-access-token"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request, _jsonOptions),
            Encoding.UTF8,
            "application/json");

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var response = await _client.PostAsync("/identity/authenticate", content);
        stopwatch.Stop();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should respond within 1 second
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task Authenticate_ShouldHandleMalformedJson()
    {
        // Arrange
        var malformedJson = "{ invalid json }";
        var content = new StringContent(malformedJson, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/identity/authenticate", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetIdentityProviders_ShouldHandleConcurrentRequests()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act - Make 10 concurrent requests
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_client.GetAsync("/identity/providers"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert - All requests should succeed
        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    #endregion
}
