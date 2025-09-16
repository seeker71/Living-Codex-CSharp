using FluentAssertions;
using LivingCodexMobile.Models;
using LivingCodexMobile.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace LivingCodexMobile.Tests.Integration;

public class ApiIntegrationTests
{
    private readonly ServiceCollection _services;
    private readonly ServiceProvider _serviceProvider;

    public ApiIntegrationTests()
    {
        _services = new ServiceCollection();
        
        // Register test services
        _services.AddSingleton<IApiConfiguration>(provider => new ApiConfiguration
        {
            BaseUrl = "http://localhost:5002",
            Timeout = TimeSpan.FromSeconds(30),
            EnableLogging = true,
            EnableRetry = true,
            MaxRetryAttempts = 3
        });

        _services.AddSingleton<ILoggingService, LoggingService>();
        _services.AddSingleton<IErrorHandlingService, ErrorHandlingService>();
        _services.AddHttpClient();
        _services.AddSingleton<HttpClient>(sp => new HttpClient { Timeout = TimeSpan.FromSeconds(30) });
        _services.AddSingleton<IApiService, GenericApiService>();
        _services.AddSingleton<IAuthenticationService, AuthenticationService>();
        _services.AddSingleton<INewsFeedService, NewsFeedService>();
        _services.AddSingleton<IConceptService, ConceptService>();
        _services.AddSingleton<IEnergyService, EnergyService>();

        _serviceProvider = _services.BuildServiceProvider();
    }

    [Fact]
    public async Task AuthenticationService_GetAvailableProviders_ShouldReturnProviders()
    {
        // Arrange
        var authService = _serviceProvider.GetRequiredService<IAuthenticationService>();

        // Act
        var providers = await authService.GetAvailableProvidersAsync();

        // Assert
        providers.Should().NotBeNull();
        providers.Providers.Should().NotBeEmpty();
    }

    [Fact]
    public async Task NewsFeedService_GetTrendingTopics_ShouldReturnTopics()
    {
        // Arrange
        var newsService = _serviceProvider.GetRequiredService<INewsFeedService>();

        // Act
        var topics = await newsService.GetTrendingTopicsAsync();

        // Assert
        topics.Should().NotBeNull();
    }

    [Fact]
    public async Task ConceptService_GetConcepts_ShouldReturnConcepts()
    {
        // Arrange
        var conceptService = _serviceProvider.GetRequiredService<IConceptService>();

        // Act
        var concepts = await conceptService.GetConceptsAsync();

        // Assert
        concepts.Should().NotBeNull();
    }

    [Fact]
    public async Task EnergyService_GetCollectiveEnergy_ShouldReturnEnergy()
    {
        // Arrange
        var energyService = _serviceProvider.GetRequiredService<IEnergyService>();

        // Act
        var energy = await energyService.GetCollectiveEnergyAsync();

        // Assert
        energy.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task ApiService_WithInvalidEndpoint_ShouldHandleError()
    {
        // Arrange: use a real GenericApiService with a handler returning 404
        var handler = new LivingCodexMobile.Tests.Helpers.TestHttpMessageHandler((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("Not Found", Encoding.UTF8, "text/plain")
        }));
        var httpClient = new HttpClient(handler);
        var logger = _serviceProvider.GetRequiredService<ILogger<GenericApiService>>();
        var logging = _serviceProvider.GetRequiredService<ILoggingService>();
        var errors = _serviceProvider.GetRequiredService<IErrorHandlingService>();
        var apiService = new GenericApiService(httpClient, logger, logging, errors);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => 
            apiService.GetAsync<ApiResponse<object>>("/invalid/endpoint"));
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
