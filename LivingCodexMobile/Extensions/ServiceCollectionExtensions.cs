using LivingCodexMobile.Services;

namespace LivingCodexMobile.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register API configuration
        services.Configure<ApiConfiguration>(configuration.GetSection("Api"));
        services.AddSingleton<IApiConfiguration, ApiConfiguration>();

        // Register HttpClient
        services.AddHttpClient<IApiService, GenericApiService>((serviceProvider, client) =>
        {
            var config = serviceProvider.GetRequiredService<IApiConfiguration>();
            client.BaseAddress = new Uri(config.BaseUrl);
            client.Timeout = config.Timeout;
        });

        // Register logging and error handling services
        services.AddSingleton<ILoggingService, LoggingService>();
        services.AddSingleton<IErrorHandlingService, ErrorHandlingService>();

        // Register services
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<INewsFeedService, NewsFeedService>();

        return services;
    }

    public static IServiceCollection AddApiServices(this IServiceCollection services, string baseUrl, TimeSpan? timeout = null)
    {
        // Register API configuration with custom values
        services.AddSingleton<IApiConfiguration>(provider => new ApiConfiguration
        {
            BaseUrl = baseUrl,
            Timeout = timeout ?? TimeSpan.FromSeconds(30),
            EnableLogging = true,
            EnableRetry = true,
            MaxRetryAttempts = 3
        });

        // Register HttpClient
        services.AddHttpClient<IApiService, GenericApiService>((serviceProvider, client) =>
        {
            var config = serviceProvider.GetRequiredService<IApiConfiguration>();
            client.BaseAddress = new Uri(config.BaseUrl);
            client.Timeout = config.Timeout;
        });

        // Register logging and error handling services
        services.AddSingleton<ILoggingService, LoggingService>();
        services.AddSingleton<IErrorHandlingService, ErrorHandlingService>();

        // Register services
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<INewsFeedService, NewsFeedService>();

        return services;
    }
}
