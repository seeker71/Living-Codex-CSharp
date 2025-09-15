using LivingCodexMobile.Services;
using Microsoft.Extensions.Configuration;

namespace LivingCodexMobile.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register API configuration
        services.Configure<ApiConfiguration>(config => 
        {
            config.BaseUrl = configuration["Api:BaseUrl"] ?? "http://localhost:5002";
            config.Timeout = TimeSpan.FromSeconds(int.Parse(configuration["Api:Timeout"] ?? "30"));
            config.EnableLogging = bool.Parse(configuration["Api:EnableLogging"] ?? "true");
            config.EnableRetry = bool.Parse(configuration["Api:EnableRetry"] ?? "true");
            config.MaxRetryAttempts = int.Parse(configuration["Api:MaxRetryAttempts"] ?? "3");
        });
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
        services.AddScoped<INodeExplorerService, NodeExplorerService>();
        services.AddScoped<IConceptService, ConceptService>();
        services.AddScoped<IEnergyService, EnergyService>();
        services.AddSingleton<IMediaRendererService, MediaRendererService>();

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
        services.AddScoped<INodeExplorerService, NodeExplorerService>();
        services.AddScoped<IConceptService, ConceptService>();
        services.AddScoped<IEnergyService, EnergyService>();
        services.AddSingleton<IMediaRendererService, MediaRendererService>();

        return services;
    }
}
