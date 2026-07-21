using Android.Content;
using Android.Graphics;
using Android.OS;
using CH10PosSdk.Additions;
using Recieptservice.Com.Recieptservice;
using wms_android.Interfaces;
using SysException = System.Exception;

namespace wms_android.Services
{
    public class CH10PrinterService : Java.Lang.Object, ICH10PrinterService, IServiceConnection
    {
        private IPrinterInterface? _printerInterface;
        private bool _isBound = false;
        private readonly TaskCompletionSource<bool> _bindTcs = new TaskCompletionSource<bool>();

        public CH10PrinterService()
        {
            System.Diagnostics.Debug.WriteLine("CH10PrinterService: Created");
        }

        public async Task<bool> InitializeAsync()
        {
            try
            {
                if (_isBound && _printerInterface != null)
                    return true;

                var context = Android.App.Application.Context;
                var intent = CH10PrinterHelper.CreatePrinterServiceIntent();
                bool bindResult = context.BindService(intent, this, Bind.AutoCreate);

                if (!bindResult)
                {
                    System.Diagnostics.Debug.WriteLine("CH10PrinterService: BindService returned false");
                    return false;
                }

                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(5));
                cts.Token.Register(() => _bindTcs.TrySetResult(false));

                return await _bindTcs.Task;
            }
            catch (SysException ex)
            {
                System.Diagnostics.Debug.WriteLine($"CH10PrinterService.InitializeAsync: {ex.Message}");
                return false;
            }
        }

        public void OnServiceConnected(ComponentName? name, IBinder? service)
        {
            try
            {
                _printerInterface = IPrinterInterface.Stub.AsInterface(service);
                _isBound = _printerInterface != null;
                System.Diagnostics.Debug.WriteLine($"CH10PrinterService: Service connected, bound={_isBound}");
                _bindTcs.TrySetResult(_isBound);
            }
            catch (SysException ex)
            {
                System.Diagnostics.Debug.WriteLine($"CH10PrinterService.OnServiceConnected: {ex.Message}");
                _bindTcs.TrySetResult(false);
            }
        }

        public void OnServiceDisconnected(ComponentName? name)
        {
            System.Diagnostics.Debug.WriteLine("CH10PrinterService: Service disconnected");
            _printerInterface = null;
            _isBound = false;
        }

