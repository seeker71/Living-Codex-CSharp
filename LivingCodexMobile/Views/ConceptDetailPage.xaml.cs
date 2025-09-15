using LivingCodexMobile.Models;
using LivingCodexMobile.ViewModels;

namespace LivingCodexMobile.Views;

public partial class ConceptDetailPage : ContentPage
{
    private readonly Concept _concept;
    private readonly ConceptDiscoveryViewModel _viewModel;

    public ConceptDetailPage(Concept concept, ConceptDiscoveryViewModel viewModel)
    {
        InitializeComponent();
        _concept = concept;
        _viewModel = viewModel;
        
        BindingContext = _concept;
    }

    private async void OnInterestClicked(object? sender, EventArgs e)
    {
        try
        {
            var conceptCard = new ConceptCard { Concept = _concept };
            await _viewModel.ToggleInterestAsync(conceptCard);
            
            // Update the button text and color
            InterestButton.Text = _concept.IsInterested ? "Interested ✓" : "Mark Interest";
            InterestButton.BackgroundColor = _concept.IsInterested ? Colors.Green : Colors.LightBlue;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to toggle interest: {ex.Message}", "OK");
        }
    }

    private async void OnExploreRelatedClicked(object? sender, EventArgs e)
    {
        try
        {
            // Navigate to a related concepts page or show in a modal
            await DisplayAlert("Explore Related", "Related concepts exploration not yet implemented", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to explore related concepts: {ex.Message}", "OK");
        }
    }

    private async void OnShareClicked(object? sender, EventArgs e)
    {
        try
        {
            // Implement concept sharing
            await DisplayAlert("Share", "Concept sharing not yet implemented", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to share concept: {ex.Message}", "OK");
        }
    }

    private async void OnEditClicked(object? sender, EventArgs e)
    {
        try
        {
            // Navigate to concept editing page
            await DisplayAlert("Edit", "Concept editing not yet implemented", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to edit concept: {ex.Message}", "OK");
        }
    }
}
