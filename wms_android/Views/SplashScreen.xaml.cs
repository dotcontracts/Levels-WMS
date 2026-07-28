using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Maui.Core.Primitives;

namespace wms_android.Views
{
    public partial class SplashScreen : ContentPage
    {
        private bool _hasNavigated = false;

        public SplashScreen()
        {
            InitializeComponent();
            
            // Fallback timer in case video doesn't play or takes too long
            Device.StartTimer(TimeSpan.FromSeconds(5), () => {
                MainThread.BeginInvokeOnMainThread(async () => {
                    if (!_hasNavigated)
                    {
                        await NavigateToMain();
                    }
                });
                return false; // Don't repeat the timer
            });

            // Show fallback content after a brief delay if video fails to load
            Device.StartTimer(TimeSpan.FromSeconds(1), () => {
                MainThread.BeginInvokeOnMainThread(() => {
                    if (SplashVideo.CurrentState != MediaElementState.Playing)
                    {
                        FallbackContent.IsVisible = true;
                    }
                });
                return false;
            });
        }

        private void OnSplashVideoEnded(object sender, EventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(async () => {
                if (!_hasNavigated)
                {
                    await NavigateToMain();
                }
            });
        }

        private async Task NavigateToMain()
        {
            if (_hasNavigated) return;
            
            _hasNavigated = true;
            
            var role = Microsoft.Maui.Storage.Preferences.Get("UserRole", string.Empty);
            if (role == "Manager")
            {
                Application.Current.MainPage = new wms_android.ManagerShell();
                await Shell.Current.GoToAsync("//BranchManagerDashboardView");
            }
            else
            {
                Application.Current.MainPage = new wms_android.AppShell();
                await Shell.Current.GoToAsync("//ClerkDashboardView");
            }
        }
    }
} 