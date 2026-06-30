using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using wms_android.Services;
using wms_android.Utils;
using wms_android.Interfaces;
using Microsoft.Maui.Controls;
using System.Linq;

namespace wms_android.ViewModels
{
    public class PrinterDiagnosticViewModel : BaseViewModel
    {
        private readonly IPrinterService _printerService;
        
        public event EventHandler<DiagnosticResult> DiagnosticCompleted;
        public event EventHandler<string> PrinterStatusUpdated;
        public event EventHandler<ComprehensiveDiagnosticResult> ComprehensiveDiagnosticCompleted;
        
        public ICommand PrinterDiagnosticCommand { get; }
        public ICommand PrintTestPageCommand { get; }
        public ICommand ResetPrinterCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand DirectTestCommand { get; }
        public ICommand ComprehensiveDiagnosticCommand { get; }
        public ICommand AutoFixCommand { get; }
        public ICommand ManualResetCommand { get; }
        public ICommand TestAdaptivePrinterCommand { get; }
        
        private DiagnosticResult _lastResult;
        private ComprehensiveDiagnosticResult _lastComprehensiveResult;
        private Utils.AdaptivePrinterDiagnosticResult _lastAdaptiveResult;
        
        public PrinterDiagnosticViewModel(IPrinterService printerService) : base()
        {
            _printerService = printerService;
            Title = "Printer Diagnostics";
            
            PrinterDiagnosticCommand = new Command(async () => await RunDiagnostic());
            PrintTestPageCommand = new Command(async () => await PrintTestPage());
            ResetPrinterCommand = new Command(async () => await ResetPrinter());
            BackCommand = new Command(async () => await GoBack());
            DirectTestCommand = new Command(async () => await RunDirectTest());
            ComprehensiveDiagnosticCommand = new Command(async () => await RunComprehensiveDiagnostic());
            AutoFixCommand = new Command(async () => await RunAutoFix(), () => _lastComprehensiveResult != null && !_lastComprehensiveResult.Success);
            ManualResetCommand = new Command(async () => await RunManualReset());
            TestAdaptivePrinterCommand = new Command(async () => await TestAdaptivePrinter());
            
            // Update printer status when view model is created
            UpdatePrinterStatus();
            
            Debug.WriteLine("PrinterDiagnosticViewModel initialized with IPrinterService");
        }
        
