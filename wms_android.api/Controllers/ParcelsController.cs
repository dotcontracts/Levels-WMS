using Microsoft.AspNetCore.Mvc;
using wms_android.shared.Models;
using wms_android.shared.Interfaces;
using wms_android.shared.Services;
using wms_android.shared.DTOs;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;
using wms_android.shared.Data;
using wms_android.cache;
using wms_android.cache.Interfaces;
using wms_android.api.Services;

namespace wms_android.api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ParcelsController : ControllerBase
    {
        private readonly IParcelService _parcelService;
        private readonly AppDbContext _context;
        private readonly ICacheService _cache;
        private readonly ICacheInvalidationService _cacheInvalidation;

        // Compiled queries for better performance
        private static readonly Func<AppDbContext, string, int, int, IAsyncEnumerable<Parcel>> 
            GetParcelsByDestinationCompiled = EF.CompileAsyncQuery(
                (AppDbContext context, string destination, int skip, int take) =>
                    context.Parcels.AsNoTracking()
                        .Where(p => p.Destination == destination)
                        .OrderByDescending(p => p.CreatedAt)
                        .Skip(skip)
                        .Take(take));

        private static readonly Func<AppDbContext, int, int, IAsyncEnumerable<Parcel>>
            GetParcelsPagedCompiled = EF.CompileAsyncQuery(
                (AppDbContext context, int skip, int take) =>
                    context.Parcels.AsNoTracking()
                        .OrderByDescending(p => p.CreatedAt)
                        .Skip(skip)
                        .Take(take));

        public ParcelsController(IParcelService parcelService, AppDbContext context, ICacheService cache, ICacheInvalidationService cacheInvalidation)
        {
            _parcelService = parcelService;
            _context = context;
            _cache = cache;
            _cacheInvalidation = cacheInvalidation;
        }

        [HttpGet]
        public async Task<IActionResult> GetParcels(
            [FromQuery] string? branchName = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 100)
        {
            try
            {
                // Validate pagination parameters
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 1000); // Max 1000 items per page
                
                var query = _context.Parcels.AsNoTracking().AsQueryable();
                
                // If branchName is provided, filter by destination column
                if (!string.IsNullOrEmpty(branchName))
                {
                    query = query.Where(p => p.Destination == branchName);
                }
                
                // Add default ordering and pagination to prevent loading all records
                var parcels = await query
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
                    
                return Ok(parcels);
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Enhanced paginated endpoint with efficient database-level filtering and caching
        /// </summary>
        [HttpGet("paged")]
        public async Task<IActionResult> GetParcelsPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? destination = null,
            [FromQuery] ParcelStatus? status = null,
            [FromQuery] string? statuses = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? createdByUsername = null,
            [FromQuery] string? search = null)
        {
            try
            {
                // Validate pagination parameters
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 1500); // Increased limit for better performance with large datasets

                // Use AsNoTracking for better performance on read-only operations
                var query = _context.Parcels
                    .AsNoTracking()
                    .AsQueryable();

                // Apply filters at database level for optimal performance
                if (!string.IsNullOrEmpty(destination))
                {
                    query = query.Where(p => p.Destination == destination);
                }

                // Handle multiple statuses efficiently
                if (!string.IsNullOrEmpty(statuses))
                {
                    var statusList = statuses.Split(',')
                        .Where(s => Enum.TryParse<ParcelStatus>(s.Trim(), out _))
                        .Select(s => Enum.Parse<ParcelStatus>(s.Trim()))
                        .ToList();
                    
                    if (statusList.Any())
                    {
                        query = query.Where(p => statusList.Contains(p.Status));
                    }
                }
                else if (status.HasValue)
                {
                    query = query.Where(p => p.Status == status.Value);
                }

                // Efficient date filtering using indexes
                if (fromDate.HasValue)
                {
                    query = query.Where(p => p.CreatedAt >= fromDate.Value.Date);
                }
                if (toDate.HasValue)
                {
                    var endDate = toDate.Value.Date.AddDays(1); // Include entire day
                    query = query.Where(p => p.CreatedAt < endDate);
                }

                // Username filtering with join
                if (!string.IsNullOrEmpty(createdByUsername))
                {
                    query = query.Where(p => p.CreatedBy != null && p.CreatedBy.Username == createdByUsername);
                }

                // Search functionality across multiple fields
                if (!string.IsNullOrEmpty(search))
                {
                    var searchLower = search.ToLower();
                    query = query.Where(p => 
                        p.WaybillNumber.ToLower().Contains(searchLower) ||
                        p.Sender.ToLower().Contains(searchLower) ||
                        p.Receiver.ToLower().Contains(searchLower) ||
                        p.Destination.ToLower().Contains(searchLower) ||
                        p.ReceiverTelephone.Contains(search) ||
                        p.SenderTelephone.Contains(search));
                }

                var pageCacheKey = CacheKeys.Parcels.Paged(page, pageSize, destination, (int?)status, search, fromDate?.ToString("yyyyMMdd"), toDate?.ToString("yyyyMMdd"), createdByUsername);
                var countCacheKey = pageCacheKey + ":count";

                var totalCount = await _cache.GetOrSetAsync(countCacheKey, async () =>
                {
                    var c = await query.CountAsync();
                    return new CountWrapper { Value = c };
                }, TimeSpan.FromMinutes(5), new[] { CacheTags.Parcels });
                var totalCountValue = totalCount.Value;

                var totalPages = (int)Math.Ceiling((double)totalCountValue / pageSize);

                var pagedResult = await _cache.GetOrSetAsync(pageCacheKey, async () =>
                {
                    var parcels = await query
                        .OrderByDescending(p => p.CreatedAt)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .Select(p => new Parcel
                        {
                            Id = p.Id,
                            WaybillNumber = p.WaybillNumber,
                            Sender = p.Sender,
                            Receiver = p.Receiver,
                            Destination = p.Destination,
                            Status = p.Status,
                            TotalAmount = p.TotalAmount,
                            CreatedAt = p.CreatedAt,
                            CreatedById = p.CreatedById,
                            // CreatorLastNameSnapshot carries the clerk's full name for frontend grouping.
                            // Prefer the live JOIN on Users; fall back to the stored snapshot column
                            // in case createdById is null or the user record was deleted.
                            CreatorLastNameSnapshot = p.CreatedBy != null
                                ? (p.CreatedBy.FirstName + " " + p.CreatedBy.LastName).Trim()
                                : p.CreatorLastNameSnapshot,
                            DispatchedAt = p.DispatchedAt,
                            SenderTelephone = p.SenderTelephone,
                            ReceiverTelephone = p.ReceiverTelephone,
                            PaymentMethods = p.PaymentMethods,
                            Quantity = p.Quantity,
                            Description = p.Description,
                            ConfirmedById = p.ConfirmedById,
                            ConfirmedAt = p.ConfirmedAt,
                            ConfirmedByName = p.ConfirmedById != null ? p.ConfirmedBy.FirstName + " " + p.ConfirmedBy.LastName : null,
                            DispatchedById = p.DispatchedById,
                            DispatchedByName = p.DispatchedById != null ? p.DispatchedBy.FirstName + " " + p.DispatchedBy.LastName : null,
                            DeliveredById = p.DeliveredById,
                            DeliveredAt = p.DeliveredAt,
                            DeliveredByName = p.DeliveredById != null ? p.DeliveredBy.FirstName + " " + p.DeliveredBy.LastName : null,
                        })
                        .ToListAsync();
                    return new ParcelPageWrapper { Parcels = parcels };
                }, TimeSpan.FromMinutes(5), new[] { CacheTags.Parcels });



                var result = new
                {
                    Data = pagedResult.Parcels,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCountValue,
                    TotalPages = totalPages,
                    HasNextPage = page < totalPages,
                    HasPreviousPage = page > 1
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetParcelsPaged: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// User-specific paginated endpoint for better performance in AllParcelsView
        /// </summary>
        [HttpGet("user/{userId}/paged")]
        public async Task<IActionResult> GetParcelsPagedByUser(
            int userId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? destination = null,
            [FromQuery] ParcelStatus? status = null,
            [FromQuery] string? statuses = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? search = null,
            [FromQuery] string? paymentMethod = null)
        {
            try
            {
                // Validate pagination parameters
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 1500); // Increased limit for better performance with large datasets

                // Enforce 3-day restriction for user-specific parcel queries
                var maxFromDate = DateTime.Today.AddDays(-3);
                if (!fromDate.HasValue || fromDate.Value.Date < maxFromDate)
                {
                    fromDate = maxFromDate;
                }

                var query = _context.Parcels
                    .AsNoTracking()
                    .Where(p => p.CreatedById == userId)
                    .AsQueryable();

                // Apply filters efficiently at database level
                if (!string.IsNullOrEmpty(destination))
                {
                    query = query.Where(p => p.Destination == destination);
                }

                if (!string.IsNullOrEmpty(statuses))
                {
                    var statusList = statuses.Split(',')
                        .Where(s => Enum.TryParse<ParcelStatus>(s.Trim(), out _))
                        .Select(s => Enum.Parse<ParcelStatus>(s.Trim()))
                        .ToList();
                    
                    if (statusList.Any())
                    {
                        query = query.Where(p => statusList.Contains(p.Status));
                    }
                }
                else if (status.HasValue)
                {
                    query = query.Where(p => p.Status == status.Value);
                }

                if (fromDate.HasValue)
                {
                    query = query.Where(p => p.CreatedAt >= fromDate.Value.Date);
                }
                if (toDate.HasValue)
                {
                    var endDate = toDate.Value.Date.AddDays(1);
                    query = query.Where(p => p.CreatedAt < endDate);
                }

                if (!string.IsNullOrEmpty(search))
                {
                    var searchLower = search.ToLower();
                    query = query.Where(p => 
                        p.WaybillNumber.ToLower().Contains(searchLower) ||
                        p.Sender.ToLower().Contains(searchLower) ||
                        p.Receiver.ToLower().Contains(searchLower) ||
                        p.Destination.ToLower().Contains(searchLower) ||
                        p.ReceiverTelephone.Contains(search) ||
                        p.SenderTelephone.Contains(search));
                }

                if (!string.IsNullOrEmpty(paymentMethod))
                {
                    var paymentMethodLower = paymentMethod.ToLower();
                    query = query.Where(p => p.PaymentMethods != null && p.PaymentMethods.ToLower().Contains(paymentMethodLower));
                }

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var parcels = await query
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var result = new
                {
                    Data = parcels,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    HasNextPage = page < totalPages,
                    HasPreviousPage = page > 1
                };

                System.Diagnostics.Debug.WriteLine($"Retrieved page {page} for user {userId} with {parcels.Count} parcels out of {totalCount} total");
                return Ok(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetParcelsPagedByUser: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Optimized destinations endpoint with caching headers
        /// </summary>
        [HttpGet("destinations")]
        public async Task<ActionResult<IEnumerable<string>>> GetDestinations([FromQuery] ParcelStatus? status = null)
        {
            try
            {
                // Add cache headers for better performance
                Response.Headers.Add("Cache-Control", "public, max-age=300"); // 5 minutes cache
                
                var query = _context.Parcels.AsQueryable();

                if (status.HasValue)
                {
                    query = query.Where(p => p.Status == status.Value);
                }

                var destinations = await query
                    .Where(p => !string.IsNullOrEmpty(p.Destination))
                    .Select(p => p.Destination)
                    .Distinct()
                    .OrderBy(d => d)
                    .ToListAsync();

                return Ok(destinations);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting destinations: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// User-specific destinations for better filtering
        /// </summary>
        [HttpGet("user/{userId}/destinations")]
        public async Task<ActionResult<IEnumerable<string>>> GetDestinationsByUser(int userId, [FromQuery] ParcelStatus? status = null)
        {
            try
            {
                Response.Headers.Add("Cache-Control", "public, max-age=300");
                
                var query = _context.Parcels
                    .Where(p => p.CreatedById == userId)
                    .AsQueryable();

                if (status.HasValue)
                {
                    query = query.Where(p => p.Status == status.Value);
                }

                var destinations = await query
                    .Where(p => !string.IsNullOrEmpty(p.Destination))
                    .Select(p => p.Destination)
                    .Distinct()
                    .OrderBy(d => d)
                    .ToListAsync();

                return Ok(destinations);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting destinations for user: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get all branches for destination selection
        /// </summary>
        [HttpGet("branches")]
        public async Task<ActionResult<IEnumerable<Branch>>> GetAllBranches()
        {
            try
            {
                // Add cache headers for better performance
                Response.Headers.Add("Cache-Control", "public, max-age=3600"); // 1 hour cache
                
                var branches = await _context.Branches
                    .OrderBy(b => b.Name)
                    .ToListAsync();
                    
                return Ok(branches);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting all branches: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("waybill/generate")]
        public async Task<ActionResult<string>> GenerateWaybill()
        {
            try
            {
                // Log the request
                System.Diagnostics.Debug.WriteLine("Generating waybill number...");
                
                // Call the service to generate waybill number
                var waybillNumber = await _parcelService.GenerateWaybillNumberAsync();
                
                // Log the generated number
                System.Diagnostics.Debug.WriteLine($"Successfully generated waybill number: {waybillNumber}");
                
                // Return as a simple string value
                return Ok(waybillNumber);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error generating waybill: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $" Inner exception: {ex.InnerException.Message}";
                }
                System.Diagnostics.Debug.WriteLine(errorMessage);
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                return StatusCode(500, new { 
                    error = "Failed to generate waybill number", 
                    message = ex.Message,
                    innerException = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace 
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Parcel>> GetParcel(Guid id)
        {
            var parcel = await _parcelService.GetParcelByIdAsync(id);
            return Ok(parcel);
        }

        [HttpGet("waybill/{waybillNumber}")]
        public async Task<ActionResult<Parcel>> GetParcelByWaybill(string waybillNumber)
        {
            var parcel = await _parcelService.GetParcelByWaybillNumberAsync(waybillNumber);
            return Ok(parcel);
        }

        [HttpGet("qr/{qrCode}")]
        public async Task<ActionResult<IEnumerable<Parcel>>> GetParcelsByQRCode(string qrCode)
        {
            var parcels = await _parcelService.GetParcelsByQRCodeAsync(qrCode);
            return Ok(parcels);
        }

        [HttpGet("pending")]
        public ActionResult<IEnumerable<Parcel>> GetPendingOrders([FromQuery] string? branchName)
        {
            var parcelsQuery = _parcelService.GetPendingOrdersAsync().Result.AsQueryable();
            
            if (!string.IsNullOrEmpty(branchName))
            {
                // Filter by destination instead of using UserBranches join
                parcelsQuery = parcelsQuery.Where(p => p.Destination == branchName);
            }

            var parcels = parcelsQuery.ToList();
            return Ok(parcels);
        }

        [HttpGet("count")]
        public async Task<ActionResult<object>> GetParcelCount([FromQuery] DateTime date, [FromQuery] string? branchName)
        {
            var parcelsQuery = _context.Parcels.Where(p => p.CreatedAt.Date == date.Date);

            if (!string.IsNullOrEmpty(branchName))
            {
                // Filter by destination instead of using UserBranches join
                parcelsQuery = parcelsQuery.Where(p => p.Destination == branchName);
            }

            var count = await parcelsQuery.CountAsync();
            return Ok(new { count });
        }

        [HttpGet("sales")]
        public async Task<ActionResult<object>> GetTotalSales([FromQuery] DateTime date, [FromQuery] string? branchName)
        {
            var parcelsQuery = _context.Parcels.Where(p => p.CreatedAt.Date == date.Date);

            if (!string.IsNullOrEmpty(branchName))
            {
                // Filter by destination instead of using UserBranches join
                parcelsQuery = parcelsQuery.Where(p => p.Destination == branchName);
            }

            var total = await parcelsQuery.SumAsync(p => p.TotalAmount);
            return Ok(new { totalSales = total });
        }

        [HttpPost]
        public async Task<ActionResult<Parcel>> CreateParcel([FromBody] Parcel parcel)
        {
            try
            {
                // Log incoming parcel
                System.Diagnostics.Debug.WriteLine($"Received parcel: {JsonConvert.SerializeObject(parcel)}");

                // Only generate waybill number if not provided
                if (string.IsNullOrEmpty(parcel.WaybillNumber))
                {
                    parcel.WaybillNumber = await _parcelService.GenerateWaybillNumberAsync();
                    parcel.QRCode = parcel.WaybillNumber; // Set QR code to be the same as waybill number
                }

                // Log after generation
                System.Diagnostics.Debug.WriteLine($"Using WaybillNumber: {parcel.WaybillNumber}");

                var validationErrors = ValidateParcel(parcel);
                if (validationErrors.Any())
                {
                    return BadRequest(new { Errors = validationErrors });
                }

                if (!string.IsNullOrWhiteSpace(parcel.Destination) &&
                    parcel.Destination.Trim().Equals("Mombasa", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { Errors = new[] { "Parcels destined for Mombasa are not accepted at this time." } });
                }

                // Ensure CreatedAt is set
                parcel.CreatedAt = DateTime.UtcNow;
                
                // Ensure Status is set
                if (parcel.Status == 0)
                {
                    parcel.Status = ParcelStatus.Pending;
                }

                // Note: CreatedById is now part of the Parcel model and should be passed in the request
                // If it's not set, it will be null in the database, which is allowed

                Console.WriteLine("=== CONTROLLER: Calling CreateParcelAsync ===");
                
                try 
                {
                    var created = await _parcelService.CreateParcelAsync(parcel);
                    Console.WriteLine("=== CONTROLLER: Service call completed successfully ===");
                    
                    if (created == null)
                    {
                        Console.WriteLine("=== CONTROLLER: Service returned null - unexpected ===");
                        throw new Exception("Failed to create parcel - service returned null");
                    }

                    Console.WriteLine("=== CONTROLLER: Parcel creation completed successfully ===");
                    
                    await _cacheInvalidation.InvalidateParcelsCache();
                    if (created.CreatedById.HasValue)
                        await _cacheInvalidation.InvalidateUserDashboardCache(created.CreatedById.Value);
                    return Ok(created);
                }
                catch (Exception serviceEx)
                {
                    Console.WriteLine($"=== SERVICE LAYER EXCEPTION ===");
                    Console.WriteLine($"Exception Type: {serviceEx.GetType().Name}");
                    Console.WriteLine($"Exception Message: {serviceEx.Message}");
                    Console.WriteLine($"Stack Trace: {serviceEx.StackTrace}");
                    
                    // Log inner exceptions
                    var innerEx = serviceEx.InnerException;
                    int depth = 1;
                    while (innerEx != null && depth <= 5)
                    {
                        Console.WriteLine($"Inner Exception {depth}: {innerEx.GetType().Name} - {innerEx.Message}");
                        innerEx = innerEx.InnerException;
                        depth++;
                    }
                    
                    throw; // Re-throw to be caught by outer catch
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== CONTROLLER LEVEL EXCEPTION ===");
                Console.WriteLine($"Exception Type: {ex.GetType().Name}");
                Console.WriteLine($"Exception Message: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                
                return StatusCode(500, new { 
                    error = "Internal server error",
                    message = ex.Message,
                    type = ex.GetType().Name,
                    innerException = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace?.Split('\n').Take(10) // Limit stack trace length
                });
            }
        }

        private List<string> ValidateParcel(Parcel parcel)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(parcel.Sender))
                errors.Add("Sender is required");
            if (string.IsNullOrWhiteSpace(parcel.SenderTelephone))
                errors.Add("Sender telephone is required");
            if (string.IsNullOrWhiteSpace(parcel.Receiver))
                errors.Add("Receiver is required");
            if (string.IsNullOrWhiteSpace(parcel.ReceiverTelephone))
                errors.Add("Receiver telephone is required");
            if (string.IsNullOrWhiteSpace(parcel.Destination))
                errors.Add("Destination is required");
            if (string.IsNullOrWhiteSpace(parcel.PaymentMethods))
                errors.Add("Payment method is required");

            return errors;
        }

        [HttpPost("batch")]
        public async Task<ActionResult<List<Parcel>>> CreateCartParcels([FromBody] List<Parcel> parcels)
        {
            List<Parcel> createdParcels = await _parcelService.CreateCartParcels(parcels);
            return Ok(createdParcels);
        }

        [HttpPost("{id}/finalize")]
        public async Task<ActionResult> FinalizeWaybill(Guid id)
        {
            await _parcelService.FinalizeWaybillAsync(id);
            return Ok();
        }

        [HttpPost("{id}/dispatch")]
        public async Task<ActionResult> DispatchParcel(Guid id, Parcel parcel)
        {
            await _parcelService.DispatchParcelAsync(parcel);
            return Ok();
        }

        [HttpPut("{id}/status")]
        public async Task<ActionResult> UpdateParcelStatus(Guid id, [FromBody] ParcelStatusUpdateDto statusUpdate)
        {
            try
            {
                await _parcelService.UpdateParcelStatusAsync(id, statusUpdate.Status, statusUpdate.ConfirmedById, statusUpdate.DeliveredById);
                return Ok(new { message = $"Parcel status updated to {statusUpdate.Status}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    error = "Failed to update parcel status", 
                    message = ex.Message 
                });
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<Parcel>>> GetParcelsByUser(int userId)
        {
            try
            {
                // Query parcels for a specific user
                var parcels = await _context.Parcels
                    .Where(p => p.CreatedById == userId)
                    .ToListAsync();
                
                System.Diagnostics.Debug.WriteLine($"Found {parcels.Count} parcels for user {userId}");
                return Ok(parcels);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting parcels for user {userId}: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("user/{userId}/amount-owed")]
        public async Task<ActionResult<double>> GetAmountOwedByUser(int userId, [FromQuery] DateTime date)
        {
            try
            {
                var cached = await _cache.GetOrSetAsync(
                    CacheKeys.UserDashboard.AmountOwed(userId, date.ToString("yyyyMMdd")),
                    async () => new DoubleWrapper { Value = await _parcelService.GetAmountOwedByUserAsync(userId, date) },
                    TimeSpan.FromMinutes(5),
                    new[] { CacheKeys.UserDashboard.UserTag(userId) });
                return Ok(cached.Value);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting amount owed for user {userId}: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("user/{userId}/cash-in")]
        public async Task<ActionResult<double>> GetCashInByUser(int userId, [FromQuery] DateTime date)
        {
            try
            {
                var cached = await _cache.GetOrSetAsync(
                    CacheKeys.UserDashboard.CashIn(userId, date.ToString("yyyyMMdd")),
                    async () => new DoubleWrapper { Value = await _parcelService.GetCashInByUserAsync(userId, date) },
                    TimeSpan.FromMinutes(5),
                    new[] { CacheKeys.UserDashboard.UserTag(userId) });
                return Ok(cached.Value);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting cash-in for user {userId}: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("user/{userId}/daily-sales")]
        public async Task<ActionResult<double>> GetDailySalesByUser(int userId, [FromQuery] DateTime date)
        {
            try
            {
                var cached = await _cache.GetOrSetAsync(
                    CacheKeys.UserDashboard.DailySales(userId, date.ToString("yyyyMMdd")),
                    async () => new DoubleWrapper { Value = await _parcelService.GetDailySalesByUserAsync(userId, date) },
                    TimeSpan.FromMinutes(5),
                    new[] { CacheKeys.UserDashboard.UserTag(userId) });
                return Ok(cached.Value);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting daily sales for user {userId}: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("user/{userId}/monthly-sales")]
        public async Task<ActionResult<double>> GetMonthlySalesByUser(int userId, [FromQuery] DateTime date)
        {
            try
            {
                var cached = await _cache.GetOrSetAsync(
                    CacheKeys.UserDashboard.MonthlySales(userId, date.ToString("yyyyMM")),
                    async () => new DoubleWrapper { Value = await _parcelService.GetMonthlySalesByUserAsync(userId, date) },
                    TimeSpan.FromMinutes(5),
                    new[] { CacheKeys.UserDashboard.UserTag(userId) });
                return Ok(cached.Value);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting monthly sales for user {userId}: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("branch-cod-lock-status")]
        public async Task<ActionResult<BranchCodLockStatusDto>> GetBranchCodLockStatus([FromQuery] string branchName, [FromQuery] DateTime? date)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(branchName))
                    return BadRequest("branchName is required.");
                var result = await _parcelService.GetBranchCodLockStatusAsync(branchName, date ?? DateTime.UtcNow);
                return Ok(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting COD lock status for {branchName}: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("clerk-lock-status")]
        public async Task<ActionResult<ClerkLockStatusDto>> GetClerkLockStatus([FromQuery] int userId, [FromQuery] DateTime? date)
        {
            try
            {
                if (userId <= 0)
                    return BadRequest("userId is required.");
                var result = await _parcelService.GetClerkLockStatusAsync(userId, date ?? DateTime.UtcNow);
                return Ok(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting clerk lock status for user {userId}: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("clerk-report")]
        public async Task<ActionResult<ClerkReportDto>> GetClerkReport([FromQuery] int userId, [FromQuery] DateTime? date)
        {
            try
            {
                if (userId <= 0)
                    return BadRequest("userId is required.");
                var resolvedDate = date ?? DateTime.UtcNow;
                var cacheKey = $"clerk-report:{userId}:{resolvedDate:yyyyMMdd}";
                var result = await _cache.GetOrSetAsync(cacheKey,
                    () => _parcelService.GetClerkReportAsync(userId, resolvedDate),
                    TimeSpan.FromMinutes(2),
                    new[] { CacheTags.UserDashboard });
                return Ok(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting clerk report for user {userId}: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("branch-manager-dashboard")]
        public async Task<ActionResult<BranchManagerDashboardDto>> GetBranchManagerDashboard([FromQuery] string branchName, [FromQuery] DateTime date)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(branchName))
                    return BadRequest("branchName is required.");
                var cacheKey = $"branch-dashboard:{branchName}:{date:yyyyMMdd}";
                var result = await _cache.GetOrSetAsync(cacheKey,
                    () => _parcelService.GetBranchManagerDashboardAsync(branchName, date),
                    TimeSpan.FromMinutes(2),
                    new[] { CacheTags.UserDashboard });
                return Ok(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting branch manager dashboard for {branchName}: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("reports/clerk-debit-summary")]
        public async Task<IActionResult> GetClerkDebitSummary(
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate)
        {
            try
            {
                var fromUtc = fromDate.Date.ToUniversalTime();
                var toUtc = toDate.Date.AddDays(1).ToUniversalTime();
                var cacheKey = CacheKeys.Reports.ClerkDebitSummary(fromDate.ToString("yyyyMMdd"), toDate.ToString("yyyyMMdd"));

                var result = await _cache.GetOrSetAsync(cacheKey, async () =>
                {
                    var rows = await _context.Parcels
                        .AsNoTracking()
                        .Where(p => p.CreatedAt >= fromUtc && p.CreatedAt < toUtc)
                        .Where(p => p.PaymentMethods != null &&
                               (p.PaymentMethods.ToLower().Contains("paid") ||
                                p.PaymentMethods.ToLower().Contains("advance") ||
                                p.PaymentMethods.ToLower().Contains("prepaid")) &&
                               !p.PaymentMethods.ToLower().Contains("cod"))
                        .GroupBy(p => new { p.CreatedById, p.CreatorLastNameSnapshot })
                        .Select(g => new
                        {
                            ClerkId = g.Key.CreatedById,
                            ClerkSnapshot = g.Key.CreatorLastNameSnapshot,
                            TotalPaidAmount = g.Sum(p => p.TotalAmount),
                            ParcelCount = g.Count()
                        })
                        .ToListAsync();

                    var clerkIds = rows.Where(r => r.ClerkId.HasValue).Select(r => r.ClerkId!.Value).Distinct().ToList();
                    var users = await _context.Users.AsNoTracking()
                        .Where(u => clerkIds.Contains(u.Id))
                        .Select(u => new { u.Id, u.Username, u.FirstName, u.LastName })
                        .ToListAsync();
                    var userMap = users.ToDictionary(u => u.Id);

                    var deposits = await _context.DailyExpenses.AsNoTracking()
                        .Where(e => e.Category == "clerk_deposit" && e.Status == "approved" &&
                                    e.Date >= fromDate.Date && e.Date < toDate.Date.AddDays(1))
                        .GroupBy(e => e.ClerkId)
                        .Select(g => new { ClerkId = g.Key, TotalDeposited = g.Sum(e => e.Amount) })
                        .ToListAsync();
                    var depositMap = deposits.ToDictionary(d => d.ClerkId, d => d.TotalDeposited);

                    var expenses = await _context.DailyExpenses.AsNoTracking()
                        .Where(e => e.Category != "clerk_deposit" && e.Status == "approved" &&
                                    e.Date >= fromDate.Date && e.Date < toDate.Date.AddDays(1))
                        .GroupBy(e => e.ClerkId)
                        .Select(g => new { ClerkId = g.Key, TotalExpenses = g.Sum(e => e.Amount) })
                        .ToListAsync();
                    var expenseMap = expenses.ToDictionary(e => e.ClerkId, e => e.TotalExpenses);

                    var summary = rows.Select(r =>
                    {
                        var name = r.ClerkId.HasValue && userMap.TryGetValue(r.ClerkId.Value, out var u)
                            ? $"{u.FirstName} {u.LastName}".Trim()
                            : r.ClerkSnapshot ?? "Unknown";
                        var deposited = r.ClerkId.HasValue && depositMap.TryGetValue(r.ClerkId, out var dep) ? dep : 0;
                        var expensed = r.ClerkId.HasValue && expenseMap.TryGetValue(r.ClerkId, out var exp) ? exp : 0;
                        return new
                        {
                            r.ClerkId,
                            ClerkName = name,
                            Username = r.ClerkId.HasValue && userMap.TryGetValue(r.ClerkId.Value, out var u2) ? u2.Username : null,
                            r.TotalPaidAmount,
                            r.ParcelCount,
                            TotalDeposited = deposited,
                            TotalExpenses = expensed,
                            CurrentDebit = r.TotalPaidAmount - deposited - expensed
                        };
                    }).ToList();

                    return new { Clerks = summary };
                }, TimeSpan.FromMinutes(3), new[] { CacheTags.Reports, CacheTags.Parcels });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("reports/summary")]
        public async Task<IActionResult> GetReportSummary(
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate,
            [FromQuery] string? destination = null,
            [FromQuery] string? branch = null)
        {
            try
            {
                var fromUtc = fromDate.Date.ToUniversalTime();
                var toUtc = toDate.Date.AddDays(1).ToUniversalTime();
                var cacheKey = CacheKeys.Reports.ReportSummary(
                    fromDate.ToString("yyyyMMdd"), toDate.ToString("yyyyMMdd"), destination, branch);

                var result = await _cache.GetOrSetAsync(cacheKey, async () =>
                {
                    var query = _context.Parcels.AsNoTracking()
                        .Where(p => p.CreatedAt >= fromUtc && p.CreatedAt < toUtc);

                    if (!string.IsNullOrWhiteSpace(destination))
                        query = query.Where(p => p.Destination == destination);

                    var dailySales = await query
                        .GroupBy(p => new { p.CreatedById, p.CreatorLastNameSnapshot, Date = p.CreatedAt.Date })
                        .Select(g => new
                        {
                            g.Key.CreatedById,
                            g.Key.CreatorLastNameSnapshot,
                            Date = g.Key.Date,
                            TotalSales = g.Sum(p => p.TotalAmount),
                            ParcelCount = g.Count()
                        })
                        .ToListAsync();

                    var clerkIds = dailySales.Where(r => r.CreatedById.HasValue).Select(r => r.CreatedById!.Value).Distinct().ToList();
                    var users = await _context.Users.AsNoTracking()
                        .Where(u => clerkIds.Contains(u.Id))
                        .Select(u => new { u.Id, u.Username, u.FirstName, u.LastName, RoleId = u.RoleId })
                        .ToListAsync();
                    var userMap = users.ToDictionary(u => u.Id);

                    var grouped = dailySales
                        .GroupBy(r => r.CreatedById)
                        .Select(g =>
                        {
                            var userId = g.Key;
                            var name = userId.HasValue && userMap.TryGetValue(userId.Value, out var u)
                                ? new { Username = u.Username, FirstName = u.FirstName, LastName = u.LastName }
                                : new { Username = g.First().CreatorLastNameSnapshot ?? "Unknown", FirstName = (string?)null, LastName = (string?)null };
                            return new
                            {
                                UserId = userId,
                                name.Username,
                                name.FirstName,
                                name.LastName,
                                DailySales = g.Select(d => new
                                {
                                    Date = d.Date.ToString("yyyy-MM-dd"),
                                    d.TotalSales,
                                    d.ParcelCount,
                                    AverageValue = d.ParcelCount > 0 ? d.TotalSales / d.ParcelCount : 0
                                }).OrderBy(d => d.Date).ToList()
                            };
                        }).ToList();

                    return new { Users = grouped };
                }, TimeSpan.FromMinutes(3), new[] { CacheTags.Reports, CacheTags.Parcels });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("dispatch")]
        public async Task<ActionResult<IEnumerable<Parcel>>> GetParcelsForDispatch([FromQuery] string? destination = null, 
            [FromQuery] string? statuses = null, [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null, 
            [FromQuery] string? createdByUsername = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 100)
        {
            try
            {
                // Validate pagination parameters
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 500);

                var query = _context.Parcels
                    .Include(p => p.CreatedBy)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(destination))
                {
                    query = query.Where(p => p.Destination == destination);
                }

                // Handle status filtering - support multiple statuses (Pending, Confirmed)
                if (!string.IsNullOrEmpty(statuses))
                {
                    var statusList = statuses.Split(',').Select(s => Enum.Parse<ParcelStatus>(s.Trim())).ToList();
                    query = query.Where(p => statusList.Contains(p.Status));
                }
                else
                {
                    // Default to pending and confirmed for dispatch
                    query = query.Where(p => p.Status == ParcelStatus.Pending || p.Status == ParcelStatus.Confirmed);
                }

                // Date filtering - DEFAULT to last 7 days if not provided, then apply user filter
                var defaultFromDate = DateTime.UtcNow.AddDays(-7);
                var effectiveFromDate = fromDate ?? defaultFromDate;
                query = query.Where(p => p.CreatedAt.Date >= effectiveFromDate.Date);
                
                if (toDate.HasValue)
                {
                    query = query.Where(p => p.CreatedAt.Date <= toDate.Value.Date);
                }

                // Username filtering
                if (!string.IsNullOrEmpty(createdByUsername))
                {
                    query = query.Where(p => p.CreatedBy != null && p.CreatedBy.Username == createdByUsername);
                }

                // Add pagination to prevent memory issues and improve performance
                var parcels = await query
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                System.Diagnostics.Debug.WriteLine($"Retrieved {parcels.Count} parcels for dispatch");
                return Ok(parcels);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting parcels for dispatch: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Enhanced multi-destination dispatch loading with optimized performance
        /// Loads all dispatchable parcels across all destinations without destination constraint
        /// </summary>
        [HttpGet("dispatch-multi-dest")]
        public async Task<ActionResult<MultiDestinationDispatchResult>> GetParcelsForMultiDestinationDispatch(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 100,
            [FromQuery] string? statuses = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? createdByUsername = null,
            [FromQuery] int? userBranchId = null)
        {
            try
            {
                // Validate pagination parameters
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 10, 500); // Min 10, Max 500 items per page

                var query = _context.Parcels
                    .AsQueryable();

                // Default to pending and confirmed for dispatch
                var validStatuses = new List<ParcelStatus> { ParcelStatus.Pending, ParcelStatus.Confirmed };
                if (!string.IsNullOrEmpty(statuses))
                {
                    var statusList = statuses.Split(',')
                        .Where(s => Enum.TryParse<ParcelStatus>(s.Trim(), out _))
                        .Select(s => Enum.Parse<ParcelStatus>(s.Trim()))
                        .ToList();
                    if (statusList.Any())
                    {
                        validStatuses = statusList;
                    }
                }
                query = query.Where(p => validStatuses.Contains(p.Status));

                // Branch filtering - filter by creator's branch if userBranchId provided
                if (userBranchId.HasValue)
                {
                    query = query.Where(p => p.CreatedBy != null && p.CreatedBy.BranchId == userBranchId.Value);
                }

                // Date filtering - default to last 7 days for multi-destination
                if (!fromDate.HasValue)
                {
                    fromDate = DateTime.UtcNow.AddDays(-7);
                }
                query = query.Where(p => p.CreatedAt.Date >= fromDate.Value.Date);

                if (toDate.HasValue)
                {
                    var endDate = toDate.Value.Date.AddDays(1);
                    query = query.Where(p => p.CreatedAt < endDate);
                }

                // Username filtering
                if (!string.IsNullOrEmpty(createdByUsername))
                {
                    query = query.Where(p => p.CreatedBy != null && p.CreatedBy.Username == createdByUsername);
                }

                // Get total count before pagination
                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                // Get paginated parcels
                var parcels = await query
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Group parcels by destination for client-side display optimization
                var parcelsByDestination = parcels
                    .GroupBy(p => p.Destination ?? "Unknown")
                    .Select(g => new DestinationGroup
                    {
                        Destination = g.Key,
                        ParcelCount = g.Count(),
                        TotalAmount = g.Sum(p => p.TotalAmount),
                        Parcels = g.ToList()
                    })
                    .OrderBy(g => g.Destination)
                    .ToList();

                // Get unique destinations for filtering
                var allDestinations = await _context.Parcels
                    .Where(p => validStatuses.Contains(p.Status))
                    .Where(p => p.CreatedAt.Date >= fromDate.Value.Date)
                    .Where(p => !string.IsNullOrEmpty(p.Destination))
                    .Select(p => p.Destination)
                    .Distinct()
                    .OrderBy(d => d)
                    .ToListAsync();

                // Get unique clerks for filtering
                var allClerks = await _context.Parcels
                    .Where(p => validStatuses.Contains(p.Status))
                    .Where(p => p.CreatedAt.Date >= fromDate.Value.Date)
                    .Where(p => p.CreatedBy != null)
                    .Select(p => new ClerkInfo
                    {
                        Id = p.CreatedById.HasValue ? p.CreatedById.Value : 0,
                        Username = p.CreatedBy!.Username,
                        BranchName = p.CreatedBy.Branch != null ? p.CreatedBy.Branch.Name : "Unknown"
                    })
                    .Distinct()
                    .OrderBy(c => c.Username)
                    .ToListAsync();

                var result = new MultiDestinationDispatchResult
                {
                    ParcelsByDestination = parcelsByDestination,
                    TotalParcels = totalCount,
                    TotalAmount = parcels.Sum(p => p.TotalAmount),
                    AvailableDestinations = allDestinations,
                    AvailableClerks = allClerks,
                    Pagination = new PaginationInfo
                    {
                        Page = page,
                        PageSize = pageSize,
                        TotalCount = totalCount,
                        TotalPages = totalPages,
                        HasNextPage = page < totalPages,
                        HasPreviousPage = page > 1
                    }
                };

                System.Diagnostics.Debug.WriteLine($"Retrieved {parcels.Count} parcels across {parcelsByDestination.Count} destinations");
                return Ok(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetParcelsForMultiDestinationDispatch: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get dispatch statistics for dashboard and summary views
        /// </summary>
        [HttpGet("dispatch-stats")]
        public async Task<ActionResult<DispatchStatistics>> GetDispatchStatistics(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int? userBranchId = null)
        {
            try
            {
                fromDate = fromDate ?? DateTime.UtcNow.AddDays(-30);
                toDate = toDate ?? DateTime.UtcNow;

                var query = _context.Parcels.AsQueryable();

                // Date filtering
                query = query.Where(p => p.CreatedAt.Date >= fromDate.Value.Date);
                query = query.Where(p => p.CreatedAt.Date <= toDate.Value.Date);

                // Branch filtering
                if (userBranchId.HasValue)
                {
                    query = query.Where(p => p.CreatedBy != null && p.CreatedBy.BranchId == userBranchId.Value);
                }

                // Get statistics by status
                var statsByStatus = await query
                    .GroupBy(p => p.Status)
                    .Select(g => new StatusStatistics
                    {
                        Status = g.Key.ToString(),
                        Count = g.Count(),
                        TotalAmount = g.Sum(p => p.TotalAmount)
                    })
                    .ToListAsync();

                // Get statistics by destination
                var statsByDestination = await query
                    .Where(p => !string.IsNullOrEmpty(p.Destination))
                    .GroupBy(p => p.Destination!)
                    .Select(g => new DestinationStatistics
                    {
                        Destination = g.Key,
                        Count = g.Count(),
                        TotalAmount = g.Sum(p => p.TotalAmount),
                        PendingCount = g.Count(p => p.Status == ParcelStatus.Pending),
                        ConfirmedCount = g.Count(p => p.Status == ParcelStatus.Confirmed)
                    })
                    .OrderByDescending(g => g.Count)
                    .Take(10) // Top 10 destinations
                    .ToListAsync();

                // Total statistics
                var totalParcels = await query.CountAsync();
                var totalAmount = await query.SumAsync(p => p.TotalAmount);
                var pendingParcels = await query.CountAsync(p => p.Status == ParcelStatus.Pending);
                var confirmedParcels = await query.CountAsync(p => p.Status == ParcelStatus.Confirmed);

                var statistics = new DispatchStatistics
                {
                    TotalParcels = totalParcels,
                    TotalAmount = totalAmount,
                    PendingParcels = pendingParcels,
                    ConfirmedParcels = confirmedParcels,
                    ReadyForDispatch = pendingParcels + confirmedParcels,
                    StatisticsByStatus = statsByStatus,
                    StatisticsByDestination = statsByDestination,
                    Period = new DateRange
                    {
                        From = fromDate.Value,
                        To = toDate.Value
                    }
                };

                return Ok(statistics);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetDispatchStatistics: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("clerks")]
        public async Task<ActionResult<IEnumerable<string>>> GetClerks([FromQuery] string? destination = null, [FromQuery] ParcelStatus? status = null)
        {
            try
            {
                Response.Headers.Add("Cache-Control", "public, max-age=300"); // 5 minutes cache
                
                var query = _context.Parcels
                    .Where(p => p.CreatedById != null)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(destination))
                {
                    query = query.Where(p => p.Destination == destination);
                }

                if (status.HasValue)
                {
                    query = query.Where(p => p.Status == status.Value);
                }

                var clerks = await query
                    .Select(p => p.CreatedBy!.Username)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                return Ok(clerks);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting clerks: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("batch/status")]
        public async Task<ActionResult> UpdateParcelsStatus([FromBody] BatchStatusUpdateDto statusUpdate)
        {
            try
            {
                // Convert to InTransit status (value 2)
                var targetStatus = (ParcelStatus)statusUpdate.Status;
                
                var parcels = await _context.Parcels
                    .Where(p => statusUpdate.ParcelIds.Contains(p.Id))
                    .ToListAsync();

                foreach (var parcel in parcels)
                {
                    parcel.Status = targetStatus;
                    parcel.DispatchedAt = DateTime.UtcNow; // Add dispatch timestamp
                }

                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    error = "Failed to update parcel statuses", 
                    message = ex.Message 
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Parcel>> UpdateParcel(Guid id, [FromBody] Parcel updatedParcel)
        {
            try
            {
                // Log incoming request
                System.Diagnostics.Debug.WriteLine($"Updating parcel {id}: {JsonConvert.SerializeObject(updatedParcel)}");

                // Get the existing parcel
                var existingParcel = await _context.Parcels.FindAsync(id);
                if (existingParcel == null)
                {
                    return NotFound(new { error = "Parcel not found", id = id });
                }

                // Validate the updated parcel data
                var validationErrors = ValidateParcel(updatedParcel);
                if (validationErrors.Any())
                {
                    return BadRequest(new { Errors = validationErrors });
                }

                // Update the parcel properties
                existingParcel.WaybillNumber = updatedParcel.WaybillNumber;
                existingParcel.Sender = updatedParcel.Sender;
                existingParcel.SenderTelephone = updatedParcel.SenderTelephone;
                existingParcel.Receiver = updatedParcel.Receiver;
                existingParcel.ReceiverTelephone = updatedParcel.ReceiverTelephone;
                existingParcel.Destination = updatedParcel.Destination;
                existingParcel.Description = updatedParcel.Description;
                existingParcel.Quantity = updatedParcel.Quantity;
                existingParcel.Amount = updatedParcel.Amount;
                existingParcel.TotalAmount = updatedParcel.TotalAmount;
                existingParcel.PaymentMethods = updatedParcel.PaymentMethods;
                existingParcel.Status = updatedParcel.Status;

                // Note: Don't update CreatedAt, CreatedById, or QRCode as these should remain unchanged

                // Save changes
                await _context.SaveChangesAsync();
                await _cacheInvalidation.InvalidateParcelsCache();
                if (existingParcel.CreatedById.HasValue)
                    await _cacheInvalidation.InvalidateUserDashboardCache(existingParcel.CreatedById.Value);

                System.Diagnostics.Debug.WriteLine($"Successfully updated parcel {id}");

                return Ok(existingParcel);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error updating parcel {id}: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $" Inner exception: {ex.InnerException.Message}";
                }
                System.Diagnostics.Debug.WriteLine(errorMessage);
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                return StatusCode(500, new { 
                    error = "Failed to update parcel", 
                    message = ex.Message,
                    innerException = ex.InnerException?.Message,
                    parcelId = id
                });
            }
        }

        [HttpPut("{id}/payment")]
        public async Task<ActionResult> UpdateParcelPayment(Guid id, [FromBody] UpdateParcelPaymentDto paymentUpdate)
        {
            try
            {
                // Log incoming request
                System.Diagnostics.Debug.WriteLine($"Updating payment for parcel {id}: {JsonConvert.SerializeObject(paymentUpdate)}");

                // Get the existing parcel
                var existingParcel = await _context.Parcels.FindAsync(id);
                if (existingParcel == null)
                {
                    return NotFound(new { error = "Parcel not found", id = id });
                }

                // Validate payment amount
                if (paymentUpdate.AmountPaid < 0)
                {
                    return BadRequest(new { error = "Amount paid cannot be negative" });
                }

                // Update payment fields
                existingParcel.AmountPaid = paymentUpdate.AmountPaid;
                existingParcel.TransactionCode = paymentUpdate.TransactionCode;
                existingParcel.PaymentUpdatedAt = DateTime.UtcNow;
                existingParcel.PaymentUpdatedById = paymentUpdate.UpdatedById;

                // Save changes
                await _context.SaveChangesAsync();
                await _cacheInvalidation.InvalidateParcelsCache();
                if (existingParcel.CreatedById.HasValue)
                    await _cacheInvalidation.InvalidateUserDashboardCache(existingParcel.CreatedById.Value);

                System.Diagnostics.Debug.WriteLine($"Successfully updated payment for parcel {id}");

                return Ok(new { 
                    message = "Payment updated successfully", 
                    parcelId = id,
                    amountPaid = existingParcel.AmountPaid,
                    transactionCode = existingParcel.TransactionCode,
                    paymentUpdatedAt = existingParcel.PaymentUpdatedAt
                });
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error updating payment for parcel {id}: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $" Inner exception: {ex.InnerException.Message}";
                }
                System.Diagnostics.Debug.WriteLine(errorMessage);
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                return StatusCode(500, new { 
                    error = "Failed to update payment", 
                    message = ex.Message,
                    innerException = ex.InnerException?.Message,
                    parcelId = id
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteParcel(Guid id)
        {
            try
            {
                // Log incoming request
                System.Diagnostics.Debug.WriteLine($"Attempting to delete parcel {id}");

                // Get the existing parcel
                var existingParcel = await _context.Parcels.FindAsync(id);
                if (existingParcel == null)
                {
                    return NotFound(new { error = "Parcel not found", id = id });
                }

                // Check if parcel can be deleted (business rules)
                if (existingParcel.Status == ParcelStatus.InTransit || existingParcel.Status == ParcelStatus.Delivered)
                {
                    return BadRequest(new { 
                        error = "Cannot delete parcel", 
                        reason = "Parcel is already in transit or delivered",
                        status = existingParcel.Status.ToString(),
                        id = id
                    });
                }

                // Remove the parcel
                var deletedUserId = existingParcel.CreatedById;
                _context.Parcels.Remove(existingParcel);
                await _context.SaveChangesAsync();
                await _cacheInvalidation.InvalidateParcelsCache();
                if (deletedUserId.HasValue)
                    await _cacheInvalidation.InvalidateUserDashboardCache(deletedUserId.Value);

                System.Diagnostics.Debug.WriteLine($"Successfully deleted parcel {id}");

                return Ok(new { 
                    message = "Parcel deleted successfully", 
                    id = id,
                    waybillNumber = existingParcel.WaybillNumber
                });
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error deleting parcel {id}: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $" Inner exception: {ex.InnerException.Message}";
                }
                System.Diagnostics.Debug.WriteLine(errorMessage);
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                return StatusCode(500, new { 
                    error = "Failed to delete parcel", 
                    message = ex.Message,
                    innerException = ex.InnerException?.Message,
                    parcelId = id
                });
            }
        }
    }

    public class ParcelStatusUpdateDto
    {
        public ParcelStatus Status { get; set; }
        public int? ConfirmedById { get; set; }
        public int? DeliveredById { get; set; }
    }

    public class BatchStatusUpdateDto
    {
        public List<Guid> ParcelIds { get; set; } = new();
        public ParcelStatus Status { get; set; }
    }

    public class UpdateMultipleParcelsDto
    {
        public List<string>? ParcelIds { get; set; }
        public int Status { get; set; }
    }

    public class UpdateParcelPaymentDto
    {
        public decimal AmountPaid { get; set; }
        public string? TransactionCode { get; set; }
        public int? UpdatedById { get; set; }
    }

    // Multi-destination dispatch result DTOs
    public class MultiDestinationDispatchResult
    {
        public List<DestinationGroup> ParcelsByDestination { get; set; } = new();
        public int TotalParcels { get; set; }
        public decimal TotalAmount { get; set; }
        public List<string> AvailableDestinations { get; set; } = new();
        public List<ClerkInfo> AvailableClerks { get; set; } = new();
        public PaginationInfo Pagination { get; set; } = new();
    }

    public class DestinationGroup
    {
        public string Destination { get; set; } = string.Empty;
        public int ParcelCount { get; set; }
        public decimal TotalAmount { get; set; }
        public List<Parcel> Parcels { get; set; } = new();
    }

    public class ClerkInfo
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
    }

    public class PaginationInfo
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    // Dispatch statistics DTOs
    public class DispatchStatistics
    {
        public int TotalParcels { get; set; }
        public decimal TotalAmount { get; set; }
        public int PendingParcels { get; set; }
        public int ConfirmedParcels { get; set; }
        public int ReadyForDispatch { get; set; }
        public List<StatusStatistics> StatisticsByStatus { get; set; } = new();
        public List<DestinationStatistics> StatisticsByDestination { get; set; } = new();
        public DateRange Period { get; set; } = new();
    }

    public class StatusStatistics
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class DestinationStatistics
    {
        public string Destination { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
        public int PendingCount { get; set; }
        public int ConfirmedCount { get; set; }
    }

    public class DateRange
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }

    public class CountWrapper
    {
        public int Value { get; set; }
    }

    public class ParcelPageWrapper
    {
        public List<Parcel> Parcels { get; set; } = new();
    }

    public class DoubleWrapper
    {
        public double Value { get; set; }
    }
} 