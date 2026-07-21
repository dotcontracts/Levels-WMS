using System;
using System.Threading.Tasks;
using Android.Graphics;
using wms_android.Interfaces;
using wms_android.Utils;
using Microsoft.Extensions.Logging;
using Com.Vanstone.Trans.Api;
using wms_android.shared.Models;

namespace wms_android.Services
{
    public class VanstonePrinterService : IPrinterService
    {
        private readonly IVanstonePosApi _printerHelper;
        private bool _isInitialized;
        private readonly ILogger<VanstonePrinterService> _logger;

        public VanstonePrinterService(ILogger<VanstonePrinterService> logger)
        {
            _logger = logger;
            _printerHelper = (IVanstonePosApi)VanstonePosApiHelper.Instance;
        }

        public async Task<bool> InitializePrinterAsync()
        {
            try
            {
                if (_isInitialized)
                {
                    return true;
                }

                var result = await Task.Run(() => _printerHelper.PrintInit(2, 2, 0, 0));
                _isInitialized = result == 0;
                
                if (_isInitialized)
                {
                    _logger.LogInformation("Vanstone printer initialized successfully");
                }
                else
                {
                    _logger.LogError("Failed to initialize Vanstone printer");
                }

                return _isInitialized;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing Vanstone printer");
                return false;
            }
        }

        public async Task<bool> PrintTextAsync(string text)
        {
            return await PrintTextAsync(text, 24, false);
        }

        public async Task<bool> PrintTextAsync(string text, int fontSize, bool isBold)
        {
            try
            {
                if (!_isInitialized)
                {
                    var initialized = await InitializePrinterAsync();
                    if (!initialized)
                    {
                        return false;
                    }
                }

                // Set font
                var fontResult = await Task.Run(() => _printerHelper.PrintSetFont((sbyte)fontSize, (sbyte)fontSize, (sbyte)(isBold ? 1 : 0)));
                if (fontResult != 0)
                {
                    _logger.LogWarning($"PrintSetFont returned {fontResult}, continuing anyway");
                }

                // Split into lines and print each one individually.
                // PrnStr_Api has a buffer limit and fails on large strings sent at once.
                var lines = text.Split('\n');
                bool anyFailed = false;
                foreach (var line in lines)
                {
                    var lineResult = await Task.Run(() => _printerHelper.PrintStr(line + "\n"));
                    if (lineResult != 0)
                    {
                        _logger.LogWarning($"PrintStr returned {lineResult} for line: {(line.Length > 30 ? line[..30] : line)}");
                        anyFailed = true;
                    }
                }

                if (anyFailed)
                {
                    _logger.LogError("One or more lines failed to print");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing text");
                return false;
            }
        }

        public async Task<bool> PrintQRCodeAsync(string content, int width, int height)
        {
            try
            {
                if (!_isInitialized)
                {
                    var initialized = await InitializePrinterAsync();
                    if (!initialized)
                    {
                        return false;
                    }
                }

                // Print QR code with specified dimensions (prepare for printing, but don't start the print job yet)
                var result = await Task.Run(() => _printerHelper.PrintQrCode(content, width, height));
                if (result != 0)
                {
                    _logger.LogError($"Failed to print QR code: {result}");
                    return false;
                }

                // Don't call PrintStart here - let the caller decide when to start printing
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing QR code");
                return false;
            }
        }

        public async Task<bool> PrintImageAsync(byte[] imageData, int width)
        {
            try
            {
                if (!await InitializePrinterAsync())
                {
                    return false;
                }

                using var bitmap = await BitmapFactory.DecodeByteArrayAsync(imageData, 0, imageData.Length);
                if (bitmap == null)
                {
                    _logger.LogError("Failed to decode image data");
                    return false;
                }

                // Scale bitmap to printer width while maintaining aspect ratio
                float scale = (float)width / bitmap.Width;
                int height = (int)(bitmap.Height * scale);

                using var scaledBitmap = Bitmap.CreateScaledBitmap(bitmap, width, height, true);
                
                // Convert bitmap to bytes
                using var stream = new System.IO.MemoryStream();
                await scaledBitmap.CompressAsync(Bitmap.CompressFormat.Png, 100, stream);
                var bitmapBytes = stream.ToArray();

                _printerHelper.PrintBitmap(bitmapBytes, width, height);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing image on Vanstone printer");
                return false;
            }
        }

        public async Task<bool> FeedPaperAsync(int lines)
        {
            try
            {
                if (!await InitializePrinterAsync())
                {
                    return false;
                }

                _printerHelper.PrintFeedPaper(lines);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error feeding paper on Vanstone printer");
                return false;
            }
        }

        public async Task<bool> CutPaperAsync()
        {
            try
            {
                if (!_isInitialized)
                {
                    return false;
                }

                // Feed paper before cutting
                var feedResult = await Task.Run(() => _printerHelper.PrintFeedPaper(3));
                if (feedResult != 0)
                {
                    _logger.LogError($"Failed to feed paper: {feedResult}");
                    return false;
                }

                // Start printing to execute the cut
                var startResult = await Task.Run(() => _printerHelper.PrintStart());
                return startResult == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cutting paper");
                return false;
            }
        }

        public async Task<string> GetPrinterStatusAsync()
        {
            try
            {
                if (!_isInitialized)
                {
                    return PrinterStatus.Offline.ToString();
                }

                var status = await Task.Run(() => _printerHelper.GetPrinterStatus());
                return status switch
                {
                    0 => PrinterStatus.Ready.ToString(),
                    1 => PrinterStatus.OutOfPaper.ToString(),
                    2 => PrinterStatus.Error.ToString(),
                    3 => PrinterStatus.Busy.ToString(),
                    _ => PrinterStatus.Unknown.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting printer status");
                return PrinterStatus.Error.ToString();
            }
        }

        public async Task<bool> StartPrintJobAsync()
        {
            try
            {
                if (!_isInitialized)
                {
                    var initialized = await InitializePrinterAsync();
                    if (!initialized)
                    {
                        return false;
                    }
                }

                var result = await Task.Run(() => _printerHelper.PrintStart());
                return result == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting print job");
                return false;
            }
        }

        public void ClosePrinter()
        {
            try
            {
                if (_isInitialized)
                {
                    _printerHelper?.Dispose();
                    _isInitialized = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing Vanstone printer");
            }
        }
    }
} 