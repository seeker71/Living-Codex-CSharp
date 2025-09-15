using LivingCodexMobile.Services;
using LivingCodexMobile.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Windows.Input;

namespace LivingCodexMobile.Tests.UI.UI_Test_Helpers;

/// <summary>
/// Helper class for testing XAML pages and their interactions
/// </summary>
public static class PageTestHelper
{
    /// <summary>
    /// Creates a mock service provider with all required services
    /// </summary>
    public static ServiceProvider CreateMockServiceProvider()
    {
        var services = new ServiceCollection();
        
        // Mock services
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
    /// Creates a DashboardViewModel with mock services
    /// </summary>
    public static DashboardViewModel CreateDashboardViewModel()
    {
        var serviceProvider = CreateMockServiceProvider();
        
        return new DashboardViewModel(
            serviceProvider.GetRequiredService<IApiService>(),
            serviceProvider.GetRequiredService<IConceptService>(),
            serviceProvider.GetRequiredService<ISignalRService>(),
            serviceProvider.GetRequiredService<IAuthenticationService>(),
            serviceProvider.GetRequiredService<IEnergyService>());
    }

    /// <summary>
    /// Creates a LoginViewModel with mock services
    /// </summary>
    public static LoginViewModel CreateLoginViewModel()
    {
        var serviceProvider = CreateMockServiceProvider();
        
        return new LoginViewModel(
            serviceProvider.GetRequiredService<IApiService>(),
            serviceProvider.GetRequiredService<IAuthenticationService>());
    }

    /// <summary>
    /// Creates a NewsFeedViewModel with mock services
    /// </summary>
    public static NewsFeedViewModel CreateNewsFeedViewModel()
    {
        var serviceProvider = CreateMockServiceProvider();
        
        return new NewsFeedViewModel(
            serviceProvider.GetRequiredService<INewsFeedService>(),
            serviceProvider.GetRequiredService<IConceptService>(),
            serviceProvider.GetRequiredService<ILoggingService>(),
            serviceProvider.GetRequiredService<IAuthenticationService>());
    }

    /// <summary>
    /// Creates a ConceptDiscoveryViewModel with mock services
    /// </summary>
    public static ConceptDiscoveryViewModel CreateConceptDiscoveryViewModel()
    {
        var serviceProvider = CreateMockServiceProvider();
        
        return new ConceptDiscoveryViewModel(
            serviceProvider.GetRequiredService<IConceptService>(),
            serviceProvider.GetRequiredService<ILoggingService>(),
            serviceProvider.GetRequiredService<IAuthenticationService>());
    }

    /// <summary>
    /// Creates a NodeExplorerViewModel with mock services
    /// </summary>
    public static NodeExplorerViewModel CreateNodeExplorerViewModel()
    {
        var serviceProvider = CreateMockServiceProvider();
        
        return new NodeExplorerViewModel(
            serviceProvider.GetRequiredService<INodeExplorerService>(),
            serviceProvider.GetRequiredService<IMediaRendererService>(),
            serviceProvider.GetRequiredService<ILoggingService>());
    }
}

/// <summary>
/// Helper class for testing button interactions
/// </summary>
public static class ButtonTestHelper
{
    /// <summary>
    /// Tests if a command can be executed
    /// </summary>
    public static bool CanExecuteCommand(ICommand command, object? parameter = null)
    {
        return command.CanExecute(parameter);
    }

    /// <summary>
    /// Tests if a command can be executed with a specific parameter
    /// </summary>
    public static bool CanExecuteCommand<T>(ICommand command, T parameter)
    {
        return command.CanExecute(parameter);
    }

    /// <summary>
    /// Executes a command and returns the result
    /// </summary>
    public static void ExecuteCommand(ICommand command, object? parameter = null)
    {
        command.Execute(parameter);
    }

    /// <summary>
    /// Executes a command with a specific parameter
    /// </summary>
    public static void ExecuteCommand<T>(ICommand command, T parameter)
    {
        command.Execute(parameter);
    }
}

/// <summary>
/// Helper class for testing input field interactions
/// </summary>
public static class InputFieldTestHelper
{
    /// <summary>
    /// Tests if a property can be set
    /// </summary>
    public static bool CanSetProperty<T>(T currentValue, T newValue)
    {
        return !EqualityComparer<T>.Default.Equals(currentValue, newValue);
    }

    /// <summary>
    /// Tests if a string property is valid
    /// </summary>
    public static bool IsValidString(string? value, bool allowEmpty = false)
    {
        if (string.IsNullOrEmpty(value))
            return allowEmpty;
        
        return !string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Tests if an email is valid
    /// </summary>
    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrEmpty(email))
            return false;
        
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
