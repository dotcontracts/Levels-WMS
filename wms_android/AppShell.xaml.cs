using wms_android.Views;
using wms_android.Services;

namespace wms_android
{
    public partial class AppShell : Shell
    {
        // Reference to loading service to ensure it's initialized
        private readonly LoadingService _loadingService = LoadingService.Instance;
        
        public AppShell()
        {
            InitializeComponent();
            
            // Register routes for navigation
            Routing.RegisterRoute(nameof(ParcelsView), typeof(ParcelsView));
            Routing.RegisterRoute(nameof(ClerkDashboardView), typeof(ClerkDashboardView));
            Routing.RegisterRoute(nameof(ReceiptView), typeof(ReceiptView));
            Routing.RegisterRoute(nameof(ReceiptCartView), typeof(ReceiptCartView));
            Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
            Routing.RegisterRoute(nameof(ProfileView), typeof(ProfileView));
            Routing.RegisterRoute(nameof(SettingsView), typeof(SettingsView));
            Routing.RegisterRoute(nameof(PrinterDiagnosticView), typeof(PrinterDiagnosticView));
            Routing.RegisterRoute(nameof(ReportView), typeof(ReportView));
            Routing.RegisterRoute(nameof(ListParcelsView), typeof(ListParcelsView));
            Routing.RegisterRoute(nameof(AllParcelsView), typeof(AllParcelsView));
            
            // Add route for DeliveryView
            Routing.RegisterRoute(nameof(DeliveryView), typeof(DeliveryView));
            // Add route for DeliveryConfirmationView
            Routing.RegisterRoute(nameof(DeliveryConfirmationView), typeof(DeliveryConfirmationView));
            // Add route for ParcelConfirmationView
            Routing.RegisterRoute("ParcelConfirmationView", typeof(ParcelConfirmationView));
            // Add route for DispatchView
            Routing.RegisterRoute(nameof(DispatchView), typeof(DispatchView));
            Routing.RegisterRoute(nameof(BranchManagerReportView), typeof(BranchManagerReportView));
        }
    }
}
