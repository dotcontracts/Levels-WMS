using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using wms_android.shared.Models;
using wms_android.shared.Interfaces;
using System.Collections.ObjectModel;
using wms_android.Interfaces;
using wms_android.Utils;
using wms_android.Services;
using Com.Vanstone.Trans.Api;
using Microsoft.Maui.Controls;

namespace wms_android.ViewModels
{
    public class ReceiptViewModel : INotifyPropertyChanged, IQueryAttributable
    {
        private readonly IParcelService _parcelService;
        private readonly IPrinterService _printerService;
        
        private ObservableCollection<Parcel> _parcels = new();
        public ObservableCollection<Parcel> Parcels
        {
            get => _parcels;
            set
            {
                if (_parcels != value)
                {
                    _parcels = value;
                    OnPropertyChanged(nameof(Parcels));
                    OnPropertyChanged(nameof(IsCartMode));
                    OnPropertyChanged(nameof(IsSingleParcelMode));
                }
            }
        }

        public ObservableCollection<string> PaymentMethods { get; set; } = new();

        private string _waybillNumber;
        public string WaybillNumber
        {
            get => _waybillNumber;
            set
            {
                if (_waybillNumber != value)
                {
                    _waybillNumber = value;
                    OnPropertyChanged(nameof(WaybillNumber));
                }
            }
        }

        private decimal _totalAmount;
        public decimal TotalAmount
        {
            get => _totalAmount;
            set
            {
                if (_totalAmount != value)
                {
                    _totalAmount = value;
                    OnPropertyChanged(nameof(TotalAmount));
                }
            }
        }

        private string _paymentMethod;
        public string PaymentMethod
        {
            get => _paymentMethod;
            set
            {
                if (_paymentMethod != value)
                {
                    _paymentMethod = value;
                    OnPropertyChanged(nameof(PaymentMethod));
                }
            }
        }

        private Parcel _parcel;
        public Parcel Parcel
        {
            get => _parcel;
            set
            {
                if (_parcel != value)
                {
                    _parcel = value;
                    OnPropertyChanged(nameof(Parcel));
                    OnPropertyChanged(nameof(IsSingleParcelMode));
                    OnPropertyChanged(nameof(IsCartMode));
                    
                    // Update properties from the Parcel
                    if (value != null)
                    {
                        // Set the waybill number from the parcel
                        if (!string.IsNullOrEmpty(value.WaybillNumber))
                        {
                            WaybillNumber = value.WaybillNumber;
                        }
                        
                        // Set the total amount from the parcel's TotalAmount field
                        TotalAmount = value.TotalAmount;
                        
                        // Set the payment method from the parcel
                        if (!string.IsNullOrEmpty(value.PaymentMethods))
                        {
                            PaymentMethod = value.PaymentMethods;
                        }
                    }
                }
            }
        }

        private bool _receiptPrinted;
        public bool ReceiptPrinted
        {
            get => _receiptPrinted;
            set
            {
                if (_receiptPrinted != value)
                {
                    _receiptPrinted = value;
                    OnPropertyChanged(nameof(ReceiptPrinted));
                    OnPropertyChanged(nameof(PrintStatusMessage));
                }
            }
        }

        public string PrintStatusMessage => ReceiptPrinted 
            ? "Receipt printed successfully!" 
            : "Receipt printing failed. You can try printing again below.";

        public bool IsSingleParcelMode => Parcel != null;
        public bool IsCartMode => Parcels != null && Parcels.Count > 0;
        public DateTime CurrentDate => DateTime.Now;

        public ICommand PrintReceiptCommand { get; }
        public ICommand PrintCartReceiptCommand { get; }

        // Constructor for single parcel receipt
        public ReceiptViewModel(IParcelService parcelService, IPrinterService printerService)
        {
            _parcelService = parcelService;
            _printerService = printerService;
            PrintReceiptCommand = new Command(async () => await PrintReceipt());
            PrintCartReceiptCommand = new Command(async () => await PrintCartReceipt());
            
            // Initialize collections
            Parcels = new ObservableCollection<Parcel>();
            
            // Initialize PaymentMethods
            PaymentMethods.Add("Cash");
            PaymentMethods.Add("Mobile Money");
            PaymentMethods.Add("Credit Card");

            Debug.WriteLine("ReceiptViewModel initialized with IPrinterService");
        }
        
