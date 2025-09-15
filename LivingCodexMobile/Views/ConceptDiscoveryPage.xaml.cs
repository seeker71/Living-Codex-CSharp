using LivingCodexMobile.Models;
using LivingCodexMobile.ViewModels;

namespace LivingCodexMobile.Views;

public partial class ConceptDiscoveryPage : ContentPage
{
    private readonly ConceptDiscoveryViewModel _viewModel;

    public ConceptDiscoveryPage(ConceptDiscoveryViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    private async void OnConceptSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is ConceptCard selectedConcept)
        {
            await _viewModel.SelectConceptAsync(selectedConcept);
            await Navigation.PushAsync(new ConceptDetailPage(selectedConcept.Concept, _viewModel));
        }
    }
}
