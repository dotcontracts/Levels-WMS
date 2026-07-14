using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using wms_android.shared.Data;
using wms_android.shared.DTOs;
using wms_android.shared.Models;

namespace wms_android.api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ParcelDepositsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ParcelDepositsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/ParcelDeposits
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ParcelDepositDto>>> GetParcelDeposits(
            [FromQuery] DateTime? date = null,
            [FromQuery] int? clerkId = null,
            [FromQuery] string? destination = null)
        {
            var query = _context.ParcelDeposits
                .Include(pd => pd.Parcel)
                .Include(pd => pd.CreatedBy)
                .Include(pd => pd.UpdatedBy)
                .AsQueryable();

            // Filter by date if specified
            if (date.HasValue)
            {
                var filterDate = date.Value.Date;
                query = query.Where(pd => pd.Parcel.CreatedAt.Date == filterDate);
            }

            // Filter by clerk if specified
            if (clerkId.HasValue)
            {
                query = query.Where(pd => pd.Parcel.CreatedById == clerkId.Value);
            }

            // Filter by destination if specified
            if (!string.IsNullOrEmpty(destination))
            {
                query = query.Where(pd => pd.Parcel.Destination.ToLower() == destination.ToLower());
            }

            var deposits = await query
                .OrderBy(pd => pd.Parcel.CreatedById)
                .ThenBy(pd => pd.CreatedAt)
                .ToListAsync();

            var result = deposits.Select(pd => new ParcelDepositDto
            {
                Id = pd.Id,
                ParcelId = pd.ParcelId,
                WaybillNumber = pd.Parcel?.WaybillNumber,
                DepositedAmount = pd.DepositedAmount,
                Expenses = pd.Expenses,
                Notes = pd.Notes,
                CreatedAt = pd.CreatedAt,
                UpdatedAt = pd.UpdatedAt,
                CreatedById = pd.CreatedById,
                CreatedByName = pd.CreatedBy != null ? $"{pd.CreatedBy.FirstName} {pd.CreatedBy.LastName}" : null,
                UpdatedById = pd.UpdatedById,
                UpdatedByName = pd.UpdatedBy != null ? $"{pd.UpdatedBy.FirstName} {pd.UpdatedBy.LastName}" : null,
                RemainingDebt = pd.RemainingDebt,
                ParcelTotalAmount = pd.Parcel?.TotalAmount ?? 0,
                ParcelSender = pd.Parcel?.Sender,
                ParcelReceiver = pd.Parcel?.Receiver,
                ParcelDestination = pd.Parcel?.Destination
            });

            return Ok(result);
        }

        // GET: api/ParcelDeposits/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ParcelDepositDto>> GetParcelDeposit(int id)
        {
            var deposit = await _context.ParcelDeposits
                .Include(pd => pd.Parcel)
                .Include(pd => pd.CreatedBy)
                .Include(pd => pd.UpdatedBy)
                .FirstOrDefaultAsync(pd => pd.Id == id);

            if (deposit == null)
            {
                return NotFound();
            }

            var result = new ParcelDepositDto
            {
                Id = deposit.Id,
                ParcelId = deposit.ParcelId,
                WaybillNumber = deposit.Parcel?.WaybillNumber,
                DepositedAmount = deposit.DepositedAmount,
                Expenses = deposit.Expenses,
                Notes = deposit.Notes,
                CreatedAt = deposit.CreatedAt,
                UpdatedAt = deposit.UpdatedAt,
                CreatedById = deposit.CreatedById,
                CreatedByName = deposit.CreatedBy != null ? $"{deposit.CreatedBy.FirstName} {deposit.CreatedBy.LastName}" : null,
                UpdatedById = deposit.UpdatedById,
                UpdatedByName = deposit.UpdatedBy != null ? $"{deposit.UpdatedBy.FirstName} {deposit.UpdatedBy.LastName}" : null,
                RemainingDebt = deposit.RemainingDebt,
                ParcelTotalAmount = deposit.Parcel?.TotalAmount ?? 0,
                ParcelSender = deposit.Parcel?.Sender,
                ParcelReceiver = deposit.Parcel?.Receiver,
                ParcelDestination = deposit.Parcel?.Destination
            };

            return result;
        }

        // GET: api/ParcelDeposits/parcel/{parcelId}
        [HttpGet("parcel/{parcelId}")]
        public async Task<ActionResult<ParcelDepositDto>> GetParcelDepositByParcelId(Guid parcelId)
        {
            var deposit = await _context.ParcelDeposits
                .Include(pd => pd.Parcel)
                .Include(pd => pd.CreatedBy)
                .Include(pd => pd.UpdatedBy)
                .FirstOrDefaultAsync(pd => pd.ParcelId == parcelId);

            if (deposit == null)
            {
                return NotFound();
            }

            var result = new ParcelDepositDto
            {
                Id = deposit.Id,
                ParcelId = deposit.ParcelId,
                WaybillNumber = deposit.Parcel?.WaybillNumber,
                DepositedAmount = deposit.DepositedAmount,
                Expenses = deposit.Expenses,
                Notes = deposit.Notes,
                CreatedAt = deposit.CreatedAt,
                UpdatedAt = deposit.UpdatedAt,
                CreatedById = deposit.CreatedById,
                CreatedByName = deposit.CreatedBy != null ? $"{deposit.CreatedBy.FirstName} {deposit.CreatedBy.LastName}" : null,
                UpdatedById = deposit.UpdatedById,
                UpdatedByName = deposit.UpdatedBy != null ? $"{deposit.UpdatedBy.FirstName} {deposit.UpdatedBy.LastName}" : null,
                RemainingDebt = deposit.RemainingDebt,
                ParcelTotalAmount = deposit.Parcel?.TotalAmount ?? 0,
                ParcelSender = deposit.Parcel?.Sender,
                ParcelReceiver = deposit.Parcel?.Receiver,
                ParcelDestination = deposit.Parcel?.Destination
            };

            return result;
        }

        // GET: api/ParcelDeposits/clerk-summary
        [HttpGet("clerk-summary")]
        public async Task<ActionResult<IEnumerable<ClerkCashInSummaryDto>>> GetClerkCashInSummary(
            [FromQuery] DateTime? date = null,
            [FromQuery] string? destination = null)
        {
            var query = _context.ParcelDeposits
                .Include(pd => pd.Parcel)
                .ThenInclude(p => p.CreatedBy)
                .AsQueryable();

            // Filter by date if specified
            if (date.HasValue)
            {
                var filterDate = date.Value.Date;
                query = query.Where(pd => pd.Parcel.CreatedAt.Date == filterDate);
            }

            // Filter by destination if specified
            if (!string.IsNullOrEmpty(destination))
            {
                query = query.Where(pd => pd.Parcel.Destination.ToLower() == destination.ToLower());
            }

            // Only include paid parcels
            query = query.Where(pd => pd.Parcel.PaymentMethods.ToLower().Contains("paid"));

            var deposits = await query.ToListAsync();

            var clerkSummaries = deposits
                .GroupBy(pd => new { 
                    ClerkId = pd.Parcel.CreatedById ?? 0,
                    ClerkName = pd.Parcel.CreatedBy != null ? $"{pd.Parcel.CreatedBy.FirstName} {pd.Parcel.CreatedBy.LastName}" : "Unknown",
                    ClerkUsername = pd.Parcel.CreatedBy?.Username ?? "unknown"
                })
                .Select(g => new ClerkCashInSummaryDto
                {
                    ClerkId = g.Key.ClerkId,
                    ClerkName = g.Key.ClerkName,
                    ClerkUsername = g.Key.ClerkUsername,
                    TotalPaidAmount = g.Sum(pd => pd.Parcel.TotalAmount),
                    TotalDeposited = g.Sum(pd => pd.DepositedAmount),
                    TotalExpenses = g.Sum(pd => pd.Expenses),
                    RemainingDebt = g.Sum(pd => pd.RemainingDebt),
                    ParcelCount = g.Count(),
                    LastUpdateDate = g.Max(pd => pd.UpdatedAt)
                })
                .OrderByDescending(cs => cs.TotalPaidAmount)
                .ToList();

            return Ok(clerkSummaries);
        }

        // GET: api/ParcelDeposits/clerk-summary/user/{userId}
        [HttpGet("clerk-summary/user/{userId}")]
        public async Task<ActionResult<ClerkCashInSummaryDto>> GetClerkCashInSummaryForUser(
            int userId,
            [FromQuery] DateTime? date = null)
        {
            var queryDate = date ?? DateTime.Today;
            
            // Calculate the date range: previous month + first 7 days of current month
            var startOfCurrentMonth = new DateTime(queryDate.Year, queryDate.Month, 1);
            var startOfPreviousMonth = startOfCurrentMonth.AddMonths(-1);
            var endOfRange = startOfCurrentMonth.AddDays(7);
            
            DateTime startDate, endDate;
            
            // Check if we should show current month only (if current date is past 7th of current month)
            if (queryDate >= startOfCurrentMonth.AddDays(7))
            {
                // Show current month from beginning if we're past the 7-day grace period
                startDate = startOfCurrentMonth;
                endDate = startOfCurrentMonth.AddMonths(1); // End of current month
            }
            else
            {
                // Show previous month + 7 days of current month
                startDate = startOfPreviousMonth;
                endDate = endOfRange;
            }

            // Get user info first
            var userInfo = await _context.Users.FindAsync(userId);
            if (userInfo == null)
            {
                return NotFound(new { message = "User not found." });
            }

            // Query parcels directly (like React component does) instead of requiring ParcelDeposits
            // Only include paid parcels (like React component)
            var parcelsQuery = _context.Parcels
                .Where(p => p.CreatedById == userId && 
                           p.CreatedAt >= startDate &&
                           p.CreatedAt < endDate &&
                           p.PaymentMethods.ToLower().Contains("paid"));

            var parcels = await parcelsQuery.ToListAsync();

            if (!parcels.Any())
            {
                // Return summary with zero values if no parcels exist for this user in the date range
                return Ok(new ClerkCashInSummaryDto
                {
                    ClerkId = userId,
                    ClerkName = $"{userInfo.FirstName} {userInfo.LastName}",
                    ClerkUsername = userInfo.Username ?? "unknown",
                    TotalPaidAmount = 0,
                    TotalDeposited = 0,
                    TotalExpenses = 0,
                    RemainingDebt = 0,
                    ParcelCount = 0,
                    LastUpdateDate = null
                });
            }

            // Calculate totals from parcels (like React component)
            decimal totalPaidAmount = parcels.Sum(p => p.TotalAmount);
            
            // Get corresponding ParcelDeposits if they exist for these parcels
            var parcelIds = parcels.Select(p => p.Id).ToList();
            var deposits = await _context.ParcelDeposits
                .Where(pd => parcelIds.Contains(pd.ParcelId))
                .ToListAsync();

            // Calculate deposited and expenses from ParcelDeposits
            decimal totalDeposited = deposits.Sum(pd => pd.DepositedAmount);
            decimal totalExpenses = deposits.Sum(pd => pd.Expenses);

            // Calculate remaining debt: total collected minus (deposits + expenses)
            // remainingDebt = totalPaidAmount - (totalDeposited + totalExpenses)
            decimal remainingDebt = totalPaidAmount - (totalDeposited + totalExpenses);

            var summary = new ClerkCashInSummaryDto
            {
                ClerkId = userId,
                ClerkName = $"{userInfo.FirstName} {userInfo.LastName}",
                ClerkUsername = userInfo.Username ?? "unknown",
                TotalPaidAmount = totalPaidAmount,
                TotalDeposited = totalDeposited,
                TotalExpenses = totalExpenses,
                RemainingDebt = remainingDebt,
                ParcelCount = parcels.Count(),
                LastUpdateDate = deposits.Any() ? deposits.Max(pd => pd.UpdatedAt) : null
            };

            return Ok(summary);
        }

        // POST: api/ParcelDeposits
        [HttpPost]
        public async Task<ActionResult<ParcelDepositDto>> CreateParcelDeposit(CreateParcelDepositDto createDto)
        {
            // Check if deposit already exists for this parcel
            var existingDeposit = await _context.ParcelDeposits
                .FirstOrDefaultAsync(pd => pd.ParcelId == createDto.ParcelId);

            if (existingDeposit != null)
            {
                return Conflict(new { message = "A deposit record already exists for this parcel." });
            }

            // Verify parcel exists
            var parcel = await _context.Parcels.FindAsync(createDto.ParcelId);
            if (parcel == null)
            {
                return NotFound(new { message = "Parcel not found." });
            }

            var deposit = new ParcelDeposit
            {
                ParcelId = createDto.ParcelId,
                DepositedAmount = createDto.DepositedAmount,
                Expenses = createDto.Expenses,
                Notes = createDto.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedById = createDto.CreatedById ?? parcel.CreatedById, // Use parcel creator if not specified
                UpdatedById = GetCurrentUserId() // Current user is the one creating the deposit record
            };

            _context.ParcelDeposits.Add(deposit);
            await _context.SaveChangesAsync();

            // Load the full deposit with related data
            var createdDeposit = await _context.ParcelDeposits
                .Include(pd => pd.Parcel)
                .Include(pd => pd.CreatedBy)
                .Include(pd => pd.UpdatedBy)
                .FirstOrDefaultAsync(pd => pd.Id == deposit.Id);

            var result = new ParcelDepositDto
            {
                Id = createdDeposit.Id,
                ParcelId = createdDeposit.ParcelId,
                WaybillNumber = createdDeposit.Parcel?.WaybillNumber,
                DepositedAmount = createdDeposit.DepositedAmount,
                Expenses = createdDeposit.Expenses,
                Notes = createdDeposit.Notes,
                CreatedAt = createdDeposit.CreatedAt,
                UpdatedAt = createdDeposit.UpdatedAt,
                CreatedById = createdDeposit.CreatedById,
                CreatedByName = createdDeposit.CreatedBy != null ? $"{createdDeposit.CreatedBy.FirstName} {createdDeposit.CreatedBy.LastName}" : null,
                UpdatedById = createdDeposit.UpdatedById,
                UpdatedByName = createdDeposit.UpdatedBy != null ? $"{createdDeposit.UpdatedBy.FirstName} {createdDeposit.UpdatedBy.LastName}" : null,
                RemainingDebt = createdDeposit.RemainingDebt,
                ParcelTotalAmount = createdDeposit.Parcel?.TotalAmount ?? 0,
                ParcelSender = createdDeposit.Parcel?.Sender,
                ParcelReceiver = createdDeposit.Parcel?.Receiver,
                ParcelDestination = createdDeposit.Parcel?.Destination
            };

            return CreatedAtAction(nameof(GetParcelDeposit), new { id = deposit.Id }, result);
        }

        // PUT: api/ParcelDeposits/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateParcelDeposit(int id, UpdateParcelDepositDto updateDto)
        {
            var deposit = await _context.ParcelDeposits.FindAsync(id);

            if (deposit == null)
            {
                return NotFound();
            }

            deposit.DepositedAmount = updateDto.DepositedAmount;
            deposit.Expenses = updateDto.Expenses;
            deposit.Notes = updateDto.Notes;
            deposit.UpdatedAt = DateTime.UtcNow;
            deposit.UpdatedById = updateDto.UpdatedById ?? GetCurrentUserId();

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ParcelDepositExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // PUT: api/ParcelDeposits/parcel/{parcelId}
        [HttpPut("parcel/{parcelId}")]
        public async Task<ActionResult<ParcelDepositDto>> UpdateOrCreateParcelDeposit(Guid parcelId, UpdateParcelDepositDto updateDto)
        {
            var deposit = await _context.ParcelDeposits
                .FirstOrDefaultAsync(pd => pd.ParcelId == parcelId);

            if (deposit == null)
            {
                // Get the original parcel to use its CreatedById
                var parcel = await _context.Parcels.FindAsync(parcelId);
                if (parcel == null)
                {
                    return NotFound(new { message = "Parcel not found." });
                }

                // Create new deposit - use original parcel creator as CreatedById
                var createDto = new CreateParcelDepositDto
                {
                    ParcelId = parcelId,
                    DepositedAmount = updateDto.DepositedAmount,
                    Expenses = updateDto.Expenses,
                    Notes = updateDto.Notes,
                    CreatedById = parcel.CreatedById // Use original parcel creator, not current user
                };

                return await CreateParcelDeposit(createDto);
            }
            else
            {
                // Update existing deposit - only update UpdatedById, keep original CreatedById
                deposit.DepositedAmount = updateDto.DepositedAmount;
                deposit.Expenses = updateDto.Expenses;
                deposit.Notes = updateDto.Notes;
                deposit.UpdatedAt = DateTime.UtcNow;
                deposit.UpdatedById = updateDto.UpdatedById ?? GetCurrentUserId();
                // NOTE: CreatedById remains unchanged - it should always be the original parcel creator

                await _context.SaveChangesAsync();

                // Return updated deposit
                return await GetParcelDeposit(deposit.Id);
            }
        }

        // DELETE: api/ParcelDeposits/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteParcelDeposit(int id)
        {
            var deposit = await _context.ParcelDeposits.FindAsync(id);
            if (deposit == null)
            {
                return NotFound();
            }

            _context.ParcelDeposits.Remove(deposit);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ParcelDepositExists(int id)
        {
            return _context.ParcelDeposits.Any(e => e.Id == id);
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
}