using LivingCodexMobile.ViewModels;
using LivingCodexMobile.Models;

namespace LivingCodexMobile.Views;

public partial class OnboardingPage : ContentPage
{
    private readonly OnboardingViewModel _viewModel;

    public OnboardingPage(OnboardingViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    private void OnCarouselCurrentItemChanged(object sender, CurrentItemChangedEventArgs e)
    {
        if (e.CurrentItem is OnboardingStep step)
        {
            UpdatePageIndicators(step.StepNumber);
        }
    }

    private void UpdatePageIndicators(int currentStep)
    {
        // Reset all indicators
        Indicator1.Color = Colors.LightGray;
        Indicator2.Color = Colors.LightGray;
        Indicator3.Color = Colors.LightGray;
        Indicator4.Color = Colors.LightGray;

        // Highlight current step
        switch (currentStep)
        {
            case 1:
                Indicator1.Color = Color.FromArgb("#512BD4");
                break;
            case 2:
                Indicator2.Color = Color.FromArgb("#512BD4");
                break;
            case 3:
                Indicator3.Color = Color.FromArgb("#512BD4");
                break;
            case 4:
                Indicator4.Color = Color.FromArgb("#512BD4");
                break;
        }
    }
}

