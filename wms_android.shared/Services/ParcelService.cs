using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using wms_android.shared.Models;
using wms_android.shared.Interfaces;
using wms_android.shared.DTOs;
using Microsoft.Extensions.Configuration;
using System.Threading;

namespace wms_android.shared.Services
{
    public class ParcelService : IParcelService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public ParcelService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "https://wmsandroidapi-w74du.ondigitalocean.app/";
            
            // Configure HttpClient
            _httpClient.BaseAddress = new Uri(_baseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(30); // Set default timeout
            
            // Add default headers
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "WMS-Android-App");
            
            System.Diagnostics.Debug.WriteLine($"ParcelService initialized with API URL: {_baseUrl}");
        }

        public async Task<IEnumerable<Parcel>> GetParcelsAsync()
        {
            var response = await _httpClient.GetAsync("/api/parcels");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<Parcel>>(content);
        }

        public async Task<Parcel> GetParcelByIdAsync(Guid id)
        {
            var response = await _httpClient.GetAsync($"/api/parcels/{id}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Parcel>(content);
        }

        public async Task<Parcel> CreateParcelAsync(Parcel parcel)
        {
            // Maximum number of retries
            const int maxRetries = 3;
            
            // Initial delay in milliseconds (500ms)
            int retryDelay = 500;
            
            // Track retry attempts
            int retryAttempt = 0;
            
            while (true)
            {
                try 
                {
                    // Ensure the waybill number is set before sending to the server
                    if (string.IsNullOrEmpty(parcel.WaybillNumber))
                    {
                        parcel.WaybillNumber = await GenerateWaybillNumberAsync();
                        parcel.QRCode = parcel.WaybillNumber;
                    }
                    
                    // Ensure CreatedAt is current UTC time before sending to the API
                    System.Diagnostics.Debug.WriteLine($"[ParcelService MAUI Client] Method Entry. parcel.CreatedAt: {parcel.CreatedAt}, Kind: {parcel.CreatedAt.Kind}");
                    parcel.CreatedAt = DateTime.UtcNow;
                    System.Diagnostics.Debug.WriteLine($"[ParcelService MAUI Client] After override, parcel.CreatedAt: {parcel.CreatedAt}, Kind: {parcel.CreatedAt.Kind}");
                    
                    // Log the parcel data before sending
                    System.Diagnostics.Debug.WriteLine($"[ParcelService MAUI Client] Sending parcel data: {JsonSerializer.Serialize(parcel)}");

                    var json = JsonSerializer.Serialize(parcel);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    
                    // Use a timeout that increases with each retry to give more time on problematic connections
                    var timeoutMs = 10000 + (retryAttempt * 5000); // 10s base + 5s per retry
                    var cancellationToken = new CancellationTokenSource(timeoutMs).Token;
                    
                    // Reset the HttpClient's connection before each retry
                    if (retryAttempt > 0)
                    {
                        _httpClient.DefaultRequestHeaders.Connection.Clear();
                        _httpClient.DefaultRequestHeaders.Connection.Add("keep-alive");
                    }
                    
                    var response = await _httpClient.PostAsync("/api/parcels", content, cancellationToken);
                
                    var responseContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"API Response: {response.StatusCode}, Content: {responseContent}");
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException($"Error saving parcel: {response.StatusCode}, Content: {responseContent}");
                    }
                    
                    // Configure JsonSerializerOptions to handle camelCase AND preserved references
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve
                    };
                    
                    var savedParcel = JsonSerializer.Deserialize<Parcel>(responseContent, options);
                    
                    // Validate the deserialized object
                    if (savedParcel == null)
                    {
                        throw new Exception("Failed to deserialize parcel data from API response");
                    }

                    // Always use our original waybill number and QR code
                    // This ensures server-generated values don't override our format
                    savedParcel.WaybillNumber = parcel.WaybillNumber;
                    savedParcel.QRCode = parcel.WaybillNumber;
                    
                    // Log the final parcel data - use the same options as deserialization to handle cycles
                    System.Diagnostics.Debug.WriteLine($"Final parcel data: {JsonSerializer.Serialize(savedParcel, options)}");
                    
                    return savedParcel;
                }
                catch (Exception ex)
                {
                    retryAttempt++;
                    
                    bool isTransientError = IsTransientError(ex) || ex.Message.Contains("Socket closed");
                    bool shouldRetry = isTransientError && retryAttempt < maxRetries;
                    
                    System.Diagnostics.Debug.WriteLine($"CreateParcelAsync error: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    }
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    
                    if (!shouldRetry)
                    {
                        System.Diagnostics.Debug.WriteLine($"Not retrying: isTransientError={isTransientError}, retryAttempt={retryAttempt}");
                        throw;
                    }
                    
                    // Calculate exponential backoff delay
                    retryDelay = Math.Min(5000, retryDelay * 2); // Cap at 5 seconds
                    System.Diagnostics.Debug.WriteLine($"Retrying in {retryDelay}ms (attempt {retryAttempt} of {maxRetries})");
                    await Task.Delay(retryDelay);
                }
            }
        }

        public async Task<Parcel> UpdateParcelAsync(Parcel parcel)
        {
            try
            {
                var json = JsonSerializer.Serialize(parcel);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PutAsync($"/api/parcels/{parcel.Id}", content);
                response.EnsureSuccessStatusCode();
                
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Parcel>(responseContent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateParcelAsync error: {ex.Message}");
                throw;
            }
        }
        
        private bool IsTransientError(Exception ex)
        {
            // Check for common transient errors
            if (ex is HttpRequestException httpEx)
            {
                // Consider 5xx errors as transient
                if (httpEx.StatusCode.HasValue && (int)httpEx.StatusCode.Value >= 500)
                    return true;
                
                // Consider connection errors as transient
                if (httpEx.Message.Contains("connection") || 
                    httpEx.Message.Contains("timeout") || 
                    httpEx.Message.Contains("network") ||
                    httpEx.Message.Contains("Socket closed"))
                    return true;
            }
            
            // Check for socket-related errors
            if (ex.Message.Contains("Socket closed") || 
                ex.Message.Contains("connection") || 
                ex.Message.Contains("network") ||
                ex.Message.Contains("timeout"))
                return true;
            
            return false;
        }

        public async Task<string> GenerateWaybillNumberAsync()
        {
            try
            {
                // Generate a waybill number locally with the format "NID" + 5 random alphanumeric characters
                System.Diagnostics.Debug.WriteLine("Generating local waybill number...");
                
                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                var random = new Random();
                var randomString = new string(Enumerable.Repeat(chars, 5)
                    .Select(s => s[random.Next(s.Length)]).ToArray());
                
                var waybillNumber = $"NID{randomString}";
                
                System.Diagnostics.Debug.WriteLine($"Generated waybill number: {waybillNumber}");
                return waybillNumber;
                
                /* Commented out API call
                System.Diagnostics.Debug.WriteLine("Calling API to generate waybill number...");
                var response = await _httpClient.GetAsync("/api/parcels/waybill/generate");
                var content = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"API response: Status={response.StatusCode}, Content={content}");

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Failed to generate waybill number. Status: {response.StatusCode}, Content: {content}");
                }

                // Parse the response - remove any quotes that might be present
                var waybillNumber = content.Trim('"');
                System.Diagnostics.Debug.WriteLine($"Generated waybill number: {waybillNumber}");
                return waybillNumber;
                */
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GenerateWaybillNumberAsync: {ex}");
                throw;
            }
        }

        public async Task FinalizeWaybillAsync(Guid parcelId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Starting FinalizeWaybillAsync for parcel ID: {parcelId}");
                var response = await _httpClient.PostAsync($"/api/parcels/{parcelId}/finalize", null);
                var content = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Finalize API Response: {response.StatusCode}, Content: {content}");

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Failed to finalize waybill. Status: {response.StatusCode}, Content: {content}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in FinalizeWaybillAsync: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task FinalizeWaybillAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Starting parameterless FinalizeWaybillAsync - this is a compatibility wrapper");
                
                // Since no parcel ID is provided, we can't finalize a specific waybill
                // This is provided for backward compatibility only
                // Log a warning but don't throw an exception
                System.Diagnostics.Debug.WriteLine("WARNING: Calling FinalizeWaybillAsync without a parcel ID has no effect in the shared implementation");
                
                // Return completed task
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in parameterless FinalizeWaybillAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Parcel>> CreateCartParcels(List<Parcel> parcels)
        {
            // Maximum number of retries
            const int maxRetries = 3;
            
            // Initial delay in milliseconds (500ms)
            int retryDelay = 500;
            
            // Track retry attempts
            int retryAttempt = 0;
            
            while (true)
            {
                try
            {
                // Ensure all parcels have waybill numbers before sending
                for (int i = 0; i < parcels.Count; i++)
                {
                    if (string.IsNullOrEmpty(parcels[i].WaybillNumber))
                    {
                        parcels[i].WaybillNumber = await GenerateWaybillNumberAsync();
                        parcels[i].QRCode = parcels[i].WaybillNumber;
                    }
                }
                
                // Log the parcels data before sending
                System.Diagnostics.Debug.WriteLine($"Sending batch parcels data, count: {parcels.Count}");
                
                var json = JsonSerializer.Serialize(parcels);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    
                    // Use a timeout that increases with each retry to give more time on problematic connections
                    var timeoutMs = 20000 + (retryAttempt * 10000); // 20s base + 10s per retry for batch operations
                    var cancellationToken = new CancellationTokenSource(timeoutMs).Token;
                    
                    var response = await _httpClient.PostAsync("/api/parcels/batch", content, cancellationToken);
                
                var responseContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Batch API Response: {response.StatusCode}, Content length: {responseContent.Length}");
                
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Error saving batch parcels: {response.StatusCode}");
                }
                
                // Configure JsonSerializerOptions to handle camelCase AND preserved references
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve
                };
                
                var savedParcels = JsonSerializer.Deserialize<List<Parcel>>(responseContent, options);
                
                    if (savedParcels == null || savedParcels.Count == 0)
                    {
                        throw new Exception("Failed to deserialize parcel data or empty result returned");
                    }
                    
                    // Make sure all saved parcels have their waybill numbers set
                    for (int i = 0; i < savedParcels.Count; i++)
                    {
                        if (string.IsNullOrEmpty(savedParcels[i].WaybillNumber) && i < parcels.Count)
                {
                    savedParcels[i].WaybillNumber = parcels[i].WaybillNumber;
                    savedParcels[i].QRCode = parcels[i].WaybillNumber;
                }
                }
                
                    System.Diagnostics.Debug.WriteLine($"Successfully processed {savedParcels.Count} parcels");
                return savedParcels;
            }
            catch (Exception ex)
            {
                    retryAttempt++;
                    
                    bool isTransientError = IsTransientError(ex);
                    bool shouldRetry = isTransientError && retryAttempt < maxRetries;
                    
                System.Diagnostics.Debug.WriteLine($"CreateCartParcels error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    
                    if (shouldRetry)
                    {
                        System.Diagnostics.Debug.WriteLine($"Retrying batch API call (attempt {retryAttempt}/{maxRetries}) after {retryDelay}ms due to transient error: {ex.Message}");
                        
                        // Wait before retrying with exponential backoff
                        await Task.Delay(retryDelay);
                        
                        // Exponential backoff: double the delay for next retry
                        retryDelay *= 2;
                    }
                    else
                    {
                        // If we've exhausted our retries or it's not a transient error, rethrow
                        System.Diagnostics.Debug.WriteLine($"Not retrying batch: isTransientError={isTransientError}, retryAttempt={retryAttempt}");
                throw;
                    }
                }
            }
        }

        public async Task<decimal> GetTotalSalesForDateAsync(DateTime date)
        {
            var response = await _httpClient.GetAsync($"/api/parcels/sales/{date:yyyy-MM-dd}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return decimal.Parse(content);
        }

        public async Task<IEnumerable<Parcel>> GetPendingOrdersAsync(DateTime? dateFilter = null)
        {
            try
        {
                var url = "/api/parcels/pending";
                if (dateFilter.HasValue)
                {
                    // Format the date as YYYY-MM-DD and add as a query parameter
                    url = $"{url}?created_at={dateFilter.Value:yyyy-MM-dd}";
                }

                System.Diagnostics.Debug.WriteLine($"[ParcelService MAUI Client] Calling API for pending parcels: {_baseUrl}{url}");
                var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
                
                // Configure JsonSerializerOptions to handle camelCase AND preserved references
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve
                };

                return JsonSerializer.Deserialize<IEnumerable<Parcel>>(content, options);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ParcelService MAUI Client] Error fetching pending parcels: {ex.Message}");
                throw;
            }
        }

        public async Task<Parcel> GetParcelByWaybillNumberAsync(string waybillNumber)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[GetParcelByWaybillNumberAsync] Searching for parcel with WaybillNumber: {waybillNumber}");
                
                var response = await _httpClient.GetAsync($"/api/parcels/waybill/{waybillNumber}");

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    System.Diagnostics.Debug.WriteLine($"Parcel with Waybill {waybillNumber} not found (404).");
                    return null;
                }

                response.EnsureSuccessStatusCode(); 
                var content = await response.Content.ReadAsStringAsync();
                
                 if (string.IsNullOrWhiteSpace(content))
                 {
                     System.Diagnostics.Debug.WriteLine($"API returned success status but empty content for Waybill {waybillNumber}.");
                    return null;
                 }

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve
                    };
                
                    var parcel = JsonSerializer.Deserialize<Parcel>(content, options);
                    System.Diagnostics.Debug.WriteLine($"Successfully deserialized parcel for Waybill {waybillNumber}. ID: {parcel?.Id}");
                    return parcel;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetParcelByWaybillNumberAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<Parcel>> GetParcelsByQRCodeAsync(string qrCode)
        {
            string responseContent = string.Empty;
            try
            {
                System.Diagnostics.Debug.WriteLine($"Connecting to API at {_baseUrl}");
                System.Diagnostics.Debug.WriteLine($"Fetching parcels with QR code: {qrCode}");
                
                // API endpoint for fetching parcels by QR code (expected to return a list)
                var response = await _httpClient.GetAsync($"/api/parcels/qr/{qrCode}");
                
                responseContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"QR API Response: {response.StatusCode}, Content: {responseContent}");

                if (!response.IsSuccessStatusCode)
                {
                    // If not found, return empty list, otherwise throw for actual errors.
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        System.Diagnostics.Debug.WriteLine($"No parcels found for QR code {qrCode} (404). Returning empty list.");
                        return Enumerable.Empty<Parcel>();
                    }
                    throw new HttpRequestException($"API error fetching parcels by QR code: {response.StatusCode}, Content: {responseContent}");
                }
                
                if (string.IsNullOrWhiteSpace(responseContent))
                {
                    System.Diagnostics.Debug.WriteLine($"API returned success for QR code {qrCode} but content was empty. Returning empty list.");
                    return Enumerable.Empty<Parcel>();
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    ReferenceHandler = ReferenceHandler.IgnoreCycles,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };
                
                // Try to deserialize into a list of parcels
                System.Diagnostics.Debug.WriteLine($"Attempting to deserialize QR response for {qrCode}. Content preview: {responseContent.Substring(0, Math.Min(200, responseContent.Length))}...");
                
                System.Diagnostics.Debug.WriteLine($"Raw API response for debugging: {responseContent}");
                
                IEnumerable<Parcel> parcels;
                try
                {
                    // First try to deserialize as a list
                    parcels = JsonSerializer.Deserialize<List<Parcel>>(responseContent, options);
                    System.Diagnostics.Debug.WriteLine($"List deserialization succeeded for QR {qrCode}");
                }
                catch (JsonException ex1)
                {
                    System.Diagnostics.Debug.WriteLine($"List deserialization failed: {ex1.Message}");
                    // If that fails, try to deserialize as an array
                    try
                    {
                        parcels = JsonSerializer.Deserialize<Parcel[]>(responseContent, options);
                        System.Diagnostics.Debug.WriteLine($"Array deserialization succeeded for QR {qrCode}");
                    }
                    catch (JsonException ex2)
                    {
                        // Log the full response for debugging
                        System.Diagnostics.Debug.WriteLine($"Both List and Array deserialization failed for QR {qrCode}");
                        System.Diagnostics.Debug.WriteLine($"List error: {ex1.Message}");
                        System.Diagnostics.Debug.WriteLine($"Array error: {ex2.Message}");
                        System.Diagnostics.Debug.WriteLine($"Full response content: {responseContent}");
                        throw new JsonException($"Failed to deserialize QR response for {qrCode}. List error: {ex1.Message}, Array error: {ex2.Message}");
                    }
                }
                
                if (parcels == null)
                {
                     System.Diagnostics.Debug.WriteLine($"Deserialization succeeded but returned null for QR {qrCode}. Content: {responseContent}. Returning empty list.");
                    return Enumerable.Empty<Parcel>();
                }
                
                System.Diagnostics.Debug.WriteLine($"Successfully fetched {parcels.Count()} parcels via QR code: {qrCode}");
                return parcels;
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"HTTP error in GetParcelsByQRCodeAsync: {ex.Message}");
                // Depending on requirements, you might want to return an empty list or rethrow.
                // For now, rethrowing to make issues visible.
                throw; 
            }
            catch (JsonException jsonEx)
            {
                System.Diagnostics.Debug.WriteLine($"JSON Deserialization error in GetParcelsByQRCodeAsync for QR {qrCode}: {jsonEx.Message}");
                if (!string.IsNullOrEmpty(responseContent))
                {
                    System.Diagnostics.Debug.WriteLine($"Raw API Response: {responseContent}");
                }
                
                // Instead of crashing, return empty list and log the issue
                System.Diagnostics.Debug.WriteLine($"Returning empty list due to JSON deserialization error for QR {qrCode}");
                return Enumerable.Empty<Parcel>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetParcelsByQRCodeAsync for QR {qrCode}: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        public async Task<int> GetParcelCountForDateAsync(DateTime date)
        {
            var response = await _httpClient.GetAsync($"/api/parcels/count/{date:yyyy-MM-dd}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return int.Parse(content);
        }

        public async Task DispatchParcelAsync(Parcel parcel)
        {
            var json = JsonSerializer.Serialize(parcel);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"/api/parcels/{parcel.Id}/dispatch", content);
            response.EnsureSuccessStatusCode();
        }

        public async Task UpdateParcelStatusAsync(Guid parcelId, ParcelStatus status)
        {
            var statusUpdate = new { Status = status };
            var json = JsonSerializer.Serialize(statusUpdate);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"/api/parcels/{parcelId}/status", content);
            response.EnsureSuccessStatusCode();
        }

        // Dashboard methods implementation
        public async Task<int> GetParcelCountAsync(DateTime date)
        {
            // Reuse the existing count method
            return await GetParcelCountForDateAsync(date);
        }
        
        public async Task<int> GetDeliveredParcelCountAsync(DateTime date)
        {
            var response = await _httpClient.GetAsync($"/api/parcels/delivered/count/{date:yyyy-MM-dd}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return int.Parse(content);
        }
        
        public async Task<double> GetTotalSalesAsync(DateTime date)
        {
            // Reuse the existing sales method and convert to double
            var total = await GetTotalSalesForDateAsync(date);
            return (double)total;
        }
        
        // Dictionary to track SMS notifications at the service level
        private static readonly HashSet<Guid> _smsNotificationsSent = new HashSet<Guid>();
        
        /// <summary>
        /// Checks if an SMS notification has already been sent for a parcel
        /// </summary>
        /// <param name="parcelId">The ID of the parcel to check</param>
        /// <returns>True if a notification has been sent, false otherwise</returns>
        public Task<bool> CheckSmsNotificationSentAsync(Guid parcelId)
        {
            bool notificationSent = _smsNotificationsSent.Contains(parcelId);
            System.Diagnostics.Debug.WriteLine($"Checking if SMS sent for parcel {parcelId}: {notificationSent}");
            return Task.FromResult(notificationSent);
        }
        
        /// <summary>
        /// Marks that an SMS notification has been sent for a parcel
        /// </summary>
        /// <param name="parcelId">The ID of the parcel to mark</param>
        public Task MarkSmsNotificationSentAsync(Guid parcelId)
        {
            _smsNotificationsSent.Add(parcelId);
            System.Diagnostics.Debug.WriteLine($"Marked SMS as sent for parcel {parcelId}");
            return Task.CompletedTask;
        }
        
        public async Task<IEnumerable<Parcel>> GetParcelsByUserAsync(int userId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[ParcelService MAUI Client] Fetching parcels for user: {userId}");
                
                var response = await _httpClient.GetAsync($"/api/parcels/user/{userId}");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve
                };
                
                var parcels = JsonSerializer.Deserialize<IEnumerable<Parcel>>(content, options);
                System.Diagnostics.Debug.WriteLine($"Successfully fetched {parcels?.Count() ?? 0} parcels for user {userId}");
                
                return parcels ?? Enumerable.Empty<Parcel>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching parcels for user {userId}: {ex.Message}");
                throw;
            }
        }
        
        public async Task<double> GetAmountOwedByUserAsync(int userId, DateTime date)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[ParcelService MAUI Client] Fetching amount owed for user: {userId} on date: {date:yyyy-MM-dd}");
                
                var response = await _httpClient.GetAsync($"/api/parcels/user/{userId}/amount-owed?date={date:yyyy-MM-dd}");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                
                return double.Parse(content);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching amount owed for user {userId}: {ex.Message}");
                throw;
            }
        }
        
        public async Task<double> GetCashInByUserAsync(int userId, DateTime date)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[ParcelService MAUI Client] Fetching cash-in for user: {userId} on date: {date:yyyy-MM-dd}");
                
                var response = await _httpClient.GetAsync($"/api/parcels/user/{userId}/cash-in?date={date:yyyy-MM-dd}");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                
                return double.Parse(content);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching cash-in for user {userId}: {ex.Message}");
                throw;
            }
        }
        
        public async Task<double> GetDailySalesByUserAsync(int userId, DateTime date)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[ParcelService MAUI Client] Fetching daily sales for user: {userId} on date: {date:yyyy-MM-dd}");
                
                var response = await _httpClient.GetAsync($"/api/parcels/user/{userId}/daily-sales?date={date:yyyy-MM-dd}");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                
                return double.Parse(content);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching daily sales for user {userId}: {ex.Message}");
                throw;
            }
        }
        
        public async Task<double> GetMonthlySalesByUserAsync(int userId, DateTime date)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[ParcelService MAUI Client] Fetching monthly sales for user: {userId} for month: {date:yyyy-MM}");
                
                var response = await _httpClient.GetAsync($"/api/parcels/user/{userId}/monthly-sales?date={date:yyyy-MM-dd}");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                
                return double.Parse(content);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching monthly sales for user {userId}: {ex.Message}");
                throw;
            }
        }

        // New dispatch-related methods
        public async Task<IEnumerable<Parcel>> GetParcelsReadyForDispatchAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[ParcelService MAUI Client] Fetching parcels ready for dispatch");
                
                var response = await _httpClient.GetAsync("/api/parcels/ready-for-dispatch");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };
                
                return JsonSerializer.Deserialize<IEnumerable<Parcel>>(content, options) ?? new List<Parcel>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching parcels ready for dispatch: {ex.Message}");
                // Return empty list as fallback
                return new List<Parcel>();
            }
        }

        public async Task<Dispatch> CreateDispatchAsync(Dispatch dispatch)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[ParcelService MAUI Client] Creating dispatch: {dispatch.DispatchCode}");
                System.Diagnostics.Debug.WriteLine($"[ParcelService MAUI Client] ParcelIds count: {dispatch.ParcelIds?.Count ?? 0}");
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                var jsonContent = JsonSerializer.Serialize(dispatch, options);
                System.Diagnostics.Debug.WriteLine($"[ParcelService MAUI Client] JSON content: {jsonContent}");
                
                var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("/api/dispatches/create", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"[ParcelService MAUI Client] API Error: {response.StatusCode} - {errorContent}");
                    throw new HttpRequestException($"API returned {response.StatusCode}: {errorContent}");
                }
                
                var responseContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[ParcelService MAUI Client] API Response: {responseContent}");
                
                return JsonSerializer.Deserialize<Dispatch>(responseContent, options);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating dispatch: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<Dispatch>> GetRecentDispatchesAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[ParcelService MAUI Client] Fetching recent dispatches");
                
                var response = await _httpClient.GetAsync("/api/dispatches/recent");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };
                
                return JsonSerializer.Deserialize<IEnumerable<Dispatch>>(content, options) ?? new List<Dispatch>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching recent dispatches: {ex.Message}");
                // Return empty list as fallback
                return new List<Dispatch>();
            }
        }

        public async Task<IEnumerable<string>> GetDestinationsAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[ParcelService MAUI Client] Fetching destinations");
                
                var response = await _httpClient.GetAsync("/api/parcels/destinations");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                return JsonSerializer.Deserialize<IEnumerable<string>>(content, options) ?? new List<string>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching destinations: {ex.Message}");
                // Return fallback destinations
                return new List<string> { "Nairobi", "Mombasa", "Kisumu", "Eldoret", "Nakuru" };
            }
        }

        public async Task<IEnumerable<Parcel>> GetAllParcelsAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[ParcelService MAUI Client] Fetching all parcels");
                
                // Use the existing GetParcelsAsync method
                return await GetParcelsAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching all parcels: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<Parcel>> GetParcelsForDispatchAsync(string? destination = null, 
            List<ParcelStatus>? statuses = null, DateTime? fromDate = null, DateTime? toDate = null, 
            string? createdByUsername = null)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[ParcelService] Fetching parcels for dispatch - Destination: {destination}");
                
                var parameters = new List<string>();
                
                if (!string.IsNullOrWhiteSpace(destination))
                    parameters.Add($"destination={Uri.EscapeDataString(destination)}");
                
                if (statuses != null && statuses.Any())
                {
                    var statusString = string.Join(",", statuses);
                    parameters.Add($"statuses={statusString}");
                }
                
                if (fromDate.HasValue)
                    parameters.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
                
                if (toDate.HasValue)
                    parameters.Add($"toDate={toDate.Value:yyyy-MM-dd}");
                
                if (!string.IsNullOrWhiteSpace(createdByUsername))
                    parameters.Add($"createdByUsername={Uri.EscapeDataString(createdByUsername)}");
                
                var queryString = string.Join("&", parameters);
                var response = await _httpClient.GetAsync($"/api/parcels/dispatch?{queryString}");
                
                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"API call failed with status: {response.StatusCode}");
                    // Fallback to getting all parcels and filtering client-side
                    return await GetParcelsForDispatchFallback(destination, statuses, fromDate, toDate, createdByUsername);
                }
                
                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    ReferenceHandler = ReferenceHandler.Preserve
                };
                
                var result = JsonSerializer.Deserialize<IEnumerable<Parcel>>(content, options);
                System.Diagnostics.Debug.WriteLine($"Successfully fetched {result?.Count()} parcels for dispatch");
                
                return result ?? new List<Parcel>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetParcelsForDispatchAsync: {ex.Message}");
                // Fallback to client-side filtering
                return await GetParcelsForDispatchFallback(destination, statuses, fromDate, toDate, createdByUsername);
            }
        }

        public async Task<PaginatedResponse<Parcel>> GetParcelsPagedAsync(ParcelQueryParams queryParams)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[ParcelService] Fetching paginated parcels - Page: {queryParams.Page}, Size: {queryParams.PageSize}");
                
                var queryString = BuildQueryString(queryParams);
                var response = await _httpClient.GetAsync($"/api/parcels/paged?{queryString}");
                
                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"API call failed with status: {response.StatusCode}");
                    // Fallback to client-side pagination for now
                    return await GetParcelsPagedFallback(queryParams);
                }
                
                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    ReferenceHandler = ReferenceHandler.Preserve
                };
                
                var result = JsonSerializer.Deserialize<PaginatedResponse<Parcel>>(content, options);
                System.Diagnostics.Debug.WriteLine($"Successfully fetched {result?.Data?.Count()} parcels from page {queryParams.Page}");
                
                return result ?? new PaginatedResponse<Parcel>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetParcelsPagedAsync: {ex.Message}");
                // Fallback to client-side pagination
                return await GetParcelsPagedFallback(queryParams);
            }
        }

        public async Task<IEnumerable<string>> GetUniqueDestinationsAsync(ParcelStatus? status = null)
        {
            try
            {
                var statusParam = status.HasValue ? $"?status={status}" : "";
                var response = await _httpClient.GetAsync($"/api/parcels/destinations{statusParam}");
                
                if (!response.IsSuccessStatusCode)
                {
                    // Fallback to existing method
                    return await GetDestinationsAsync();
                }
                
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<IEnumerable<string>>(content) ?? new List<string>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting unique destinations: {ex.Message}");
                return await GetDestinationsAsync();
            }
        }

        public async Task<IEnumerable<string>> GetUniqueClerksByDestinationAsync(string destination, ParcelStatus? status = null)
        {
            try
            {
                var statusParam = status.HasValue ? $"&status={status}" : "";
                var response = await _httpClient.GetAsync($"/api/parcels/clerks?destination={Uri.EscapeDataString(destination)}{statusParam}");
                
                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"API call failed, using fallback for clerks by destination");
                    return await GetClerksByDestinationFallback(destination, status);
                }
                
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<IEnumerable<string>>(content) ?? new List<string>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting clerks by destination: {ex.Message}");
                return await GetClerksByDestinationFallback(destination, status);
            }
        }

        public async Task<PaginatedResponse<Parcel>> GetParcelsPagedByUserAsync(int userId, ParcelQueryParams queryParams)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[ParcelService] Fetching paginated parcels for user {userId} - Page: {queryParams.Page}, Size: {queryParams.PageSize}");
                
                var queryString = BuildQueryString(queryParams);
                var response = await _httpClient.GetAsync($"/api/parcels/user/{userId}/paged?{queryString}");
                
                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"API call failed with status: {response.StatusCode}, using fallback");
                    // Fallback to user-specific client-side pagination
                    return await GetParcelsPagedByUserFallback(userId, queryParams);
                }
                
                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    ReferenceHandler = ReferenceHandler.Preserve
                };
                
                var result = JsonSerializer.Deserialize<PaginatedResponse<Parcel>>(content, options);
                System.Diagnostics.Debug.WriteLine($"Successfully fetched {result?.Data?.Count()} parcels from page {queryParams.Page} for user {userId}");
                
                return result ?? new PaginatedResponse<Parcel>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetParcelsPagedByUserAsync: {ex.Message}");
                // Fallback to user-specific client-side pagination
                return await GetParcelsPagedByUserFallback(userId, queryParams);
            }
        }

        public async Task<IEnumerable<string>> GetUniqueDestinationsByUserAsync(int userId, ParcelStatus? status = null)
        {
            try
            {
                var statusParam = status.HasValue ? $"?status={status}" : "";
                var response = await _httpClient.GetAsync($"/api/parcels/user/{userId}/destinations{statusParam}");
                
                if (!response.IsSuccessStatusCode)
                {
                    // Fallback to user-specific destinations
                    return await GetDestinationsByUserFallback(userId, status);
                }
                
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<IEnumerable<string>>(content) ?? new List<string>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting unique destinations for user: {ex.Message}");
                return await GetDestinationsByUserFallback(userId, status);
            }
        }

        public async Task<IEnumerable<string>> GetUniqueClerksByDestinationAndUserAsync(int userId, string destination, ParcelStatus? status = null)
        {
            try
            {
                var statusParam = status.HasValue ? $"&status={status}" : "";
                var response = await _httpClient.GetAsync($"/api/parcels/user/{userId}/clerks?destination={Uri.EscapeDataString(destination)}{statusParam}");
                
                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"API call failed, using fallback for clerks by destination and user");
                    return await GetClerksByDestinationAndUserFallback(userId, destination, status);
                }
                
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<IEnumerable<string>>(content) ?? new List<string>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting clerks by destination and user: {ex.Message}");
                return await GetClerksByDestinationAndUserFallback(userId, destination, status);
            }
        }

        private string BuildQueryString(ParcelQueryParams queryParams)
        {
            var parameters = new List<string>
            {
                $"page={queryParams.Page}",
                $"pageSize={queryParams.PageSize}"
            };

            if (!string.IsNullOrWhiteSpace(queryParams.Destination))
                parameters.Add($"destination={Uri.EscapeDataString(queryParams.Destination)}");

            if (queryParams.Status.HasValue)
                parameters.Add($"status={queryParams.Status}");
            
            if (queryParams.Statuses != null && queryParams.Statuses.Any())
            {
                var statusString = string.Join(",", queryParams.Statuses);
                parameters.Add($"statuses={statusString}");
            }

            if (queryParams.FromDate.HasValue)
                parameters.Add($"fromDate={queryParams.FromDate.Value:yyyy-MM-dd}");

            if (queryParams.ToDate.HasValue)
                parameters.Add($"toDate={queryParams.ToDate.Value:yyyy-MM-dd}");

            if (!string.IsNullOrWhiteSpace(queryParams.CreatedByUsername))
                parameters.Add($"createdBy={Uri.EscapeDataString(queryParams.CreatedByUsername)}");

            if (!string.IsNullOrWhiteSpace(queryParams.SearchTerm))
                parameters.Add($"search={Uri.EscapeDataString(queryParams.SearchTerm)}");

            return string.Join("&", parameters);
        }

        private async Task<PaginatedResponse<Parcel>> GetParcelsPagedFallback(ParcelQueryParams queryParams)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Using client-side pagination fallback");
                
                // Get all parcels and filter client-side (temporary fallback)
                var allParcels = await GetAllParcelsAsync();
                var filtered = allParcels.AsEnumerable();

                // Apply filters
                if (!string.IsNullOrWhiteSpace(queryParams.Destination))
                {
                    filtered = filtered.Where(p => string.Equals(p.Destination, queryParams.Destination, StringComparison.OrdinalIgnoreCase));
                }

                if (queryParams.Status.HasValue)
                {
                    filtered = filtered.Where(p => p.Status == queryParams.Status.Value);
                }
                else if (queryParams.Statuses != null && queryParams.Statuses.Any())
                {
                    filtered = filtered.Where(p => queryParams.Statuses.Contains(p.Status));
                }

                if (queryParams.FromDate.HasValue)
                {
                    filtered = filtered.Where(p => p.CreatedAt.Date >= queryParams.FromDate.Value.Date);
                }

                if (queryParams.ToDate.HasValue)
                {
                    filtered = filtered.Where(p => p.CreatedAt.Date <= queryParams.ToDate.Value.Date);
                }

                if (!string.IsNullOrWhiteSpace(queryParams.CreatedByUsername))
                {
                    filtered = filtered.Where(p => p.CreatedBy != null && 
                        string.Equals(p.CreatedBy.Username, queryParams.CreatedByUsername, StringComparison.OrdinalIgnoreCase));
                }

                var filteredList = filtered.OrderByDescending(p => p.CreatedAt).ToList();
                var totalCount = filteredList.Count;
                var totalPages = (int)Math.Ceiling((double)totalCount / queryParams.PageSize);

                // Apply pagination
                var pagedData = filteredList
                    .Skip((queryParams.Page - 1) * queryParams.PageSize)
                    .Take(queryParams.PageSize)
                    .ToList();

                return new PaginatedResponse<Parcel>
                {
                    Data = pagedData,
                    Page = queryParams.Page,
                    PageSize = queryParams.PageSize,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    HasNextPage = queryParams.Page < totalPages,
                    HasPreviousPage = queryParams.Page > 1
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in fallback pagination: {ex.Message}");
                return new PaginatedResponse<Parcel>();
            }
        }

        private async Task<IEnumerable<Parcel>> GetParcelsForDispatchFallback(string? destination, 
            List<ParcelStatus>? statuses, DateTime? fromDate, DateTime? toDate, string? createdByUsername)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Using client-side filtering fallback for dispatch parcels");
                
                var allParcels = await GetAllParcelsAsync();
                var filtered = allParcels.AsEnumerable();

                // Apply filters
                if (!string.IsNullOrWhiteSpace(destination))
                {
                    filtered = filtered.Where(p => string.Equals(p.Destination, destination, StringComparison.OrdinalIgnoreCase));
                }

                if (statuses != null && statuses.Any())
                {
                    filtered = filtered.Where(p => statuses.Contains(p.Status));
                }
                else
                {
                    // Default to pending and confirmed for dispatch
                    filtered = filtered.Where(p => p.Status == ParcelStatus.Pending || p.Status == ParcelStatus.Confirmed);
                }

                if (fromDate.HasValue)
                {
                    filtered = filtered.Where(p => p.CreatedAt.Date >= fromDate.Value.Date);
                }

                if (toDate.HasValue)
                {
                    filtered = filtered.Where(p => p.CreatedAt.Date <= toDate.Value.Date);
                }

                if (!string.IsNullOrWhiteSpace(createdByUsername))
                {
                    filtered = filtered.Where(p => p.CreatedBy != null && 
                        string.Equals(p.CreatedBy.Username, createdByUsername, StringComparison.OrdinalIgnoreCase));
                }

                var result = filtered.OrderByDescending(p => p.CreatedAt).ToList();
                System.Diagnostics.Debug.WriteLine($"Fallback: Found {result.Count} parcels for dispatch");
                
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in dispatch parcels fallback: {ex.Message}");
                return new List<Parcel>();
            }
        }

        private async Task<IEnumerable<string>> GetClerksByDestinationFallback(string destination, ParcelStatus? status)
        {
            try
            {
                var allParcels = await GetAllParcelsAsync();
                var filtered = allParcels.Where(p => 
                    string.Equals(p.Destination, destination, StringComparison.OrdinalIgnoreCase) &&
                    (!status.HasValue || p.Status == status.Value) &&
                    p.CreatedBy != null && 
                    !string.IsNullOrWhiteSpace(p.CreatedBy.Username));

                return filtered
                    .Select(p => p.CreatedBy.Username)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in clerk fallback: {ex.Message}");
                return new List<string>();
            }
        }

        private async Task<PaginatedResponse<Parcel>> GetParcelsPagedByUserFallback(int userId, ParcelQueryParams queryParams)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Using user-specific client-side pagination fallback for user {userId}");
                
                // Get user-specific parcels and filter client-side
                var userParcels = await GetParcelsByUserAsync(userId);
                var filtered = userParcels.AsEnumerable();

                // Apply filters
                if (!string.IsNullOrWhiteSpace(queryParams.Destination))
                {
                    filtered = filtered.Where(p => string.Equals(p.Destination, queryParams.Destination, StringComparison.OrdinalIgnoreCase));
                }

                if (queryParams.Status.HasValue)
                {
                    filtered = filtered.Where(p => p.Status == queryParams.Status.Value);
                }
                else if (queryParams.Statuses != null && queryParams.Statuses.Any())
                {
                    filtered = filtered.Where(p => queryParams.Statuses.Contains(p.Status));
                }

                if (queryParams.FromDate.HasValue)
                {
                    filtered = filtered.Where(p => p.CreatedAt.Date >= queryParams.FromDate.Value.Date);
                }

                if (queryParams.ToDate.HasValue)
                {
                    filtered = filtered.Where(p => p.CreatedAt.Date <= queryParams.ToDate.Value.Date);
                }

                if (!string.IsNullOrWhiteSpace(queryParams.CreatedByUsername))
                {
                    filtered = filtered.Where(p => p.CreatedBy != null && 
                        string.Equals(p.CreatedBy.Username, queryParams.CreatedByUsername, StringComparison.OrdinalIgnoreCase));
                }

                var filteredList = filtered.OrderByDescending(p => p.CreatedAt).ToList();
                var totalCount = filteredList.Count;
                var totalPages = (int)Math.Ceiling((double)totalCount / queryParams.PageSize);

                // Apply pagination
                var pagedData = filteredList
                    .Skip((queryParams.Page - 1) * queryParams.PageSize)
                    .Take(queryParams.PageSize)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"User {userId} fallback: Found {totalCount} parcels matching criteria, returning page {queryParams.Page} with {pagedData.Count} items");

                return new PaginatedResponse<Parcel>
                {
                    Data = pagedData,
                    Page = queryParams.Page,
                    PageSize = queryParams.PageSize,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    HasNextPage = queryParams.Page < totalPages,
                    HasPreviousPage = queryParams.Page > 1
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in user fallback pagination: {ex.Message}");
                return new PaginatedResponse<Parcel>();
            }
        }

        private async Task<IEnumerable<string>> GetDestinationsByUserFallback(int userId, ParcelStatus? status)
        {
            try
            {
                var userParcels = await GetParcelsByUserAsync(userId);
                var filtered = userParcels.Where(p => 
                    (!status.HasValue || p.Status == status.Value) &&
                    !string.IsNullOrWhiteSpace(p.Destination));

                return filtered
                    .Select(p => p.Destination)
                    .Distinct()
                    .OrderBy(d => d)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in destinations fallback: {ex.Message}");
                return new List<string> { "Nairobi", "Mombasa", "Kisumu", "Eldoret" };
            }
        }

        private async Task<IEnumerable<string>> GetClerksByDestinationAndUserFallback(int userId, string destination, ParcelStatus? status)
        {
            try
            {
                var userParcels = await GetParcelsByUserAsync(userId);
                var filtered = userParcels.Where(p => 
                    string.Equals(p.Destination, destination, StringComparison.OrdinalIgnoreCase) &&
                    (!status.HasValue || p.Status == status.Value) &&
                    p.CreatedBy != null && 
                    !string.IsNullOrWhiteSpace(p.CreatedBy.Username));

                return filtered
                    .Select(p => p.CreatedBy.Username)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in clerk by user fallback: {ex.Message}");
                return new List<string>();
            }
        }

        public async Task<ClerkCashInSummaryDto?> GetClerkCashInSummaryAsync(int userId, DateTime date)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[ParcelService MAUI Client] Fetching clerk cash-in summary for user: {userId} on date: {date:yyyy-MM-dd}");
                
                // Use the new user-specific endpoint that handles the date range logic
                var response = await _httpClient.GetAsync($"/api/ParcelDeposits/clerk-summary/user/{userId}?date={date:yyyy-MM-dd}");
                
                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"API call failed with status: {response.StatusCode}");
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Error content: {errorContent}");
                    return null;
                }
                
                var content = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"API response content: {content}");
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                var userSummary = JsonSerializer.Deserialize<ClerkCashInSummaryDto>(content, options);
                
                System.Diagnostics.Debug.WriteLine($"Deserialized clerk summary for user {userId}: {userSummary != null}");
                if (userSummary != null)
                {
                    System.Diagnostics.Debug.WriteLine($"User {userId} - RemainingDebt: {userSummary.RemainingDebt}, TotalPaidAmount: {userSummary.TotalPaidAmount}, ParcelCount: {userSummary.ParcelCount}");
                }
                
                return userSummary;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching clerk cash-in summary for user {userId}: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return null;
            }
        }
    }
}
