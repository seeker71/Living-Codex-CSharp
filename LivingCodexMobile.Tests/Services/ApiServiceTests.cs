using FluentAssertions;
using LivingCodexMobile.Models;
using LivingCodexMobile.Services;
using Microsoft.Extensions.Logging;
using Moq;
using LivingCodexMobile.Tests.Helpers;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace LivingCodexMobile.Tests.Services;

public class ApiServiceTests
{
    private TestHttpMessageHandler _handler = null!;
    private readonly Mock<ILoggingService> _loggingServiceMock;
    private readonly Mock<IErrorHandlingService> _errorHandlingServiceMock;
    private readonly GenericApiService _apiService;

    public ApiServiceTests()
    {
        _handler = TestHttpMessageHandler.FromResponse(new HttpResponseMessage(HttpStatusCode.OK));
        var httpClient = new HttpClient(_handler);
        _loggingServiceMock = new Mock<ILoggingService>();
        _errorHandlingServiceMock = new Mock<IErrorHandlingService>();
        
        _apiService = new GenericApiService(
            httpClient,
            Mock.Of<ILogger<GenericApiService>>(),
            _loggingServiceMock.Object,
            _errorHandlingServiceMock.Object);
    }

    [Fact]
    public async Task GetAsync_WithValidResponse_ShouldReturnData()
    {
        // Arrange
        var expectedUser = new User
        {
            Id = "123",
            Username = "testuser",
            Email = "test@example.com",
            DisplayName = "Test User"
        };

        var responseJson = JsonSerializer.Serialize(new ApiResponse<User>
        {
            Success = true,
            Data = expectedUser,
            Message = "Success"
        });

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _handler = TestHttpMessageHandler.FromResponse(httpResponse);
        var httpClient = new HttpClient(_handler);
        typeof(GenericApiService)
            .GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(_apiService, httpClient);

        // Act
        var result = await _apiService.GetAsync<ApiResponse<User>>("/users/123");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().BeEquivalentTo(expectedUser);
    }

    [Fact]
    public async Task PostAsync_WithValidRequest_ShouldReturnData()
    {
        // Arrange
        var request = new UserCreateRequest
        {
            Username = "newuser",
            Email = "new@example.com",
            DisplayName = "New User"
        };

        var expectedUser = new User
        {
            Id = "456",
            Username = "newuser",
            Email = "new@example.com",
            DisplayName = "New User"
        };

        var responseJson = JsonSerializer.Serialize(new ApiResponse<User>
        {
            Success = true,
            Data = expectedUser,
            Message = "User created successfully"
        });

        var httpResponse = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _handler = TestHttpMessageHandler.FromResponse(httpResponse);
        var httpClient = new HttpClient(_handler);
        typeof(GenericApiService)
            .GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(_apiService, httpClient);

        // Act
        var result = await _apiService.PostAsync<UserCreateRequest, ApiResponse<User>>("/users", request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().BeEquivalentTo(expectedUser);
    }

    [Fact]
    public async Task GetAsync_WithHttpError_ShouldHandleError()
    {
        // Arrange
        _handler = TestHttpMessageHandler.Throws(new HttpRequestException("Network error"));
        var httpClient = new HttpClient(_handler);
        typeof(GenericApiService)
            .GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(_apiService, httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => 
            _apiService.GetAsync<ApiResponse<User>>("/users/123"));
    }

    [Fact]
    public async Task PostAsync_WithInvalidJson_ShouldHandleError()
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("invalid json", Encoding.UTF8, "application/json")
        };

        _handler = TestHttpMessageHandler.FromResponse(httpResponse);
        var httpClient = new HttpClient(_handler);
        typeof(GenericApiService)
            .GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(_apiService, httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<JsonException>(() => 
            _apiService.PostAsync<UserCreateRequest, ApiResponse<User>>("/users", new UserCreateRequest()));
    }
}

public class UserCreateRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}