        private async Task RunComprehensiveDiagnostic()
        {
            if (IsBusy)
                return;
                
            IsBusy = true;
            
            try
            {
                OnPrinterStatusUpdated("Running comprehensive printer diagnostics...");
                Debug.WriteLine("PrinterDiagnostic: Starting comprehensive diagnostic test");
                
                // Run the comprehensive diagnostic
                var result = await ComprehensivePrinterDiagnostics.RunFullDiagnosticAsync();
                _lastComprehensiveResult = result;
                
                OnComprehensiveDiagnosticCompleted(result);
                
                // Update command availability after getting results
                ((Command)AutoFixCommand).ChangeCanExecute();
                
                if (result.Success)
                {
                    OnPrinterStatusUpdated("Comprehensive diagnostic completed successfully. All systems operational.");
                    await ShowAlert("Diagnostic Success", "All printer systems are working correctly!", "OK");
                }
                else
                {
                    OnPrinterStatusUpdated($"Comprehensive diagnostic found issues. Auto-fix is now available.");
                    
                    // Show a summary of key issues
                    var summary = "";
                    if (!result.AndroidContextAvailable)
                        summary += "• Android context not available\n";
                    if (!result.DeviceDetectionConsistent)
                        summary += "• Device detection inconsistent\n";
                    if (!result.SystemInitializationSuccessful && result.PrinterFactoryDetection == "A90")
                        summary += "• System initialization failed\n";
                    if (!result.PrinterServiceInitialized)
                        summary += "• Printer service initialization failed\n";
                    if (!result.PrinterHardwareCommunication)
                        summary += "• Hardware communication failed\n";
                    
                    bool tryAutoFix = await Application.Current.MainPage.DisplayAlert(
                        "Diagnostic Issues Found", 
                        $"Issues detected:\n{summary}\nWould you like to try automatic fixes?", 
                        "Auto Fix", "See Details");
                        
                    if (tryAutoFix)
                    {
                        await RunAutoFix();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PrinterDiagnostic: Error in comprehensive diagnostic: {ex.Message}");
                OnPrinterStatusUpdated($"Comprehensive diagnostic error: {ex.Message}");
                await ShowAlert("Diagnostic Error", $"Error running comprehensive diagnostic: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        private async Task RunDiagnostic()
        {
            if (IsBusy)
                return;
                
            IsBusy = true;
            
            try
            {
                Android.Util.Log.Debug("PrinterDiagnostic", "Starting diagnostic test using IPrinterService...");
                OnPrinterStatusUpdated("Running diagnostic test using printer service...");
                
                // Test printer initialization
                var initialized = await _printerService.InitializePrinterAsync();
                if (initialized)
                {
                    OnPrinterStatusUpdated("Printer initialized successfully.");
                    
                    // Test printer status
                    var status = await _printerService.GetPrinterStatusAsync();
                    OnPrinterStatusUpdated($"Printer status: {status}");
                    
                    // Test basic printing
                    var testText = "Printer Diagnostic Test\nDevice compatibility check\nTest completed successfully";
                    var printResult = await _printerService.PrintTextAsync(testText);
                    
                    if (printResult)
                    {
                        OnPrinterStatusUpdated("Basic print test prepared successfully.");
                        
                        // Start the print job
                        var startResult = await _printerService.StartPrintJobAsync();
                        if (startResult)
                        {
                            OnPrinterStatusUpdated("Diagnostic test printed successfully.");
                            _lastResult = new DiagnosticResult { Success = true, ErrorMessage = null };
                        }
                        else
                        {
                            OnPrinterStatusUpdated("Failed to start diagnostic print job.");
                            _lastResult = new DiagnosticResult { Success = false, ErrorMessage = "Failed to start print job" };
                        }
                    }
                    else
                    {
                        OnPrinterStatusUpdated("Basic print test failed.");
                        _lastResult = new DiagnosticResult { Success = false, ErrorMessage = "Print test failed" };
                    }
                }
                else
                {
                    OnPrinterStatusUpdated("Printer initialization failed.");
                    _lastResult = new DiagnosticResult { Success = false, ErrorMessage = "Printer initialization failed" };
                }
                
                Android.Util.Log.Debug("PrinterDiagnostic", $"Diagnostic complete: {_lastResult.Success}");
                OnDiagnosticCompleted(_lastResult);
                
                if (_lastResult.Success)
                {
                    Android.Util.Log.Debug("PrinterDiagnostic", "Diagnostic successful");
                    OnPrinterStatusUpdated("Diagnostic completed successfully. Printer is working properly.");
                }
                else
                {
                    Android.Util.Log.Debug("PrinterDiagnostic", $"Diagnostic failed: {_lastResult.ErrorMessage}");
                    OnPrinterStatusUpdated($"Diagnostic failed: {_lastResult.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                Android.Util.Log.Debug("PrinterDiagnostic", $"Error in diagnostic: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error in diagnostic: {ex.Message}");
                OnPrinterStatusUpdated($"Error: {ex.Message}");
                _lastResult = new DiagnosticResult { Success = false, ErrorMessage = ex.Message };
                OnDiagnosticCompleted(_lastResult);
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        private async Task PrintTestPage()
        {
            if (IsBusy)
                return;
                
            IsBusy = true;
            
            try
            {
                OnPrinterStatusUpdated("Printing test page using printer service...");
                
                // Initialize printer
                var initialized = await _printerService.InitializePrinterAsync();
                if (!initialized)
                {
                    OnPrinterStatusUpdated("Failed to initialize printer for test page.");
                    await ShowAlert("Error", "Failed to initialize printer for test page.", "OK");
                    return;
                }
                
                // Build comprehensive test page content
                var testPageContent = BuildTestPageContent();
                
                // Print the test page
                var printResult = await _printerService.PrintTextAsync(testPageContent);
                
                if (printResult)
                {
                    OnPrinterStatusUpdated("Test page content prepared successfully.");
                    
                    // Test QR code printing if supported
                    try
                    {
                        await _printerService.PrintQRCodeAsync("TEST_QR_CODE_DEVICE_AGNOSTIC", 200, 200);
                        OnPrinterStatusUpdated("Test QR code prepared successfully.");
                    }
                    catch (Exception qrEx)
                    {
                        Debug.WriteLine($"QR code test failed: {qrEx.Message}");
                        OnPrinterStatusUpdated("Test page prepared successfully (QR code preparation failed).");
                    }
                    
                    // Start the actual print job
                    var startResult = await _printerService.StartPrintJobAsync();
                    if (startResult)
                    {
                        OnPrinterStatusUpdated("Test page printed successfully.");
                        await ShowAlert("Success", "Test page printed successfully.", "OK");
                    }
                    else
                    {
                        OnPrinterStatusUpdated("Failed to start test page print job.");
                        await ShowAlert("Error", "Failed to start print job for test page.", "OK");
                    }
                }
                else
                {
                    OnPrinterStatusUpdated("Failed to prepare test page content.");
                    await ShowAlert("Error", "Failed to prepare test page content.", "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error printing test page: {ex.Message}");
                OnPrinterStatusUpdated($"Error: {ex.Message}");
                await ShowAlert("Error", $"Error printing test page: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        private string BuildTestPageContent()
        {
            return $@"========== PRINTER TEST PAGE ==========
Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
Device: {(_printerService is VanstonePrinterService ? "A90" : "CS30")}
Service: {_printerService.GetType().Name}

--- Text Formatting Test ---
Normal text line
Bold formatting test
Center aligned text

--- Character Test ---
ABCDEFGHIJKLMNOPQRSTUVWXYZ
abcdefghijklmnopqrstuvwxyz
0123456789
!@#$%^&*()_+-=[]{{}}|;:,.<>?

--- Special Characters ---
äöüß çñáéíóú àèìòù

--- Line Test ---
--------------------------------
Line above should be dashed

Test completed successfully!
============================

";
        }
        
        private async Task ResetPrinter()
        {
            if (IsBusy)
                return;
                
            IsBusy = true;
            
            try
            {
                OnPrinterStatusUpdated("Resetting printer using printer service...");
                
                // Initialize printer (which typically resets it)
                var initialized = await _printerService.InitializePrinterAsync();
                
                if (initialized)
                {
                    OnPrinterStatusUpdated("Printer reset successfully.");
                    await ShowAlert("Success", "Printer reset successfully.", "OK");
                }
                else
                {
                    OnPrinterStatusUpdated("Failed to reset printer.");
                    await ShowAlert("Error", "Failed to reset printer.", "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error resetting printer: {ex.Message}");
                OnPrinterStatusUpdated($"Error: {ex.Message}");
                await ShowAlert("Error", $"Error resetting printer: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        private async Task GoBack()
        {
            await Application.Current.MainPage.Navigation.PopAsync();
        }
        
        private void UpdatePrinterStatus()
        {
            try
            {
                // Get status through the printer service
                OnPrinterStatusUpdated($"Printer service: {_printerService.GetType().Name} ready");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating printer status: {ex.Message}");
                OnPrinterStatusUpdated($"Error: {ex.Message}");
            }
        }
        
        private async Task ShowAlert(string title, string message, string cancel)
        {
            await Application.Current.MainPage.DisplayAlert(title, message, cancel);
        }
        
        private void OnDiagnosticCompleted(DiagnosticResult result)
        {
            DiagnosticCompleted?.Invoke(this, result);
        }
        
        private void OnPrinterStatusUpdated(string status)
        {
            PrinterStatusUpdated?.Invoke(this, status);
        }
        
        private async Task RunDirectTest()
        {
            if (IsBusy)
                return;
                
            IsBusy = true;
            
            try
            {
                Android.Util.Log.Debug("PrinterDiagnostic", "Starting direct test using IPrinterService...");
                OnPrinterStatusUpdated("Running direct test using printer service...");
                
                // Test direct printing capability
                var initialized = await _printerService.InitializePrinterAsync();
                
                if (initialized)
                {
                    var directTestContent = $@"===== DIRECT TEST =====
Device Type: {(_printerService is VanstonePrinterService ? "A90" : "CS30")}
Service: {_printerService.GetType().Name}
Test Time: {DateTime.Now:HH:mm:ss}
Status: DIRECT TEST SUCCESSFUL
=======================";
                    
                    var result = await _printerService.PrintTextAsync(directTestContent);
                    
                    if (result)
                    {
                        OnPrinterStatusUpdated("Direct test content prepared successfully.");
                        
                        // Start the print job
                        var startResult = await _printerService.StartPrintJobAsync();
                        if (startResult)
                        {
                            Android.Util.Log.Debug("PrinterDiagnostic", "Direct test successful!");
                            OnPrinterStatusUpdated("Direct test successfully printed test page!");
                        }
                        else
                        {
                            Android.Util.Log.Debug("PrinterDiagnostic", "Direct test failed to start print job.");
                            OnPrinterStatusUpdated("Direct test failed. Print job could not be started.");
                        }
                    }
                    else
                    {
                        Android.Util.Log.Debug("PrinterDiagnostic", "Direct test failed.");
                        OnPrinterStatusUpdated("Direct test failed. Print operation unsuccessful.");
                    }
                }
                else
                {
                    Android.Util.Log.Debug("PrinterDiagnostic", "Direct test failed - initialization failed.");
                    OnPrinterStatusUpdated("Direct test failed. Printer initialization unsuccessful.");
                }
            }
            catch (Exception ex)
            {
                Android.Util.Log.Debug("PrinterDiagnostic", $"Error in direct test: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error in direct test: {ex.Message}");
                OnPrinterStatusUpdated($"Error in direct test: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        private void OnComprehensiveDiagnosticCompleted(ComprehensiveDiagnosticResult result)
        {
            ComprehensiveDiagnosticCompleted?.Invoke(this, result);
        }
        
        private async Task RunAutoFix()
        {
            if (IsBusy)
                return;
                
            if (_lastComprehensiveResult == null)
            {
                await ShowAlert("No Diagnostic Data", "Please run comprehensive diagnostics first before attempting fixes.", "OK");
                return;
            }
                
            IsBusy = true;
            
            try
            {
                OnPrinterStatusUpdated("Applying automatic fixes based on diagnostic results...");
                Debug.WriteLine("PrinterDiagnostic: Starting automatic fixes");
                
                var fixResult = await PrinterFixUtility.ApplyAutomaticFixes(_lastComprehensiveResult);
                
                // Display the fix results
                OnPrinterStatusUpdated("Automatic fixes completed. See results below.");
                DiagnosticCompleted?.Invoke(this, new DiagnosticResult 
                { 
                    Success = fixResult.Success, 
                    ErrorMessage = fixResult.ToString() 
                });
                
                if (fixResult.Success)
                {
                    await ShowAlert("Fixes Applied", 
                        $"Automatic fixes completed successfully!\n\n{fixResult.SuccessfulFixes.Count} fixes applied.\n\nTry printing again.", 
                        "OK");
                        
                    // Suggest running diagnostics again
                    OnPrinterStatusUpdated("Fixes applied. Run comprehensive diagnostics again to verify.");
                }
                else
                {
                    await ShowAlert("Fix Issues", 
                        $"Some fixes failed:\n\n{string.Join("\n", fixResult.FailedFixes.Take(3))}\n\nSee detailed results below.", 
                        "OK");
                }
                
                // Update command availability
                ((Command)AutoFixCommand).ChangeCanExecute();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PrinterDiagnostic: Error in auto fix: {ex.Message}");
                OnPrinterStatusUpdated($"Auto fix error: {ex.Message}");
                await ShowAlert("Fix Error", $"Error applying automatic fixes: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        private async Task RunManualReset()
        {
            if (IsBusy)
                return;
                
            bool confirm = await Application.Current.MainPage.DisplayAlert(
                "Manual Reset", 
                "This will reset all printer components. Continue?", 
                "Yes", "No");
                
            if (!confirm)
                return;
                
            IsBusy = true;
            
            try
            {
                OnPrinterStatusUpdated("Performing manual printer system reset...");
                Debug.WriteLine("PrinterDiagnostic: Starting manual reset");
                
                var resetResult = await PrinterFixUtility.ResetAllPrinterComponents();
                
                // Display the reset results
                OnPrinterStatusUpdated("Manual reset completed. See results below.");
                DiagnosticCompleted?.Invoke(this, new DiagnosticResult 
                { 
                    Success = resetResult.Success, 
                    ErrorMessage = resetResult.ToString() 
                });
                
                if (resetResult.Success)
                {
                    await ShowAlert("Reset Complete", 
                        "Manual reset completed successfully!\n\nTry printing again.", 
                        "OK");
                }
                else
                {
                    await ShowAlert("Reset Issues", 
                        "Some reset operations failed. See detailed results below.", 
                        "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PrinterDiagnostic: Error in manual reset: {ex.Message}");
                OnPrinterStatusUpdated($"Manual reset error: {ex.Message}");
                await ShowAlert("Reset Error", $"Error during manual reset: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        private async Task TestAdaptivePrinter()
        {
            if (IsBusy)
                return;
                
            IsBusy = true;
            
            try
            {
                OnPrinterStatusUpdated("Running adaptive printer system test...");
                Debug.WriteLine("PrinterDiagnostic: Starting adaptive printer test");
                
                // Run the adaptive printer diagnostic
                var result = await Utils.AdaptivePrinterDiagnostics.RunComprehensiveTestAsync();
                _lastAdaptiveResult = result;
                
                // Display results in the UI
                var summary = result.ToString();
                OnPrinterStatusUpdated($"Adaptive printer test completed. Success: {result.Success}");
                
                if (result.Success)
                {
                    await ShowAlert("Adaptive Printer Test Success", 
                        $"The adaptive printer system is working correctly!\n\n" +
                        $"Successful Strategy: {result.SuccessfulStrategyName}\n" +
                        $"Duration: {result.Duration.TotalSeconds:F2} seconds\n\n" +
                        $"Text Printing: {result.TextPrintingWorking}\n" +
                        $"QR Code Printing: {result.QRCodePrintingWorking}\n" +
                        $"Print Job Execution: {result.PrintJobExecutionWorking}", 
                        "OK");
                }
                else
                {
                    var errorDetails = "Issues found:\n";
                    
                    if (!result.DeviceDetectionWorking)
                        errorDetails += "• Device detection failed\n";
                    if (!result.InitializationServiceWorking)
                        errorDetails += "• Initialization service failed\n";
                    if (!result.PrinterServiceWorking)
                        errorDetails += "• Printer service failed\n";
                    if (!string.IsNullOrEmpty(result.ErrorMessage))
                        errorDetails += $"• Error: {result.ErrorMessage}\n";
                    
                    // Check strategy results
                    var workingStrategies = result.StrategyTestResults.Where(s => s.CanHandleDevice && s.InitializationSuccess).ToList();
                    if (workingStrategies.Any())
                    {
                        errorDetails += $"\nWorking strategies found: {string.Join(", ", workingStrategies.Select(s => s.StrategyName))}";
                    }
                    else
                    {
                        errorDetails += "\nNo working strategies found for this device.";
                    }
                    
                    await ShowAlert("Adaptive Printer Test Failed", errorDetails, "OK");
                }
                
                // Trigger diagnostic completed event to update UI
                var adaptiveDiagnosticResult = new DiagnosticResult
                {
                    Success = result.Success,
                    ErrorMessage = result.ErrorMessage,
                    DeviceModel = Android.OS.Build.Model,
                    StartedAt = result.StartTime,
                    CompletedAt = result.EndTime,
                    PrinterType = result.SuccessfulStrategyName ?? "Unknown"
                };
                OnDiagnosticCompleted(adaptiveDiagnosticResult);
                
                Debug.WriteLine($"PrinterDiagnostic: Adaptive printer test completed. Success: {result.Success}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PrinterDiagnostic: Error in adaptive printer test: {ex.Message}");
                OnPrinterStatusUpdated($"Adaptive printer test error: {ex.Message}");
                await ShowAlert("Adaptive Printer Test Error", $"Error running adaptive printer test: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
} 