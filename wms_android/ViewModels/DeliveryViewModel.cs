using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using wms_android.Interfaces;   // Using the interface namespace
using System.Diagnostics;
using wms_android.shared.Interfaces;
using wms_android.Views; // Added for navigation to ReceiptView
using System.Collections.Generic; // Added for navigation parameters
using wms_android.shared.Models; // Added for Parcel type
using System.Linq;
using System.Text.RegularExpressions;

namespace wms_android.ViewModels
{
    public partial class DeliveryViewModel : ObservableObject
    {
        // Inject the interface, not the concrete class
        private readonly IScannerService _scannerService;
        private readonly IParcelService _parcelService;

        [ObservableProperty]
        private string? _scannedResult;

        [ObservableProperty]
        private string? _statusMessage;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string? _manualWaybillNumber; // Added property

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsScannedResultVisible))] // Added Notify attribute
        private bool _showScannedResult = false; // Backing field for visibility

        // Added property to control visibility in XAML
        public bool IsScannedResultVisible => _showScannedResult && !string.IsNullOrEmpty(ScannedResult);


        public DeliveryViewModel(IScannerService scannerService, IParcelService parcelService)
        {
            _scannerService = scannerService;
            _parcelService = parcelService;
            StatusMessage = "Enter Waybill or Scan QR Code.";
        }

        [RelayCommand]
        private async Task ScanQrCodeAsync()
        {
            Debug.WriteLine("=== ScanQrCodeAsync method called ===");
            if (IsBusy) 
            {
                Debug.WriteLine("IsBusy is true, returning early");
                return;
            }

            IsBusy = true;
            ScannedResult = null;
            ShowScannedResult = false; // Reset visibility
            StatusMessage = "Initializing scanner...";
            Debug.WriteLine("Scanner initialization started");

            bool isScannerInitialized = false;
            try
            {
                isScannerInitialized = _scannerService.InitializeScanner();
                Debug.WriteLine($"Scanner initialization result: {isScannerInitialized}");

                if (isScannerInitialized)
                {
                    StatusMessage = "Scanner initialized. Starting scan...";
                    Debug.WriteLine("Attempting to scan...");
                    var result = await _scannerService.ScanAsync(TimeSpan.FromSeconds(20));
                    Debug.WriteLine($"Scan result received: '{result}'");

                    if (!string.IsNullOrEmpty(result))
                    {
                        ScannedResult = result;
                        ShowScannedResult = true; // Show result area
                        StatusMessage = $"Scan successful: {result}. Looking up parcel...";
                        Debug.WriteLine($"Scan successful: {result}. About to call ParcelService...");

                        // Call the plural version and expect an IEnumerable<Parcel>
                        Debug.WriteLine("Calling _parcelService.GetParcelsByQRCodeAsync...");
                        var parcels = await _parcelService.GetParcelsByQRCodeAsync(ScannedResult);

                        // Check if the list is not null and has any parcels
                        if (parcels != null && parcels.Any())
                        {
                            var pendingParcels = parcels.Where(p => p.Status != ParcelStatus.Delivered).ToList();

                            if (pendingParcels.Any())
                            {
                                var waybillNumber = pendingParcels.First().WaybillNumber; // Assuming all parcels in the list share the same waybill
                                StatusMessage = $"Found {pendingParcels.Count} pending parcel(s) for Waybill {waybillNumber}. Navigating to confirmation...";
                                Debug.WriteLine($"{pendingParcels.Count} pending parcel(s) for Waybill {waybillNumber} found via QR. Navigating.");
                                // Navigate to DeliveryConfirmationView, passing the list of pending parcels
                                await GoToDeliveryConfirmationView(pendingParcels);
                            }
                            else
                            {
                                var waybillNumber = parcels.First().WaybillNumber; // Assuming all parcels in the list share the same waybill
                                StatusMessage = $"All parcels for Waybill {waybillNumber} have already been delivered.";
                                Debug.WriteLine($"All parcels for Waybill {waybillNumber} already delivered.");
                            }
                        }
                        else
                        {
                            StatusMessage = $"Failed to find any parcels with QR code {ScannedResult}.";
                            Debug.WriteLine($"ParcelService couldn't find any parcels with QR code {ScannedResult}.");
                        }
                    }
                    else
                    {
                        StatusMessage = "Scan timed out or failed. Please try again.";
                        ShowScannedResult = false;
                        Debug.WriteLine("Scan returned null or empty.");
                    }
                }
                else
                {
                    StatusMessage = "Failed to initialize scanner.";
                    ShowScannedResult = false;
                    Debug.WriteLine("Scanner initialization failed.");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"An error occurred during scan: {ex.Message}";
                ShowScannedResult = false;
                Debug.WriteLine($"=== EXCEPTION in ScanQrCodeAsync ===");
                Debug.WriteLine($"Error during scan command execution: {ex.Message}");
                Debug.WriteLine($"Exception type: {ex.GetType().Name}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            finally
            {
                if (isScannerInitialized)
                {
                     _scannerService.CloseScanner();
                     Debug.WriteLine("Scanner closed after operation.");
                }
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task LookupWaybillAsync() // Renamed to match convention
        {
            if (IsBusy || string.IsNullOrWhiteSpace(ManualWaybillNumber))
            {
                StatusMessage = "Please enter a valid Waybill Number.";
                return;
            }

            IsBusy = true;
            ScannedResult = null; // Clear any previous scan result
            ShowScannedResult = false;

            string waybillToLookup = ManualWaybillNumber.Trim().ToUpper();
            if (!waybillToLookup.StartsWith("FHL") && !waybillToLookup.StartsWith("WB") && Regex.IsMatch(waybillToLookup, @"^[A-Z0-9]+$"))
            {
                waybillToLookup = $"FHL{waybillToLookup}";
                Debug.WriteLine($"Auto-prefixed waybill: {ManualWaybillNumber} -> {waybillToLookup}");
            }

            StatusMessage = $"Looking up Waybill: {waybillToLookup}...";
            Debug.WriteLine($"Looking up Waybill: {waybillToLookup}");

            try
            {
                var parcel = await _parcelService.GetParcelByWaybillNumberAsync(waybillToLookup);

                if (parcel != null)
                {
                     // Check if already delivered using the enum
                    if (parcel.Status == ParcelStatus.Delivered)
                    {
                        StatusMessage = $"Parcel {parcel.WaybillNumber} has already been delivered.";
                        Debug.WriteLine($"Parcel {parcel.WaybillNumber} already delivered.");
                    }
                    else
                    {
                        StatusMessage = $"Parcel {parcel.WaybillNumber} found. Navigating to confirmation...";
                        Debug.WriteLine($"Parcel {parcel.WaybillNumber} found via Waybill. Navigating.");
                        // Navigate to DeliveryConfirmationView, passing the parcel data
                        await GoToDeliveryConfirmationView(parcel);
                    }
                }
                else
                {
                    StatusMessage = $"Failed to find parcel with Waybill Number {ManualWaybillNumber}.";
                    Debug.WriteLine($"ParcelService couldn't find parcel with Waybill {ManualWaybillNumber}.");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"An error occurred during lookup: {ex.Message}";
                Debug.WriteLine($"Error during lookup command execution: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                // Optionally clear the manual entry field after lookup?
                // ManualWaybillNumber = string.Empty;
            }
        }

        // Helper method for navigation to avoid code duplication for single parcel lookup
        private async Task GoToDeliveryConfirmationView(Parcel parcel) 
        {
            StatusMessage = "Ready";
            ManualWaybillNumber = string.Empty; 
            ScannedResult = string.Empty; 
            ShowScannedResult = false;

            await Shell.Current.GoToAsync($"{nameof(DeliveryConfirmationView)}", true, new Dictionary<string, object>
            {
                { "ParcelToDeliver", parcel } 
            });
        }

        // Helper method for navigation for multiple parcels from QR scan
        private async Task GoToDeliveryConfirmationView(List<Parcel> parcelsToDeliver)
        {
            // Reset status before navigating away
            StatusMessage = "Ready";
            ManualWaybillNumber = string.Empty; // Clear entry
            ScannedResult = string.Empty; // Clear scan result
            ShowScannedResult = false;

            await Shell.Current.GoToAsync($"{nameof(DeliveryConfirmationView)}", true, new Dictionary<string, object>
            {
                { "ParcelsToDeliver", parcelsToDeliver } // Pass the list of parcel objects
            });
        }

        public void Cleanup()
        {
             Debug.WriteLine("DeliveryViewModel cleanup called.");
             // Consider closing scanner here too if it could be left open on navigation away
             _scannerService?.CloseScanner();
        }
    }
} 