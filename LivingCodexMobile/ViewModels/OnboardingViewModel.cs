using System.Collections.ObjectModel;
using System.Windows.Input;
using LivingCodexMobile.Models;
using LivingCodexMobile.Services;

namespace LivingCodexMobile.ViewModels;

public class OnboardingViewModel : BaseViewModel
{
    private readonly INavigation _navigation;
    private readonly IAuthenticationService _authService;
    private int _currentStep = 1;
    private string _nextButtonText = "Next";

    public OnboardingViewModel(INavigation navigation, IAuthenticationService authService)
    {
        _navigation = navigation;
        _authService = authService;
        
        NextCommand = new Command(ExecuteNext);
        SkipCommand = new Command(ExecuteSkip);
        
        InitializeOnboardingSteps();
    }

    public ObservableCollection<OnboardingStep> OnboardingSteps { get; } = new();

    public string NextButtonText
    {
        get => _nextButtonText;
        set => SetProperty(ref _nextButtonText, value);
    }

    public ICommand NextCommand { get; }
    public ICommand SkipCommand { get; }

    private void InitializeOnboardingSteps()
    {
        OnboardingSteps.Add(new OnboardingStep
        {
            StepNumber = 1,
            Title = "Welcome to Living Codex",
            Description = "A consciousness-expanding platform where knowledge flows, resonates, and evolves through collective human intelligence.",
            Icon = "dotnet_bot.png",
            Features = new List<string>
            {
                "• Fractal-based knowledge architecture",
                "• Real-time concept resonance",
                "• Collective intelligence amplification",
                "• Sacred frequency integration (432Hz, 528Hz, 741Hz)"
            }
        });

        OnboardingSteps.Add(new OnboardingStep
        {
            StepNumber = 2,
            Title = "Everything is a Node",
            Description = "Data, structure, flow, state, deltas, policies, and specs all exist as interconnected nodes in our living knowledge graph.",
            Icon = "dotnet_bot.png",
            Features = new List<string>
            {
                "• 1,258+ fractal nodes with 318+ edges",
                "• Meta-nodes describe structure and APIs",
                "• Tiny deltas for minimal change tracking",
                "• Single lifecycle: Compose → Expand → Validate"
            }
        });

        OnboardingSteps.Add(new OnboardingStep
        {
            StepNumber = 3,
            Title = "AI-Powered News Intelligence",
            Description = "Experience news like never before with AI-powered concept extraction, resonance scoring, and fractal transformation.",
            Icon = "dotnet_bot.png",
            Features = new List<string>
            {
                "• Real-time concept extraction from news",
                "• Resonance scoring and energy analysis",
                "• Fractal transformation of information",
                "• Personalized knowledge discovery"
            },
            IsNewsFeedStep = true
        });

        OnboardingSteps.Add(new OnboardingStep
        {
            StepNumber = 4,
            Title = "Join the Collective",
            Description = "Connect with like-minded individuals, contribute to the knowledge graph, and help shape the future of human consciousness.",
            Icon = "dotnet_bot.png",
            Features = new List<string>
            {
                "• OAuth integration with Google/Microsoft",
                "• User discovery by interests and location",
                "• Concept contribution and collaboration",
                "• Real-time collaboration tools"
            }
        });
    }

    private void ExecuteNext()
    {
        if (_currentStep < OnboardingSteps.Count)
        {
            _currentStep++;
            UpdateNextButtonText();
        }
        else
        {
            // Navigate to login/registration
            _navigation.PushAsync(new MainPage());
        }
    }

    private void ExecuteSkip()
    {
        // Navigate directly to login/registration
        _navigation.PushAsync(new MainPage());
    }

    private void UpdateNextButtonText()
    {
        NextButtonText = _currentStep >= OnboardingSteps.Count ? "Get Started" : "Next";
    }
}

public class OnboardingStep
{
    public int StepNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public List<string> Features { get; set; } = new();
    public bool IsNewsFeedStep { get; set; }
}

