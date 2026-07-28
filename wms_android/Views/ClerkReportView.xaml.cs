using Microsoft.Maui.Controls;
using wms_android.ViewModels;

namespace wms_android.Views
{
    public partial class ClerkReportView : ContentPage
    {
        private readonly ClerkReportViewModel _viewModel;

        public ClerkReportView(ClerkReportViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (_viewModel != null)
                await _viewModel.LoadReportAsync();
        }
    }
}
