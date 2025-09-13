using LivingCodexMobile.ViewModels;

namespace LivingCodexMobile;

public partial class MainPage : ContentPage
{
    public MainPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}