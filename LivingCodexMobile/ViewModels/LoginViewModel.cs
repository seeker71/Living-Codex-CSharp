using System.Windows.Input;
using LivingCodexMobile.Models;
using LivingCodexMobile.Services;

namespace LivingCodexMobile.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly IApiService _apiService;
        private readonly IAuthenticationService _authService;
        private string _username = string.Empty;
        private string _password = string.Empty;
        private string _email = string.Empty;
        private bool _isLoginMode = true;
        private string _errorMessage = string.Empty;

        public LoginViewModel(IApiService apiService, IAuthenticationService authService)
        {
            _apiService = apiService;
            _authService = authService;
            Title = "Living Codex";
            
            LoginCommand = new Command(async () => await LoginAsync());
            RegisterCommand = new Command(async () => await RegisterAsync());
            ToggleModeCommand = new Command(() => ToggleMode());
            GoogleLoginCommand = new Command(async () => await LoginWithGoogleAsync());
            MicrosoftLoginCommand = new Command(async () => await LoginWithMicrosoftAsync());
        }

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public bool IsLoginMode
        {
            get => _isLoginMode;
            set => SetProperty(ref _isLoginMode, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public ICommand LoginCommand { get; }
        public ICommand RegisterCommand { get; }
        public ICommand ToggleModeCommand { get; }
        public ICommand GoogleLoginCommand { get; }
        public ICommand MicrosoftLoginCommand { get; }

        private async Task LoginAsync()
        {
            if (IsBusy) return;

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Please enter both username and password.";
                return;
            }

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                var response = await _apiService.AuthenticateAsync(Username, Password);
                
                if (response.Success && response.Data != null)
                {
                    // Navigate to main page
                    await Shell.Current.GoToAsync("//main");
                }
                else
                {
                    ErrorMessage = response.Message ?? "Login failed. Please try again.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Login error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RegisterAsync()
        {
            if (IsBusy) return;

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(Email))
            {
                ErrorMessage = "Please fill in all fields.";
                return;
            }

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                var response = await _apiService.CreateUserAsync(Username, Email, Password);
                
                if (response.Success && response.Data != null)
                {
                    // Auto-login after successful registration
                    await LoginAsync();
                }
                else
                {
                    ErrorMessage = response.Message ?? "Registration failed. Please try again.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Registration error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoginWithGoogleAsync()
        {
            if (IsBusy) return;

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                System.Diagnostics.Debug.WriteLine("Starting Google login...");
                var success = await _authService.LoginWithGoogleAsync();
                
                if (success)
                {
                    System.Diagnostics.Debug.WriteLine("Google login successful, navigating to main page...");
                    // Navigate to main page
                    await Shell.Current.GoToAsync("//main");
                }
                else
                {
                    ErrorMessage = "Google login failed. Please try again.";
                    System.Diagnostics.Debug.WriteLine("Google login failed");
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Google login error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Google login error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoginWithMicrosoftAsync()
        {
            if (IsBusy) return;

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                System.Diagnostics.Debug.WriteLine("Starting Microsoft login...");
                var success = await _authService.LoginWithMicrosoftAsync();
                
                if (success)
                {
                    System.Diagnostics.Debug.WriteLine("Microsoft login successful, navigating to main page...");
                    // Navigate to main page
                    await Shell.Current.GoToAsync("//main");
                }
                else
                {
                    ErrorMessage = "Microsoft login failed. Please try again.";
                    System.Diagnostics.Debug.WriteLine("Microsoft login failed");
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Microsoft login error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Microsoft login error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ToggleMode()
        {
            IsLoginMode = !IsLoginMode;
            ErrorMessage = string.Empty;
        }
    }
}
