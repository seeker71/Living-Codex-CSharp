using LivingCodexMobile.Services;
using LivingCodexMobile.ViewModels;
using LivingCodexMobile.Views;
using LivingCodexMobile.Converters;

namespace LivingCodexMobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Register services
        builder.Services.AddSingleton<HttpClient>(provider =>
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("http://localhost:5002");
            return httpClient;
        });
        builder.Services.AddSingleton<IApiService, GenericApiService>();
        builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();
        builder.Services.AddSingleton<ISignalRService>(provider =>
            new SignalRService("http://localhost:5002/realtime-hub"));
        builder.Services.AddSingleton<IRealtimeNotificationService, RealtimeNotificationService>();
        builder.Services.AddSingleton<IConversationService, ConversationService>();

        // Register ViewModels
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();

        // Register Pages
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<FlyoutMenuPage>();

        // Register Converters
        builder.Services.AddSingleton<BoolToLoginTextConverter>();
        builder.Services.AddSingleton<BoolToButtonTextConverter>();
        builder.Services.AddSingleton<BoolToAltButtonTextConverter>();
        builder.Services.AddSingleton<InvertedBoolConverter>();
        builder.Services.AddSingleton<StringToBoolConverter>();

        return builder.Build();
    }
}