        // Constructor for cart receipt (multiple parcels)
        public ReceiptViewModel(IParcelService parcelService, IPrinterService printerService, ObservableCollection<Parcel> parcels, string waybillNumber, decimal totalAmount, ObservableCollection<string> paymentMethods) : this(parcelService, printerService)
        {
            Parcels = parcels;
            WaybillNumber = waybillNumber;
            TotalAmount = totalAmount;
            PaymentMethods = paymentMethods;
            PaymentMethod = paymentMethods?.FirstOrDefault() ?? "Cash"; // Default to first payment method or "Cash"
            
            // Explicitly trigger mode property notifications
            OnPropertyChanged(nameof(IsCartMode));
            OnPropertyChanged(nameof(IsSingleParcelMode));
        }
        
        // Constructor overload for cart receipt that calculates the total amount
        public ReceiptViewModel(IParcelService parcelService, IPrinterService printerService, ObservableCollection<Parcel> parcels, string waybillNumber) : this(parcelService, printerService)
        {
            Parcels = parcels;
            WaybillNumber = waybillNumber;
            
            // Calculate the total amount from parcels
            decimal calculatedTotal = 0;
            foreach (var parcel in parcels)
            {
                calculatedTotal += parcel.Amount ?? 0;
            }
            
            // Set the total amount
            TotalAmount = calculatedTotal;
            
            // Also ensure each parcel has the total amount set
            foreach (var parcel in parcels)
            {
                parcel.TotalAmount = calculatedTotal;
            }
            
            // Set default payment methods
            var defaultPaymentMethods = new ObservableCollection<string> { "Cash", "Mobile Money", "Credit Card" };
            PaymentMethods = defaultPaymentMethods;
            PaymentMethod = "Cash";
            
            // Debug the values
            Debug.WriteLine($"ReceiptViewModel created with waybill: {waybillNumber}, totalAmount: {TotalAmount}");
            
            // Explicitly trigger mode property notifications
            OnPropertyChanged(nameof(IsCartMode));
            OnPropertyChanged(nameof(IsSingleParcelMode));
        }

        // Implement IQueryAttributable to receive navigation parameters
        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            Debug.WriteLine("ReceiptViewModel: ApplyQueryAttributes called");
            
            try
            {
                if (query.TryGetValue("Parcel", out var parcelData) && parcelData is Parcel parcel)
                {
                    Debug.WriteLine($"ReceiptViewModel: Received parcel - {parcel.WaybillNumber}");
                    Parcel = parcel;
                    
                    // Clear any existing parcels for single parcel mode
                    Parcels.Clear();
                }
                
                if (query.TryGetValue("WaybillNumber", out var waybillData) && waybillData is string waybill)
                {
                    Debug.WriteLine($"ReceiptViewModel: Received waybill number - {waybill}");
                    WaybillNumber = waybill;
                }
                
                if (query.TryGetValue("ReceiptPrinted", out var printedData) && printedData is bool printed)
                {
                    Debug.WriteLine($"ReceiptViewModel: Received print status - {printed}");
                    ReceiptPrinted = printed;
                }

                Debug.WriteLine($"ReceiptViewModel: Applied query attributes - Waybill: {WaybillNumber}, Printed: {ReceiptPrinted}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ReceiptViewModel: Error applying query attributes: {ex.Message}");
            }
        }

