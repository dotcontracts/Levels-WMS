using Microsoft.AspNetCore.Mvc;
using wms_android.shared.Models;
using wms_android.shared.Interfaces;
using wms_android.shared.Services;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;
using wms_android.shared.Data;

namespace wms_android.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ParcelsController : ControllerBase
    {
        private readonly IParcelService _parcelService;
        private readonly AppDbContext _context;

        public ParcelsController(IParcelService parcelService, AppDbContext context)
        {
            _parcelService = parcelService;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetParcels([FromQuery] string? branchName = null)
        {
            try
            {
                var query = _context.Parcels.AsQueryable();
                
                // If branchName is provided, filter by destination column
                if (!string.IsNullOrEmpty(branchName))
                {
                    query = query.Where(p => p.Destination == branchName);
                }
                
                var parcels = await query.ToListAsync();
                return Ok(parcels);
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Enhanced paginated endpoint with efficient database-level filtering
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
                pageSize = Math.Clamp(pageSize, 1, 500); // Max 500 items per page

                var query = _context.Parcels
                    .Include(p => p.CreatedBy)
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

                // Get total count before pagination
                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                // Apply pagination and ordering
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

                System.Diagnostics.Debug.WriteLine($"Retrieved page {page} with {parcels.Count} parcels out of {totalCount} total");
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
            [FromQuery] string? search = null)
        {
            try
            {
                // Validate pagination parameters
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 500);

                var query = _context.Parcels
                    .Include(p => p.CreatedBy)
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

                // Ensure CreatedAt is set
                parcel.CreatedAt = DateTime.UtcNow;
                
                // Ensure Status is set
                if (parcel.Status == 0)
                {
                    parcel.Status = ParcelStatus.Pending;
                }

                // Note: CreatedById is now part of the Parcel model and should be passed in the request
                // If it's not set, it will be null in the database, which is allowed

                var created = await _parcelService.CreateParcelAsync(parcel);
                
                if (created == null)
                {
                    throw new Exception("Failed to create parcel - service returned null");
                }

                // Log created parcel
                System.Diagnostics.Debug.WriteLine($"Created parcel: {JsonConvert.SerializeObject(created)}");
                
                return CreatedAtAction(nameof(GetParcel), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error creating parcel: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $" Inner exception: {ex.InnerException.Message}";
                }
                System.Diagnostics.Debug.WriteLine(errorMessage);
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                return StatusCode(500, new { 
                    error = "Internal server error", 
                    message = ex.Message,
                    innerException = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace 
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
                await _parcelService.UpdateParcelStatusAsync(id, statusUpdate.Status);
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
                var amountOwed = await _parcelService.GetAmountOwedByUserAsync(userId, date);
                return Ok(amountOwed);
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
                var cashIn = await _parcelService.GetCashInByUserAsync(userId, date);
                return Ok(cashIn);
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
                var dailySales = await _parcelService.GetDailySalesByUserAsync(userId, date);
                return Ok(dailySales);
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
                var monthlySales = await _parcelService.GetMonthlySalesByUserAsync(userId, date);
                return Ok(monthlySales);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting monthly sales for user {userId}: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("dispatch")]
        public async Task<ActionResult<IEnumerable<Parcel>>> GetParcelsForDispatch([FromQuery] string? destination = null, 
            [FromQuery] string? statuses = null, [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null, 
            [FromQuery] string? createdByUsername = null)
        {
            try
            {
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

                // Date filtering - limit to 2 days by default
                if (fromDate.HasValue)
                {
                    query = query.Where(p => p.CreatedAt.Date >= fromDate.Value.Date);
                }
                if (toDate.HasValue)
                {
                    query = query.Where(p => p.CreatedAt.Date <= toDate.Value.Date);
                }

                // Username filtering
                if (!string.IsNullOrEmpty(createdByUsername))
                {
                    query = query.Where(p => p.CreatedBy != null && p.CreatedBy.Username == createdByUsername);
                }

                // Get all parcels (no pagination needed for 2-day range)
                var parcels = await query
                    .OrderByDescending(p => p.CreatedAt)
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

        [HttpGet("clerks")]
        public async Task<ActionResult<IEnumerable<string>>> GetClerks([FromQuery] string? destination = null, [FromQuery] ParcelStatus? status = null)
        {
            try
            {
                Response.Headers.Add("Cache-Control", "public, max-age=300"); // 5 minutes cache
                
                var query = _context.Parcels
                    .Include(p => p.CreatedBy)
                    .Where(p => p.CreatedBy != null)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(destination))
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

                // Log success
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

                // Log success
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
                _context.Parcels.Remove(existingParcel);
                await _context.SaveChangesAsync();

                // Log success
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
} 