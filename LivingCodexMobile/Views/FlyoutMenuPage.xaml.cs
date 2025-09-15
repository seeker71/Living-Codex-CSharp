using LivingCodexMobile.Services;

namespace LivingCodexMobile.Views;

public partial class FlyoutMenuPage : ContentPage
{
    private readonly IAuthenticationService _authService;

    public FlyoutMenuPage(IAuthenticationService authService)
    {
        InitializeComponent();
        _authService = authService;
    }

    private async void OnDashboardClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//main/dashboard");
    }

    private async void OnNewsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//main/news");
    }

    private async void OnConceptsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//main/concepts");
    }

    private async void OnExploreClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//main/explore");
    }

    private async void OnSearchClicked(object sender, EventArgs e)
    {
        // TODO: Implement search functionality
        await DisplayAlert("Search", "Search functionality coming soon!", "OK");
    }

    private async void OnProfileClicked(object sender, EventArgs e)
    {
        // TODO: Implement profile page
        await DisplayAlert("Profile", "Profile page coming soon!", "OK");
    }

    private async void OnSettingsClicked(object sender, EventArgs e)
    {
        // TODO: Implement settings page
        await DisplayAlert("Settings", "Settings page coming soon!", "OK");
    }

    private async void OnHelpClicked(object sender, EventArgs e)
    {
        // TODO: Implement help page
        await DisplayAlert("Help", "Help & Support page coming soon!", "OK");
    }

    private async void OnAboutClicked(object sender, EventArgs e)
    {
        await DisplayAlert("About Living Codex", 
            "Living Codex v1.0.0\n\nA consciousness-expanding, fractal-based system implementing the U-CORE framework.\n\nExplore knowledge through interconnected concepts and real-time insights.", 
            "OK");
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        var result = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");
        if (result)
        {
            await _authService.LogoutAsync();
            await Shell.Current.GoToAsync("//login");
        }
    }
}