        private async Task PrintReceipt()
        {
            try
            {
                Debug.WriteLine("Starting to print receipt using IPrinterService");
                
                // Initialize printer
                var initialized = await _printerService.InitializePrinterAsync();
                if (!initialized)
                {
                    Debug.WriteLine("Failed to initialize printer");
                    return;
                }
                
                // Build the receipt content
                var receiptContent = BuildReceiptContent();
                
                // Print the receipt
                var printResult = await _printerService.PrintTextAsync(receiptContent);
                
                if (printResult)
                {
                    Debug.WriteLine("Receipt content prepared successfully");
                    
                    // Try to print QR code separately if supported
                    if (!string.IsNullOrEmpty(WaybillNumber))
                    {
                        try
                        {
                            await _printerService.PrintQRCodeAsync(WaybillNumber, 300, 300);
                            Debug.WriteLine("QR code prepared successfully");
                        }
                        catch (Exception qrEx)
                        {
                            Debug.WriteLine($"QR code printing failed: {qrEx.Message}");
                            // Continue without QR code
                        }
                    }
                    
                    // Start the print job
                    var startResult = await _printerService.StartPrintJobAsync();
                    if (startResult)
                    {
                        Debug.WriteLine("Receipt printed successfully");
                    }
                    else
                    {
                        Debug.WriteLine("Failed to start receipt print job");
                    }
                }
                else
                {
                    Debug.WriteLine("Failed to prepare receipt content");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error printing receipt: {ex.Message}");
            }
        }

        private async Task PrintCartReceipt()
        {
            try
            {
                Debug.WriteLine("Starting to print cart receipt using IPrinterService");
                
                // Initialize printer
                var initialized = await _printerService.InitializePrinterAsync();
                if (!initialized)
                {
                    Debug.WriteLine("Failed to initialize printer");
                    return;
                }
                
                // Build the cart receipt content
                var receiptContent = BuildCartReceiptContent();
                
                // Print the receipt
                var printResult = await _printerService.PrintTextAsync(receiptContent);
                
                if (printResult)
                {
                    Debug.WriteLine("Cart receipt content prepared successfully");
                    
                    // Try to print QR code for the cart
                    if (!string.IsNullOrEmpty(WaybillNumber))
                    {
                        try
                        {
                            await _printerService.PrintQRCodeAsync(WaybillNumber, 300, 300);
                            Debug.WriteLine("Cart QR code prepared successfully");
                        }
                        catch (Exception qrEx)
                        {
                            Debug.WriteLine($"Cart QR code printing failed: {qrEx.Message}");
                            // Continue without QR code
                        }
                    }
                    
                    // Start the print job
                    var startResult = await _printerService.StartPrintJobAsync();
                    if (startResult)
                    {
                        Debug.WriteLine("Cart receipt printed successfully");
                    }
                    else
                    {
                        Debug.WriteLine("Failed to start cart receipt print job");
                    }
                }
                else
                {
                    Debug.WriteLine("Failed to prepare cart receipt content");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error printing cart receipt: {ex.Message}");
            }
        }

        private string BuildReceiptContent()
        {
            // Get the logged-in username
            var username = Preferences.Get("CurrentUsername", "Staff");
            
            var content = $@"
    FICMA HOME LOGISTICS
        0707136852
ficmahomelogistics19@gmail.com
       WAYBILL RECEIPT

Waybill Number: {WaybillNumber}
Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

";
            
            // Parcel details
            if (Parcel != null)
            {
                content += $@"Item: {Parcel.Description}
From: {Parcel.Sender}
To: {Parcel.Receiver}
Destination: {Parcel.Destination}
Quantity: {Parcel.Quantity}
Rate: Ksh {Parcel.Rate:N2}
Amount: Ksh {Parcel.Amount:N2}

";
            }
            
            content += $@"Payment Method: {PaymentMethod}
Total Amount: Ksh {TotalAmount:N2}
--------------------------------

Processed by: {username}

NB:
1. Contents not checked.
2. Customers are advised to insu
   re their goods if the value exce
   eds Ksh 500.
3. All mirrors/boards are carrie
   d at owner's risk.
4. Cash is not accepted as a cou
   rier, and the company will not b
   e held liable.

";
            
            return content;
        }

        private string BuildCartReceiptContent()
        {
            // Get the logged-in username
            var username = Preferences.Get("CurrentUsername", "Staff");
            
            var content = $@"
    FICMA HOME LOGISTICS
        0707136852
ficmahomelogistics19@gmail.com
       WAYBILL RECEIPT

Waybill Number: {WaybillNumber}
Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

Items: {Parcels?.Count ?? 0} parcels

";
            
            // Parcels details
            if (Parcels != null)
            {
                foreach (var parcel in Parcels)
                {
                    content += $@"From: {parcel.Sender ?? "N/A"}
To: {parcel.Receiver ?? "N/A"}
Destination: {parcel.Destination ?? "N/A"}
Amount: Ksh {parcel.Amount ?? 0:N2}
";
                }
            }
            
            content += $@"
Payment Method: {PaymentMethod}
Total Amount: Ksh {TotalAmount:N2}
--------------------------------

Processed by: {username}

NB:
1. Contents not checked.
2. Customers are advised to insu
   re their goods if the value exce
   eds Ksh 500.
3. All mirrors/boards are carrie
   d at owner's risk.
4. Cash is not accepted as a cou
   rier, and the company will not b
   e held liable.

";
            
            return content;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
