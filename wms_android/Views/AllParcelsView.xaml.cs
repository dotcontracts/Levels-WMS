using wms_android.ViewModels;
using Microsoft.Maui.Controls;

namespace wms_android.Views
{
    public partial class AllParcelsView : ContentPage
    {
        private readonly AllParcelsViewModel _viewModel;

        public AllParcelsView(AllParcelsViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        protected override async void OnAppearing()
        {
            try
            {
                base.OnAppearing();
                
                // Add delay to ensure page is fully loaded before loading data
                await Task.Delay(100);
                
                await _viewModel.LoadParcelsCommand.ExecuteAsync(null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in AllParcelsView.OnAppearing: {ex.Message}");
                await DisplayAlert("Error", "Failed to load parcels. Please try again.", "OK");
            }
        }
    }
}