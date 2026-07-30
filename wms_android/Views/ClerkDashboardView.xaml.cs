using Microsoft.Maui.Controls;
using wms_android.ViewModels;
using wms_android.Controls;

namespace wms_android.Views
{
    public partial class ClerkDashboardView : ContentPage
    {
        private readonly ClerkDashboardViewModel _viewModel;
        private System.Timers.Timer _lockPollTimer;

        public ClerkDashboardView(ClerkDashboardViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (_viewModel != null)
            {
                await _viewModel.LoadDashboardData();
            }

            StartLockPolling();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            StopLockPolling();
        }

        private void StartLockPolling()
        {
            StopLockPolling();
            _lockPollTimer = new System.Timers.Timer(30000);
            _lockPollTimer.Elapsed += async (s, e) =>
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (_viewModel != null)
                        await _viewModel.RefreshLockStatusCommand.ExecuteAsync(null);
                });
            };
            _lockPollTimer.AutoReset = true;
            _lockPollTimer.Start();
        }

        private void StopLockPolling()
        {
            if (_lockPollTimer != null)
            {
                _lockPollTimer.Stop();
                _lockPollTimer.Dispose();
                _lockPollTimer = null;
            }
        }
    }
}
