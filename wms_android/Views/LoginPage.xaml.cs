using Microsoft.Maui.Controls;
using wms_android.shared.Data;
using wms_android.Services;
using wms_android.ViewModels;
using wms_android.shared.Interfaces;

namespace wms_android.Views;

public partial class LoginPage : ContentPage
{
    private readonly LoginViewModel _viewModel;

    // Constructor that accepts LoginViewModel
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    // Optional default constructor if needed
    public LoginPage()
    {
        InitializeComponent();

        // Retrieve services from DI container
        var httpClient = ServiceHelper.GetService<HttpClient>();
        var userService = ServiceHelper.GetService<IUserService>();
        var notificationService = ServiceHelper.GetService<wms_android.Interfaces.INotificationService>();
        var sessionManager = ServiceHelper.GetService<wms_android.Interfaces.ISessionManager>();

        // Set the BindingContext using the resolved services
        _viewModel = new LoginViewModel(httpClient, userService, notificationService, sessionManager, this.Navigation);
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Clear any previous credentials for security
        if (_viewModel != null)
        {
            _viewModel.Username = string.Empty;
            _viewModel.Password = string.Empty;
            _viewModel.ErrorMessage = string.Empty;
            _viewModel.HasError = false;
        }
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        if (_viewModel != null)
        {
            await _viewModel.LoginCommand.ExecuteAsync(null);
        }
    }
}

