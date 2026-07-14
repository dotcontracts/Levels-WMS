using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using wms_android.shared.Data;
using wms_android.data.Services;
using wms_android.data.Interfaces;
using wms_android.Services;
using wms_android.ViewModels;
using wms_android.Views;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using wms_android.Utils;
using wms_android.Interfaces;
using CommunityToolkit.Maui;
using wms_android.shared.Interfaces;
using wms_android.shared.Services;
using UraniumUI;
using UraniumUI.Material;
using UraniumUI.Icons.FontAwesome;

namespace wms_android
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseMauiCommunityToolkitMediaElement()
                .UseUraniumUI()
                .UseUraniumUIMaterial()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFontAwesomeIconFonts();
                });

            // Load the configuration from embedded appsettings.json in the data project
            var dataAssembly = typeof(AppDbContext).Assembly; // This now points to the shared project
            var resourceNames = dataAssembly.GetManifestResourceNames();

            // Debug: Print out all resource names
            foreach (var name in resourceNames)
            {
                System.Diagnostics.Debug.WriteLine($"Found resource: {name}");
            }

            var resourceName = resourceNames.FirstOrDefault(name => name.EndsWith("appsettings.json", StringComparison.OrdinalIgnoreCase));

            if (resourceName == null)
            {
                throw new FileNotFoundException("Could not find appsettings.json as an embedded resource in the shared project.");
            }

            using var stream = dataAssembly.GetManifestResourceStream(resourceName);

            if (stream == null)
            {
                throw new InvalidOperationException("Failed to load appsettings.json stream from the shared project.");
            }

            var configuration = new ConfigurationBuilder()
                .AddJsonStream(stream)
                .Build();

            builder.Configuration.AddConfiguration(configuration);

            // Define API base URLs
            #if DEBUG
                var localIP = "192.168.1.69"; // Change this to your actual machine IP
                var apiBaseUrl = $"http://{localIP}:8081/";
            #else
                var apiBaseUrl = "https://wmsandroidapi-w74du.ondigitalocean.app";
            #endif

            // Create basic HttpClient first (without session awareness to avoid circular dependency)
            builder.Services.AddTransient<HttpClient>(sp => {
                var client = new HttpClient();

                var baseUrl = apiBaseUrl;
                // For debugging - add more detailed logging
                System.Diagnostics.Debug.WriteLine($"API Base URL configured as: {baseUrl}");
                
                // Ensure baseUrl doesn't include "/api"
                if (baseUrl.EndsWith("/api/")) {
                    baseUrl = baseUrl.Substring(0, baseUrl.Length - 5);
                } else if (baseUrl.EndsWith("/api")) {
                    baseUrl = baseUrl.Substring(0, baseUrl.Length - 4);
                }
                
                try {
                    client.BaseAddress = new Uri(baseUrl);
                    System.Diagnostics.Debug.WriteLine($"Configured HttpClient with BaseAddress: {baseUrl}");
                } catch (Exception ex) {
                    System.Diagnostics.Debug.WriteLine($"Error setting BaseAddress: {ex.Message}");
                    client.BaseAddress = new Uri("https://wmsandroidapi-w74du.ondigitalocean.app");
                }
                return client;
            });
                
            // Register shared services - use these instead of data project equivalents
            // Create a configuration object for services that need it
            builder.Services.AddSingleton<IConfiguration>(sp =>
            {
                var config = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string>
                    {
                        {"ApiSettings:BaseUrl", apiBaseUrl}
                    })
                    .Build();
                return config;
            });
            
            builder.Services.AddTransient<wms_android.shared.Services.ParcelService>();
            builder.Services.AddTransient<wms_android.shared.Interfaces.IParcelService, wms_android.shared.Services.ParcelService>();
            
            // Register UserService - Use the client-side API implementation
            builder.Services.AddTransient<wms_android.shared.Interfaces.IUserService, wms_android.Services.ApiUserService>();
            
            // Register VehicleService
            builder.Services.AddTransient<wms_android.shared.Services.VehicleService>();
            builder.Services.AddTransient<wms_android.shared.Interfaces.IVehicleService, wms_android.shared.Services.VehicleService>();
            
            // Register remaining shared services
            builder.Services.AddTransient<wms_android.shared.Interfaces.ISmsService>(sp => {
                var parcelService = sp.GetRequiredService<wms_android.shared.Interfaces.IParcelService>();
                return new wms_android.shared.Services.SmsService(parcelService);
            });
            builder.Services.AddTransient<wms_android.shared.Interfaces.IAuthService, wms_android.shared.Services.AuthService>();
            
            // Register device-specific services
            builder.Services.AddSingleton<wms_android.Interfaces.IDeviceDetectionService, wms_android.Services.DeviceDetectionService>();
            builder.Services.AddSingleton<wms_android.Interfaces.IPrinterServiceFactory, wms_android.Services.PrinterServiceFactory>();
            
            // Register the new adaptive printer services
            builder.Services.AddTransient<wms_android.Services.AdaptivePrinterInitializationService>();
            builder.Services.AddTransient<wms_android.Services.AdaptivePrinterService>();
            
            // Register IPrinterService using the adaptive printer service as default
            builder.Services.AddTransient<wms_android.Interfaces.IPrinterService>(sp =>
            {
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger<wms_android.Services.AdaptivePrinterService>();
                return new wms_android.Services.AdaptivePrinterService(logger, loggerFactory);
            });
            
            // Also register the legacy printer services for fallback scenarios
            builder.Services.AddTransient<wms_android.Services.CS30PrinterService>();
            builder.Services.AddTransient<wms_android.Services.VanstonePrinterService>();
            
            // Register default scanner service - device detection will happen at runtime
            builder.Services.AddSingleton<wms_android.Interfaces.IScannerService, wms_android.Services.Cs30ScannerService>();
            System.Diagnostics.Debug.WriteLine("Registered Cs30ScannerService as default scanner service.");
            
            // Register other singletons
            builder.Services.AddSingleton<wms_android.Interfaces.IDialogService, wms_android.Services.DialogService>();
            builder.Services.AddSingleton<wms_android.Interfaces.IMainThreadService, wms_android.Services.MainThreadService>();
            builder.Services.AddSingleton<wms_android.Interfaces.INotificationService, wms_android.Services.NotificationService>();
            builder.Services.AddSingleton<wms_android.Interfaces.IConnectivityService, wms_android.Services.ConnectivityService>();
            builder.Services.AddSingleton<wms_android.Interfaces.IUpdateService, wms_android.Services.UpdateService>();
            builder.Services.AddSingleton<wms_android.Services.UpdateManager>();
            
            // Register session manager
            builder.Services.AddSingleton<wms_android.Interfaces.ISessionManager, wms_android.Services.SessionManager>();
            
            // Register loading service (using its singleton instance)
            builder.Services.AddSingleton<wms_android.Services.LoadingService>(sp => wms_android.Services.LoadingService.Instance);

            // Register view models with their dependencies
            builder.Services.AddTransient<ParcelsViewModel>();
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<ClerkDashboardViewModel>();
            builder.Services.AddTransient<DeliveryViewModel>();
            builder.Services.AddTransient<ReportViewModel>();
            builder.Services.AddTransient<ReceiptViewModel>();
            builder.Services.AddTransient<DeliveryConfirmationViewModel>();
            builder.Services.AddTransient<PrinterDiagnosticViewModel>();
            builder.Services.AddTransient<ProfileViewModel>();
            builder.Services.AddTransient<SettingsViewModel>();
            builder.Services.AddTransient<AllParcelsViewModel>();
            builder.Services.AddTransient<ParcelConfirmationViewModel>();
            builder.Services.AddTransient<DispatchViewModel>();
            
            // Register views
            builder.Services.AddTransient<ParcelsView>();
            builder.Services.AddTransient<ListParcelsView>();
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<SplashScreen>();
            builder.Services.AddTransient<ClerkDashboardView>();
            builder.Services.AddTransient<DeliveryView>();
            builder.Services.AddTransient<ReportView>();
            builder.Services.AddTransient<ReceiptView>();
            builder.Services.AddTransient<DeliveryConfirmationView>();
            builder.Services.AddTransient<PrinterDiagnosticView>();
            builder.Services.AddTransient<ProfileView>();
            builder.Services.AddTransient<SettingsView>();
            builder.Services.AddTransient<AllParcelsView>();
            builder.Services.AddTransient<ParcelConfirmationView>();
            builder.Services.AddTransient<DispatchView>();

            return builder.Build();
        }
    }
}

