using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;
using System;
using System.Threading.Tasks;
using wms_android.Interfaces;
using Microsoft.Maui.Controls;
using System.Diagnostics;

namespace wms_android.ViewModels
{
    public partial class SettingsViewModel : BaseViewModel
    {
        private readonly IDialogService _dialogService;

        [ObservableProperty]
        private string _deviceModel = string.Empty;

        [ObservableProperty]
        private string _osVersion = string.Empty;

        [ObservableProperty]
        private string _appVersion = string.Empty;

        [ObservableProperty]
        private bool _notificationsEnabled = true;

        [ObservableProperty]
        private int _selectedLanguageIndex = 0;

        [ObservableProperty]
        private int _selectedThemeIndex = 0;

        public SettingsViewModel(IDialogService dialogService) : base()
        {
            _dialogService = dialogService;
            Title = "Settings";
            LoadDeviceInfo();
            LoadSettings();
        }

        private void LoadDeviceInfo()
        {
            try
            {
                DeviceModel = DeviceInfo.Current.Model;
                OsVersion = $"{DeviceInfo.Current.Platform} {DeviceInfo.Current.VersionString}";
                AppVersion = AppInfo.Current.VersionString;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SettingsViewModel: Error loading device info: {ex.Message}");
                DeviceModel = "Unknown";
                OsVersion = "Unknown";
                AppVersion = "1.0.0";
            }
        }

        private void LoadSettings()
        {
            // Load saved settings from preferences
            NotificationsEnabled = Preferences.Get("NotificationsEnabled", true);
            SelectedLanguageIndex = Preferences.Get("SelectedLanguage", 0);
            SelectedThemeIndex = Preferences.Get("SelectedTheme", 0);
        }

        [RelayCommand]
        private void SaveNotificationSetting()
        {
            Preferences.Set("NotificationsEnabled", NotificationsEnabled);
            Debug.WriteLine($"SettingsViewModel: Notifications enabled: {NotificationsEnabled}");
        }

        [RelayCommand]
        private void SaveLanguageSetting()
        {
            Preferences.Set("SelectedLanguage", SelectedLanguageIndex);
            Debug.WriteLine($"SettingsViewModel: Language index: {SelectedLanguageIndex}");
        }

        [RelayCommand]
        private void SaveThemeSetting()
        {
            Preferences.Set("SelectedTheme", SelectedThemeIndex);
            Debug.WriteLine($"SettingsViewModel: Theme index: {SelectedThemeIndex}");
        }

        [RelayCommand]
        private async Task LogoutAsync()
        {
            try
            {
                // Show confirmation dialog
                var confirmed = await _dialogService.ShowConfirmationAsync(
                    "Logout Confirmation", 
                    "Are you sure you want to log out?",
                    "Logout",
                    "Cancel");

                if (!confirmed)
                {
                    Debug.WriteLine("SettingsViewModel: Logout cancelled by user");
                    return;
                }

                Debug.WriteLine("SettingsViewModel: User confirmed logout, clearing session...");

                // Session will be handled by the new session manager automatically

                // Clear all stored authentication data
                ClearUserSession();

                // Show logout success message
                await _dialogService.ShowAlertAsync("Logged Out", "You have been successfully logged out.");

                // Reset lock state and navigate to login
                ManagerShell.IsManagerLocked = false;
                Application.Current.MainPage = new AppShell();
                await Shell.Current.GoToAsync("//LoginPage");

                Debug.WriteLine("SettingsViewModel: Logout completed successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SettingsViewModel: Error during logout: {ex.Message}");
                await _dialogService.ShowAlertAsync("Error", "An error occurred while logging out. Please try again.");
            }
        }

        private void ClearUserSession()
        {
            // Remove all stored user data
            Preferences.Remove("AuthToken");
            Preferences.Remove("CurrentUsername");
            Preferences.Remove("UserRole");
            Preferences.Remove("CurrentUserId");

            Debug.WriteLine("SettingsViewModel: User session data cleared");
        }

        [RelayCommand]
        private async Task ShowAboutAsync()
        {
            var aboutMessage = $"Levels WMS Android\n\n" +
                              $"Version: {AppVersion}\n" +
                              $"Device: {DeviceModel}\n" +
                              $"OS: {OsVersion}\n\n" +
                              $"© {DateTime.Now.Year} Chronos Technologies\n" +
                              $"All rights reserved.";

            await _dialogService.ShowAlertAsync("About", aboutMessage);
        }
    }
}