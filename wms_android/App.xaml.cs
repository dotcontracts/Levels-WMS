using Microsoft.Maui.Controls;
using wms_android.Views;
using wms_android.ViewModels;
using Java.Util;
using wms_android.shared.Interfaces;
using wms_android.Interfaces;
using Microsoft.Maui.Storage;

namespace wms_android
{
    public partial class App : Application
    {
        private readonly IServiceProvider _serviceProvider;
        private Services.UpdateManager _updateManager;
        private ISessionManager _sessionManager;

        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;

            // Create and configure the AppShell
            var appShell = new AppShell();
            
            // Set the MainPage to the AppShell with the SplashScreen
            MainPage = appShell;
            
            // Register the SplashScreen
            Routing.RegisterRoute(nameof(SplashScreen), typeof(SplashScreen));
            
            // For testing: Add mock authentication data
            Preferences.Set("AuthToken", "test-token-for-development");
            Preferences.Set("CurrentUsername", "test-user");
            Preferences.Set("UserRole", "Clerk");
            Preferences.Set("CurrentUserId", 1);

            // Initialize services
            InitializeUpdateManager();
            InitializeSessionManager();

            // Navigate directly to the splash screen first
            Microsoft.Maui.Controls.Shell.Current.GoToAsync("//SplashScreen");

            // Original login code (commented for testing)
            // Microsoft.Maui.Controls.Shell.Current.GoToAsync("//LoginPage");

            // Commented out previous direct navigation code
            // var parcelsViewModel = serviceProvider.GetRequiredService<ParcelsViewModel>();
            // MainPage = new NavigationPage(new ParcelsView(parcelsViewModel));

            // Set the LoginPage without passing navigation initially
            //var userService = serviceProvider.GetRequiredService<IUserService>();
            //var navigation = new NavigationPage();
            //var loginViewModel = new LoginViewModel(userService, navigation.Navigation);
            //var loginPage = new LoginPage(loginViewModel);

            //navigation.PushAsync(loginPage);
            //MainPage = navigation;  


            //var vehicleService = serviceProvider.GetRequiredService<IVehicleService>();
            //MainPage = new NavigationPage(new VehicleSelectionPage(vehicleService));

            //var confirmparcelsViewModel = serviceProvider.GetRequiredService<ConfirmParcelsViewModel>();
            //MainPage = new NavigationPage(new ConfirmParcelsView(confirmparcelsViewModel));
        }

        private void InitializeUpdateManager()
        {
            _updateManager = _serviceProvider.GetService<Services.UpdateManager>();
        }

        private void InitializeSessionManager()
        {
            _sessionManager = _serviceProvider.GetService<ISessionManager>();
            if (_sessionManager != null)
            {
                _sessionManager.SessionExpired += OnSessionExpired;
                _sessionManager.UserLoggedOut += OnUserLoggedOut;
            }
        }

        protected override void OnStart()
        {
            base.OnStart();
            
            // Start update monitoring
            _updateManager?.StartMonitoringAsync();
            
            // Validate existing session on app start
            ValidateSessionOnStartup();
        }

        protected override void OnSleep()
        {
            base.OnSleep();
            // Stop update monitoring when app goes to background
            _updateManager?.StopMonitoringAsync();
        }

        protected override void OnResume()
        {
            base.OnResume();
            
            // Resume update monitoring
            _updateManager?.StartMonitoringAsync();
        }

        private bool IsUserLoggedIn()
        {
            return !string.IsNullOrEmpty(Preferences.Get("AuthToken", string.Empty));
        }

        private void ClearUserSession()
        {
            Preferences.Remove("AuthToken");
            Preferences.Remove("CurrentUsername");
            Preferences.Remove("UserRole");
            Preferences.Remove("CurrentUserId");
            Preferences.Remove("SessionId");
        }

        private async void OnSessionExpired(object sender, EventArgs e)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    await DisplayAlert("Session Expired", "Your session has expired due to inactivity. Please log in again.", "OK");
                    await Shell.Current.GoToAsync("//LoginPage");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error handling session expiry: {ex.Message}");
                }
            });
        }

        private async void OnUserLoggedOut(object sender, EventArgs e)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    await Shell.Current.GoToAsync("//LoginPage");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error handling logout: {ex.Message}");
                }
            });
        }

        private async void ValidateSessionOnStartup()
        {
            try
            {
                if (_sessionManager != null && _sessionManager.HasActiveSession())
                {
                    System.Diagnostics.Debug.WriteLine("Validating existing session on app startup");
                    var isValid = await _sessionManager.ValidateSessionAsync();
                    
                    if (!isValid)
                    {
                        System.Diagnostics.Debug.WriteLine("Existing session is invalid, clearing session data");
                        _sessionManager.ClearSession();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Existing session is valid");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error validating session on startup: {ex.Message}");
            }
        }

        private async Task<bool> DisplayAlert(string title, string message, string accept)
        {
            if (MainPage != null)
            {
                await MainPage.DisplayAlert(title, message, accept);
                return true;
            }
            return false;
        }
    }
}
