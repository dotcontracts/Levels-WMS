using wms_android.ViewModels;

namespace wms_android.Views;

public partial class DispatchView : ContentPage
{
    public DispatchView(DispatchViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        System.Diagnostics.Debug.WriteLine("[DispatchView] OnAppearing called - starting to load dispatch data");
        
        if (BindingContext is DispatchViewModel viewModel)
        {
            System.Diagnostics.Debug.WriteLine($"[DispatchView] ViewModel is DispatchViewModel, calling LoadDataAsync()");
            await viewModel.LoadDataAsync();
            
            System.Diagnostics.Debug.WriteLine($"[DispatchView] LoadDataAsync completed, ViewModel state:");
            System.Diagnostics.Debug.WriteLine($"[DispatchView]   - IsDetailsTabSelected: {viewModel.IsDetailsTabSelected}");
            System.Diagnostics.Debug.WriteLine($"[DispatchView]   - IsParcelsTabSelected: {viewModel.IsParcelsTabSelected}");
            System.Diagnostics.Debug.WriteLine($"[DispatchView]   - FilteredParcels.Count: {viewModel.FilteredParcels.Count}");
            System.Diagnostics.Debug.WriteLine($"[DispatchView]   - UseMultiDestinationMode: {viewModel.UseMultiDestinationMode}");
            System.Diagnostics.Debug.WriteLine($"[DispatchView]   - SelectedDestination: '{viewModel.SelectedDestination}'");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("[DispatchView] ERROR: BindingContext is not DispatchViewModel!");
        }
    }
}