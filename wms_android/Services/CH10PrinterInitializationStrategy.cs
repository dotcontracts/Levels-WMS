using Android.Content;
using Android.OS;
using CH10PosSdk.Additions;
using Microsoft.Extensions.Logging;
using Recieptservice.Com.Recieptservice;


namespace wms_android.Services
{
    public class CH10PrinterInitializationStrategy : IPrinterInitializationStrategy
    {
        private readonly ILogger _logger;

        public string StrategyName => "CH10 Printer with AIDL Service";
        public int Priority => 1;

        public CH10PrinterInitializationStrategy(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<bool> CanHandleDeviceAsync()
        {
            try
            {
                var deviceModel = Android.OS.Build.Model ?? string.Empty;
                var deviceManufacturer = Android.OS.Build.Manufacturer ?? string.Empty;

                _logger.LogDebug("CH10Strategy: Device Model: {Model}, Manufacturer: {Manufacturer}",
                    deviceModel, deviceManufacturer);

                bool isCH10Device = deviceModel.Contains("CH10", StringComparison.OrdinalIgnoreCase) ||
                                    deviceModel.Contains("H10", StringComparison.OrdinalIgnoreCase) ||
                                    deviceManufacturer.Contains("sunmi", StringComparison.OrdinalIgnoreCase) ||
                                    deviceManufacturer.Contains("CH", StringComparison.OrdinalIgnoreCase);

                if (isCH10Device)
                    _logger.LogInformation("CH10Strategy: Detected CH10-compatible device");
                else
                    _logger.LogDebug("CH10Strategy: Not a CH10 device - trying anyway");

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
                var context = Android.App.Application.Context;
                var intent = CH10PrinterHelper.CreatePrinterServiceIntent();

                var tcs = new TaskCompletionSource<bool>();
                var connection = new OneTimeServiceConnection(tcs, _logger);

                bool bindResult = context.BindService(intent, connection, Bind.AutoCreate);
                if (!bindResult)
                {
                    _logger.LogWarning("CH10Strategy: BindService returned false - printer service not available");
                    result.ErrorMessage = "Printer service not found on this device";
                    result.Success = false;
                    return result;
                }

                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(5));
                cts.Token.Register(() => tcs.TrySetResult(false));

                bool connected = await tcs.Task;

                context.UnbindService(connection);

                result.Success = connected;
                if (!connected)
                    result.ErrorMessage = "Timed out waiting for printer service connection";
                else
                    _logger.LogInformation("CH10Strategy: Printer service bound successfully");

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

        private class OneTimeServiceConnection : Java.Lang.Object, Android.Content.IServiceConnection
        {
            private readonly TaskCompletionSource<bool> _tcs;
            private readonly ILogger _logger;

            public OneTimeServiceConnection(TaskCompletionSource<bool> tcs, ILogger logger)
            {
                _tcs = tcs;
                _logger = logger;
            }

            public void OnServiceConnected(ComponentName? name, IBinder? service)
            {
                try
                {
                    var printerInterface = IPrinterInterface.Stub.AsInterface(service);
                    _tcs.TrySetResult(printerInterface != null);
                }
                catch (System.Exception ex)
                {
                    _logger.LogError(ex, "CH10Strategy: Error in OnServiceConnected");
                    _tcs.TrySetResult(false);
                }
            }

            public void OnServiceDisconnected(ComponentName? name)
            {
                _logger.LogDebug("CH10Strategy: Service disconnected during probe");
            }
        }
    }
}
