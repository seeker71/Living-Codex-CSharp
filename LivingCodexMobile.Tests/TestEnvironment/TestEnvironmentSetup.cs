using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using LivingCodexMobile.Models;
using LivingCodexMobile.Services;
using LivingCodexMobile.Tests.TestData;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace LivingCodexMobile.Tests.TestEnvironment;

/// <summary>
/// Sets up the test environment with all required services and configurations
/// </summary>
public static class TestEnvironmentSetup
{
    private static ServiceProvider? _serviceProvider;
    private static readonly object _lock = new object();

    /// <summary>
    /// Gets or creates the test service provider
    /// </summary>
    public static ServiceProvider GetServiceProvider()
    {
        if (_serviceProvider == null)
        {
            lock (_lock)
            {
                if (_serviceProvider == null)
                {
                    _serviceProvider = CreateTestServiceProvider();
                }
            }
        }
        return _serviceProvider;
    }

    /// <summary>
    /// Creates a test service provider with all required services
    /// </summary>
    private static ServiceProvider CreateTestServiceProvider()
    {
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Add configuration
        services.AddSingleton<IConfiguration>(provider =>
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Api:BaseUrl"] = "http://localhost:5002",
                    ["Api:Timeout"] = "30",
                    ["Api:EnableLogging"] = "true",
                    ["Api:EnableRetry"] = "true",
                    ["Api:MaxRetryAttempts"] = "3"
                })
                .Build();
            return configuration;
        });

        // Add HttpClient factory and default client
        services.AddHttpClient();
        services.AddSingleton<HttpClient>(sp =>
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(30);
            return client;
        });

        // Add mock services
        services.AddSingleton(Mock.Of<IApiService>());
        services.AddSingleton(Mock.Of<IAuthenticationService>());
        services.AddSingleton(Mock.Of<INewsFeedService>());
        services.AddSingleton(Mock.Of<IConceptService>());
        services.AddSingleton(Mock.Of<IEnergyService>());
        services.AddSingleton(Mock.Of<INodeExplorerService>());
        services.AddSingleton(Mock.Of<IMediaRendererService>());
        services.AddSingleton(Mock.Of<ISignalRService>());
        services.AddSingleton(Mock.Of<ILoggingService>());
        services.AddSingleton(Mock.Of<IErrorHandlingService>());

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Resets the test environment
    /// </summary>
    public static void Reset()
    {
        lock (_lock)
        {
            _serviceProvider?.Dispose();
            _serviceProvider = null;
        }
    }

    /// <summary>
    /// Sets up mock responses for API calls
    /// </summary>
    public static void SetupMockApiResponses(ServiceProvider serviceProvider)
    {
        var apiServiceMock = Mock.Get(serviceProvider.GetRequiredService<IApiService>());
        
        // Create test data outside of Moq expressions
        var testUser = TestDataFactory.CreateTestUser();
        var testConcepts = TestDataFactory.CreateTestConcepts();
        var testNewsItems = TestDataFactory.CreateTestNewsItems();
        var timestamp = DateTime.UtcNow;
        
        // Setup successful responses
        apiServiceMock.Setup(x => x.GetAsync<ApiResponse<User>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<User>
            {
                Success = true,
                Data = testUser,
                Timestamp = timestamp
            });

        apiServiceMock.Setup(x => x.GetAsync<ApiResponse<List<Concept>>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<List<Concept>>
            {
                Success = true,
                Data = testConcepts,
                Timestamp = timestamp
            });

        apiServiceMock.Setup(x => x.GetAsync<ApiResponse<List<NewsItem>>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<List<NewsItem>>
            {
                Success = true,
                Data = testNewsItems,
                Timestamp = timestamp
            });

        // Setup error responses
        apiServiceMock.Setup(x => x.GetAsync<ApiResponse<object>>(It.Is<string>(s => s.Contains("error")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<object>
            {
                Success = false,
                Message = "Test error",
                Timestamp = timestamp
            });
    }

    /// <summary>
    /// Sets up mock authentication responses
    /// </summary>
    public static void SetupMockAuthentication(ServiceProvider serviceProvider)
    {
        var authServiceMock = Mock.Get(serviceProvider.GetRequiredService<IAuthenticationService>());
        
        authServiceMock.Setup(x => x.IsAuthenticated)
            .Returns(true);

        authServiceMock.Setup(x => x.CurrentUser)
            .Returns(TestDataFactory.CreateTestUser());

        authServiceMock.Setup(x => x.LoginAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        authServiceMock.Setup(x => x.LogoutAsync())
            .Returns(Task.CompletedTask);
    }

    /// <summary>
    /// Sets up mock concept service responses
    /// </summary>
    public static void SetupMockConceptService(ServiceProvider serviceProvider)
    {
        var conceptServiceMock = Mock.Get(serviceProvider.GetRequiredService<IConceptService>());
        
        // Create test data outside of Moq expressions
        var testConcepts = TestDataFactory.CreateTestConcepts();
        var testConcept = TestDataFactory.CreateTestConcept();
        
        conceptServiceMock.Setup(x => x.GetConceptsAsync(It.IsAny<ConceptQuery>()))
            .ReturnsAsync(testConcepts);

        conceptServiceMock.Setup(x => x.GetConceptAsync(It.IsAny<string>()))
            .ReturnsAsync(testConcept);

        conceptServiceMock.Setup(x => x.SearchConceptsAsync(It.IsAny<ConceptSearchRequest>()))
            .ReturnsAsync(testConcepts);
    }

    /// <summary>
    /// Sets up mock news feed service responses
    /// </summary>
    public static void SetupMockNewsFeedService(ServiceProvider serviceProvider)
    {
        var newsServiceMock = Mock.Get(serviceProvider.GetRequiredService<INewsFeedService>());
        
        // Create test data outside of Moq expressions
        var testNewsItems = TestDataFactory.CreateTestNewsItems();
        var testTrendingTopics = TestDataFactory.CreateTestTrendingTopics();
        
        newsServiceMock.Setup(x => x.GetNewsFeedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(testNewsItems);

        newsServiceMock.Setup(x => x.GetTrendingTopicsAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(testTrendingTopics);

        newsServiceMock.Setup(x => x.SearchNewsAsync(It.IsAny<NewsSearchRequest>()))
            .ReturnsAsync(testNewsItems);
    }

    /// <summary>
    /// Sets up mock energy service responses
    /// </summary>
    public static void SetupMockEnergyService(ServiceProvider serviceProvider)
    {
        var energyServiceMock = Mock.Get(serviceProvider.GetRequiredService<IEnergyService>());
        
        energyServiceMock.Setup(x => x.GetCollectiveEnergyAsync())
            .ReturnsAsync(1000.0);

        energyServiceMock.Setup(x => x.GetContributorEnergyAsync(It.IsAny<string>()))
            .ReturnsAsync(500.0);

        energyServiceMock.Setup(x => x.GetRecentContributionsAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(ContributionTestDataFactory.CreateTestContributions(5));
    }

    /// <summary>
    /// Sets up all mock services
    /// </summary>
    public static void SetupAllMocks(ServiceProvider serviceProvider)
    {
        SetupMockApiResponses(serviceProvider);
        SetupMockAuthentication(serviceProvider);
        SetupMockConceptService(serviceProvider);
        SetupMockNewsFeedService(serviceProvider);
        SetupMockEnergyService(serviceProvider);
    }
}

/// <summary>
/// Test environment configuration
/// </summary>
public class TestEnvironmentConfig
{
    public string ApiBaseUrl { get; set; } = "http://localhost:5002";
    public int TimeoutSeconds { get; set; } = 30;
    public bool EnableLogging { get; set; } = true;
    public bool EnableMocking { get; set; } = true;
    public bool EnablePerformanceMonitoring { get; set; } = true;
}
