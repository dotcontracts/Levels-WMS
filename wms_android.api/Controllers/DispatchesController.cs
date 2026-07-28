using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using wms_android.shared.Models;
using wms_android.shared.Data;
using wms_android.api.Services;
using System.Security.Claims;

namespace wms_android.api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DispatchesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IDispatchSegmentService _segmentService;

        public DispatchesController(AppDbContext context, IDispatchSegmentService segmentService)
        {
            _context = context;
            _segmentService = segmentService;
        }

        [HttpPost("create")]
        public async Task<ActionResult<Dispatch>> CreateDispatch([FromBody] Dispatch dispatch)
        {
            try
            {
                // Debug logging
                Console.WriteLine($"[DispatchesController] Received dispatch: {System.Text.Json.JsonSerializer.Serialize(dispatch)}");
                Console.WriteLine($"[DispatchesController] SourceBranch: '{dispatch.SourceBranch}'");
                Console.WriteLine($"[DispatchesController] Destination: '{dispatch.Destination}'");
                Console.WriteLine($"[DispatchesController] ParcelIds count: {dispatch.ParcelIds?.Count ?? 0}");
                Console.WriteLine($"[DispatchesController] ParcelIds: {string.Join(", ", dispatch.ParcelIds ?? new List<Guid>())}");

                // Validate parcel IDs
                if (dispatch.ParcelIds == null || dispatch.ParcelIds.Count == 0)
                    return BadRequest("At least one parcel ID is required");

                // Check if parcels exist and validate their status
                var existingParcels = await _context.Parcels
                    .Where(p => dispatch.ParcelIds.Contains(p.Id))
                    .ToListAsync();

                if (existingParcels.Count != dispatch.ParcelIds.Count)
                {
                    var missingIds = dispatch.ParcelIds.Except(existingParcels.Select(p => p.Id));
                    return BadRequest($"Some parcels not found: {string.Join(", ", missingIds)}");
                }

                // Validate that all parcels are in Confirmed status (cannot dispatch Pending parcels)
                var invalidParcels = existingParcels.Where(p => p.Status != ParcelStatus.Confirmed).ToList();
                if (invalidParcels.Any())
                {
                    var invalidWaybills = invalidParcels.Select(p => $"{p.WaybillNumber} (Status: {p.Status})");
                    return BadRequest($"Only confirmed parcels can be dispatched. Invalid parcels: {string.Join(", ", invalidWaybills)}");
                }

                // Generate a new UUID for database ID (primary key)
                dispatch.Id = Guid.NewGuid();
                // Preserve DispatchCode sent from client
                // If no DispatchCode provided, use generated UUID as fallback
                if (string.IsNullOrEmpty(dispatch.DispatchCode))
                {
                    dispatch.DispatchCode = dispatch.Id.ToString();
                }
                dispatch.DispatchTime = DateTime.UtcNow;
                dispatch.Status = "in_transit";

                // Get current user for audit tracking
                var currentUserId = GetCurrentUserId();
                
                // Update existing parcels to InTransit status
                foreach (var existingParcel in existingParcels)
                {
                    existingParcel.Status = ParcelStatus.InTransit;
                    existingParcel.DispatchedAt = dispatch.DispatchTime;
                    existingParcel.DispatchTrackingCode = dispatch.DispatchCode;
                    
                    if (currentUserId.HasValue)
                    {
                        existingParcel.ConfirmedById = currentUserId.Value;
                        existingParcel.ConfirmedAt = DateTime.UtcNow;
                        existingParcel.DispatchedById = currentUserId.Value;
                    }
                    
                    Console.WriteLine($"[DispatchesController] Updated parcel {existingParcel.WaybillNumber} to InTransit status, confirmed by user {currentUserId}");
                }

                // Associate parcels with the dispatch
                dispatch.Parcels = existingParcels;

                // Group parcels by destination for multi-destination support
                var parcelsByDestination = existingParcels
                    .GroupBy(p => p.Destination)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // If single destination or no destination specified, use the provided one
                if (parcelsByDestination.Count == 1 || !string.IsNullOrEmpty(dispatch.Destination))
                {
                    var primaryDestination = !string.IsNullOrEmpty(dispatch.Destination)
                        ? dispatch.Destination
                        : parcelsByDestination.Keys.First();

                    // Set the primary destination on the dispatch
                    dispatch.Destination = primaryDestination;
                }
                else
                {
                    // Set primary destination to first destination for backward compatibility
                    dispatch.Destination = parcelsByDestination.Keys.First();
                }

                // Add dispatch to database FIRST (must exist before creating segments)
                _context.Dispatches.Add(dispatch);
                await _context.SaveChangesAsync();

                // NOW create segments after dispatch is saved
                if (parcelsByDestination.Count == 1 || !string.IsNullOrEmpty(dispatch.Destination))
                {
                    var primaryDestination = dispatch.Destination;

                    // Create a single segment for all parcels
                    await _segmentService.CreateDispatchSegmentAsync(
                        dispatch.Id,
                        primaryDestination,
                        dispatch.ParcelIds
                    );
                }
                else
                {
                    // Create segments for each destination
                    foreach (var kvp in parcelsByDestination)
                    {
                        var destination = kvp.Key;
                        var parcelIds = kvp.Value.Select(p => p.Id).ToList();

                        await _segmentService.CreateDispatchSegmentAsync(
                            dispatch.Id,
                            destination,
                            parcelIds
                        );
                    }
                }

                // Return enhanced response with segments
                var response = new
                {
                    Id = dispatch.Id,
                    DispatchCode = dispatch.DispatchCode,
                    SourceBranch = dispatch.SourceBranch,
                    Destination = dispatch.EffectiveDestination,
                    VehicleNumber = dispatch.VehicleNumber,
                    Driver = dispatch.Driver,
                    DriverPhone = dispatch.DriverPhone,
                    ParcelIds = dispatch.ParcelIds,
                    DispatchTime = dispatch.DispatchTime,
                    Status = dispatch.Status,
                    ParcelCount = dispatch.ParcelIds?.Count ?? 0,
                    TotalAmount = dispatch.TotalAmount,
                    AllDestinations = dispatch.AllDestinations
                };

                Console.WriteLine($"[DispatchesController] Returning simplified response: {System.Text.Json.JsonSerializer.Serialize(response)}");
                return CreatedAtAction(nameof(GetDispatch), new { id = dispatch.Id }, response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating dispatch: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return StatusCode(500, new { error = "Failed to create dispatch", message = ex.Message, innerException = ex.InnerException?.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Dispatch>> GetDispatch(Guid id)
        {
            var dispatch = await _context.Dispatches
                .AsNoTracking() // Remove circular reference tracking
                .FirstOrDefaultAsync(d => d.Id == id);

            if (dispatch != null)
            {
                // Load segments separately to avoid circular references
                var segments = await _context.DispatchSegments
                    .AsNoTracking()
                    .Where(ds => ds.DispatchId == id)
                    .ToListAsync();
                    
                // Load parcels for each segment separately using join table
                foreach (var segment in segments)
                {
                    var parcelIds = await _context.Database.SqlQuery<Guid>(
                        $"SELECT \"ParcelsId\" FROM \"DispatchSegmentParcels\" WHERE \"DispatchSegmentId\" = {segment.Id}")
                        .ToListAsync();
                        
                    segment.Parcels = await _context.Parcels
                        .AsNoTracking()
                        .Where(p => parcelIds.Contains(p.Id))
                        .ToListAsync();
                }
                
                dispatch.Segments = segments;
            }

            if (dispatch == null)
            {
                return NotFound();
            }
            return Ok(dispatch);
        }

        [HttpGet("{id}/segments")]
        public async Task<ActionResult<IEnumerable<DispatchSegment>>> GetDispatchSegments(Guid id)
        {
            var segments = await _segmentService.GetSegmentsByDispatchIdCachedAsync(id);
            return Ok(segments);
        }

        [HttpGet("branch/{branch}")]
        public async Task<ActionResult<IEnumerable<Dispatch>>> GetDispatchesByBranch(string branch)
        {
            Console.WriteLine($"[DispatchesController] GetDispatchesByBranch called with branch: '{branch}'");
            
            // Get all dispatches first to debug
            var allDispatches = await _context.Dispatches.ToListAsync();
            Console.WriteLine($"[DispatchesController] Total dispatches in DB: {allDispatches.Count}");
            
            foreach (var d in allDispatches.Take(5)) // Log first 5 for debugging
            {
                Console.WriteLine($"[DispatchesController] Dispatch ID: {d.Id}, SourceBranch: '{d.SourceBranch}', Destination: '{d.Destination}', DispatchCode: '{d.DispatchCode}'");
            }
            
            var branchLower = branch.ToLower();
            var dispatches = allDispatches
                .Where(d => (!string.IsNullOrEmpty(d.SourceBranch) && d.SourceBranch.ToLower() == branchLower) ||
                            (!string.IsNullOrEmpty(d.Destination) && d.Destination.ToLower() == branchLower))
                .OrderByDescending(d => d.DispatchTime)
                .ToList();

            Console.WriteLine($"[DispatchesController] Filtered dispatches for branch '{branch}': {dispatches.Count}");
            return Ok(dispatches);
        }

        [HttpGet("{id}/note")]
        public async Task<ActionResult<DispatchNote>> GetDispatchNote(Guid id)
        {
            // Load dispatch without circular references
            var dispatch = await _context.Dispatches
                .AsNoTracking() // Remove circular reference tracking
                .FirstOrDefaultAsync(d => d.Id == id);
                
            if (dispatch != null)
            {
                // Load parcels separately to avoid circular references using join table
                var parcelIds = await _context.Database.SqlQuery<Guid>(
                    $"SELECT \"ParcelsId\" FROM \"DispatchParcels\" WHERE \"DispatchId\" = {dispatch.Id}")
                    .ToListAsync();
                    
                dispatch.Parcels = await _context.Parcels
                    .AsNoTracking()
                    .Where(p => parcelIds.Contains(p.Id))
                    .ToListAsync();
            }

            if (dispatch == null)
            {
                return NotFound();
            }

            // Explicitly load parcels if not loaded
            if (dispatch.Parcels == null || dispatch.Parcels.Count == 0)
            {
                if (dispatch.ParcelIds != null && dispatch.ParcelIds.Count > 0)
                {
                    dispatch.Parcels = await _context.Parcels
                        .Where(p => dispatch.ParcelIds.Contains(p.Id))
                        .ToListAsync();
                }
                else
                {
                    dispatch.Parcels = new List<Parcel>();
                }
            }

            var note = new DispatchNote
            {
                DispatchId = dispatch.Id,
                DispatchCode = dispatch.DispatchCode, // Include the custom dispatch code
                SourceBranch = dispatch.SourceBranch,
                VehicleNumber = dispatch.VehicleNumber,
                Driver = dispatch.Driver,
                DriverPhone = dispatch.DriverPhone,
                DispatchTime = dispatch.DispatchTime,
                Parcels = dispatch.Parcels
            };

            return Ok(note);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Dispatch>>> GetAllDispatches()
        {
            try
            {
                var dispatches = await _context.Dispatches
                    .OrderByDescending(d => d.DispatchTime)
                    .ToListAsync();
                return Ok(dispatches);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve dispatches", message = ex.Message });
            }
        }
        
        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }
            return null;
        }
    }

    public class DispatchNote
    {
        public Guid DispatchId { get; set; }
        public string? DispatchCode { get; set; } // Add the custom dispatch code
        public string SourceBranch { get; set; }
        public string VehicleNumber { get; set; }
        public string Driver { get; set; }
        public string? DriverPhone { get; set; }
        public DateTime DispatchTime { get; set; }
        public List<Parcel> Parcels { get; set; }
    }

    // DTO for creating a dispatch
    public class CreateDispatchDto
    {
        public string? SourceBranch { get; set; }
        public string? VehicleNumber { get; set; }
        public string? Driver { get; set; }
        public string? DriverPhone { get; set; }
        public string? Destination { get; set; }
        public List<Guid>? Parcels { get; set; }
    }
} 