        public async Task<bool> PrintReceiptAsync(string receiptContent)
        {
            if (!await EnsureConnected()) return false;
            try
            {
                _printerInterface!.BeginWork();
                _printerInterface.PrintText(receiptContent);
                _printerInterface.NextLine(3);
                _printerInterface.EndWork();
                return true;
            }
            catch (SysException ex)
            {
                System.Diagnostics.Debug.WriteLine($"CH10PrinterService.PrintReceiptAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> PrintTextAsync(string text)
        {
            if (!await EnsureConnected()) return false;
            try
            {
                _printerInterface!.BeginWork();
                _printerInterface.PrintText(text);
                _printerInterface.NextLine(3);
                _printerInterface.EndWork();
                return true;
            }
            catch (SysException ex)
            {
                System.Diagnostics.Debug.WriteLine($"CH10PrinterService.PrintTextAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> PrintBitmapAsync(Bitmap bitmap)
        {
            if (!await EnsureConnected()) return false;
            try
            {
                _printerInterface!.PrintBitmap(bitmap);
                return true;
            }
            catch (SysException ex)
            {
                System.Diagnostics.Debug.WriteLine($"CH10PrinterService.PrintBitmapAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> PrintQRCodeAsync(string data, int moduleSize = 4, int errorLevel = 3)
        {
            if (!await EnsureConnected()) return false;
            try
            {
                _printerInterface!.BeginWork();
                _printerInterface.PrintQRCode(data, moduleSize, errorLevel);
                _printerInterface.NextLine(3);
                _printerInterface.EndWork();
                return true;
            }
            catch (SysException ex)
            {
                System.Diagnostics.Debug.WriteLine($"CH10PrinterService.PrintQRCodeAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> PrintBarcodeAsync(string data, int type = 3, int height = 80, int width = 2)
        {
            if (!await EnsureConnected()) return false;
            try
            {
                _printerInterface!.Print128BarCode(data, type, height, width);
                return true;
            }
            catch (SysException ex)
            {
                System.Diagnostics.Debug.WriteLine($"CH10PrinterService.PrintBarcodeAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SetTextBoldAsync(bool bold)
        {
            if (!await EnsureConnected()) return false;
            try
            {
                _printerInterface!.SetTextBold(bold);
                return true;
            }
            catch (SysException ex)
            {
                System.Diagnostics.Debug.WriteLine($"CH10PrinterService.SetTextBoldAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SetTextSizeAsync(float textSize)
        {
            if (!await EnsureConnected()) return false;
            try
            {
                _printerInterface!.SetTextSize(textSize);
                return true;
            }
            catch (SysException ex)
            {
                System.Diagnostics.Debug.WriteLine($"CH10PrinterService.SetTextSizeAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SetAlignmentAsync(int alignment)
        {
            if (!await EnsureConnected()) return false;
            try
            {
                _printerInterface!.SetAlignment(alignment);
                return true;
            }
            catch (SysException ex)
            {
                System.Diagnostics.Debug.WriteLine($"CH10PrinterService.SetAlignmentAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> NextLineAsync(int lines = 1)
        {
            if (!await EnsureConnected()) return false;
            try
            {
                _printerInterface!.NextLine(lines);
                return true;
            }
            catch (SysException ex)
            {
                System.Diagnostics.Debug.WriteLine($"CH10PrinterService.NextLineAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> PrintTableTextAsync(string[] text, int[] weight, int[] alignment)
        {
            if (!await EnsureConnected()) return false;
            try
            {
                _printerInterface!.PrintTableText(text, weight, alignment);
                return true;
            }
            catch (SysException ex)
            {
                System.Diagnostics.Debug.WriteLine($"CH10PrinterService.PrintTableTextAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> BeginWorkAsync()
        {
            if (!await EnsureConnected()) return false;
            try
            {
                _printerInterface!.BeginWork();
                return true;
            }
            catch (SysException ex)
            {
                System.Diagnostics.Debug.WriteLine($"CH10PrinterService.BeginWorkAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> EndWorkAsync()
        {
            if (!await EnsureConnected()) return false;
            try
            {
                _printerInterface!.EndWork();
                return true;
            }
            catch (SysException ex)
            {
                System.Diagnostics.Debug.WriteLine($"CH10PrinterService.EndWorkAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<string> GetServiceVersionAsync()
        {
            if (!await EnsureConnected()) return "Not connected";
            try
            {
                return _printerInterface!.ServiceVersion ?? "Unknown";
            }
            catch (SysException ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public async Task<bool> GetScannerStatusAsync()
        {
            return _isBound && _printerInterface != null;
        }

        public async Task<string> GetPrinterStatusAsync()
        {
            return _isBound && _printerInterface != null ? "Ready" : "Not connected";
        }

        public async Task<bool> InitializePrinterAsync()
        {
            return await InitializeAsync();
        }

        public Task<bool> StartPrintJobAsync()
        {
            return Task.FromResult(true);
        }

        public void ClosePrinter()
        {
            Dispose(true);
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (_isBound)
                {
                    Android.App.Application.Context.UnbindService(this);
                    _isBound = false;
                    _printerInterface = null;
                }
            }
            catch (SysException ex)
            {
                System.Diagnostics.Debug.WriteLine($"CH10PrinterService.Dispose: {ex.Message}");
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        private async Task<bool> EnsureConnected()
        {
            if (_isBound && _printerInterface != null)
                return true;
            return await InitializeAsync();
        }
    }
}
