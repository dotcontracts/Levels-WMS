using Microsoft.Extensions.Logging;
using wms_android.Interfaces;
using wms_android.shared.Models;
using System.Collections.Generic;

namespace wms_android.Services
{
    /// <summary>
    /// CH10 printer initialization strategy for CH10 POS terminals
    /// </summary>
    public class CH10PrinterInitializationStrategy : IPrinterInitializationStrategy
    {
        private const string TAG = "CH10PrinterInit";
        private readonly ILogger _logger;

        public string StrategyName => "CH10 Printer with AIDL Service";
        public int Priority => 1; // High priority for specific device support

        public CH10PrinterInitializationStrategy(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<bool> CanHandleDeviceAsync()
        {
            try
            {
                // Check if this is a CH10 device by looking at device model/manufacturer
                var deviceModel = Android.OS.Build.Model;
                var deviceManufacturer = Android.OS.Build.Manufacturer;
                
                _logger.LogDebug($"CH10Strategy: Device Model: {deviceModel}, Manufacturer: {deviceManufacturer}");
                
                // CH10 devices typically have specific model names or manufacturers
                // You may need to adjust these values based on actual CH10 device properties
                bool isCH10Device = deviceModel?.Contains("CH10") == true || 
                                   deviceModel?.Contains("H10") == true ||
                                   deviceManufacturer?.ToLower().Contains("sunmi") == true ||
                                   deviceManufacturer?.ToLower().Contains("ch") == true;

                if (isCH10Device)
                {
                    _logger.LogInformation($"CH10Strategy: Detected CH10-compatible device - Model: {deviceModel}, Manufacturer: {deviceManufacturer}");
                }
                else
                {
                    _logger.LogDebug($"CH10Strategy: Not a CH10 device - Model: {deviceModel}, Manufacturer: {deviceManufacturer}");
                }

                // For testing purposes, always return true to test CH10 service availability
                // In production, you would want to check specific device properties
                return true;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "CH10Strategy: Error checking device compatibility");
                return false;
            }
        }

        public async Task<PrinterInitializationResult> InitializeAsync()
        {
            var result = new PrinterInitializationResult { StrategyUsed = StrategyName };
            
            try
            {
                _logger.LogInformation("CH10Strategy: CH10 SDK is currently unavailable - skipping initialization");
                
                // CH10 SDK is unavailable - return failure to trigger fallback
                result.ErrorMessage = "CH10 SDK reference has been removed";
                result.Success = false;
                return result;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "CH10Strategy: Exception during initialization");
                result.ErrorMessage = $"CH10 initialization exception: {ex.Message}";
                result.Success = false;
                return result;
            }
        }

        public IPrinterService CreatePrinterService()
        {
            _logger.LogInformation("CH10Strategy: CH10 SDK unavailable - returning stub implementation");
            return new CH10PrinterService(); // Return stub service
        }

        public PosDeviceType GetDeviceType()
        {
            return PosDeviceType.CH10;
        }
    }
}