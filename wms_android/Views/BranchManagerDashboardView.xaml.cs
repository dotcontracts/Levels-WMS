using Microsoft.Maui.Controls;
using wms_android.ViewModels;

namespace wms_android.Views
{
    public partial class BranchManagerDashboardView : ContentPage
    {
        private readonly BranchManagerDashboardViewModel _viewModel;

        public BranchManagerDashboardView(BranchManagerDashboardViewModel viewModel)
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
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _viewModel?.StopPolling();
        }
    }
}
