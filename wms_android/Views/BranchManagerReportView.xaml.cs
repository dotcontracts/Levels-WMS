using Microsoft.Maui.Controls;
using wms_android.ViewModels;

namespace wms_android.Views
{
    public partial class BranchManagerReportView : ContentPage
    {
        private readonly BranchManagerReportViewModel _viewModel;

        public BranchManagerReportView(BranchManagerReportViewModel viewModel)
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
                await _viewModel.LoadReportData();
            }
        }
    }
}
