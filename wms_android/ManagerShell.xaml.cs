using wms_android.Views;
using wms_android.Services;

namespace wms_android
{
    public partial class ManagerShell : Shell
    {
        private readonly LoadingService _loadingService = LoadingService.Instance;

        public static bool IsManagerLocked { get; set; } = false;

        public ManagerShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(BranchManagerReportView), typeof(BranchManagerReportView));
            Routing.RegisterRoute(nameof(DeliveryView), typeof(DeliveryView));
            Routing.RegisterRoute(nameof(DeliveryConfirmationView), typeof(DeliveryConfirmationView));
            Routing.RegisterRoute("ParcelConfirmationView", typeof(ParcelConfirmationView));
            Routing.RegisterRoute(nameof(DispatchView), typeof(DispatchView));
            Routing.RegisterRoute(nameof(AllParcelsView), typeof(AllParcelsView));
            Routing.RegisterRoute(nameof(ParcelsView), typeof(ParcelsView));
            Routing.RegisterRoute(nameof(ListParcelsView), typeof(ListParcelsView));
            Routing.RegisterRoute(nameof(ReceiptView), typeof(ReceiptView));
            Routing.RegisterRoute(nameof(ReceiptCartView), typeof(ReceiptCartView));
        }

        protected override void OnNavigating(ShellNavigatingEventArgs args)
        {
            if (IsManagerLocked &&
                (args.Source == ShellNavigationSource.ShellSectionChanged ||
                 args.Source == ShellNavigationSource.ShellItemChanged ||
                 args.Source == ShellNavigationSource.ShellContentChanged))
            {
                args.Cancel();
            }
            else
            {
                base.OnNavigating(args);
            }
        }
    }
}
