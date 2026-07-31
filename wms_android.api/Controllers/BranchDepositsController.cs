using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using wms_android.shared.Data;
using wms_android.shared.DTOs;
using wms_android.shared.Models;
using System.Linq;

namespace wms_android.api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BranchDepositsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BranchDepositsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/BranchDeposits
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BranchDepositDto>>> GetBranchDeposits(
            [FromQuery] string? branch = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var query = _context.BranchDeposits
                .Include(bd => bd.CreatedBy)
                .Include(bd => bd.ApprovedBy)
                .AsQueryable();

            if (!string.IsNullOrEmpty(branch))
            {
                query = query.Where(bd => bd.Branch == branch);
            }

            if (startDate.HasValue)
            {
                query = query.Where(bd => bd.Date >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                query = query.Where(bd => bd.Date <= endDate.Value.Date);
            }

            var branchDeposits = await query
                .OrderBy(bd => bd.Branch)
                .ThenBy(bd => bd.Date)
                .ToListAsync();

            var result = branchDeposits.Select(bd => new BranchDepositDto
            {
                Id = bd.Id,
                Branch = bd.Branch,
                Date = bd.Date,
                CodTotal = bd.CodTotal,
                DepositAmount = bd.DepositAmount,
                RunningDebt = bd.RunningDebt,
                Status = bd.Status,
                RejectionReason = bd.RejectionReason,
                ApprovedById = bd.ApprovedById,
                ApprovedByName = bd.ApprovedBy != null ? $"{bd.ApprovedBy.FirstName} {bd.ApprovedBy.LastName}" : null,
                ApprovedAt = bd.ApprovedAt,
                CreatedAt = bd.CreatedAt,
                UpdatedAt = bd.UpdatedAt,
                CreatedById = bd.CreatedById,
                CreatedByName = bd.CreatedBy != null ? $"{bd.CreatedBy.FirstName} {bd.CreatedBy.LastName}" : null
            });

            return Ok(result);
        }

        // GET: api/BranchDeposits/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<BranchDepositDto>> GetBranchDeposit(int id)
        {
            var branchDeposit = await _context.BranchDeposits
                .Include(bd => bd.CreatedBy)
                .Include(bd => bd.ApprovedBy)
                .FirstOrDefaultAsync(bd => bd.Id == id);

            if (branchDeposit == null)
            {
                return NotFound();
            }

            var result = new BranchDepositDto
            {
                Id = branchDeposit.Id,
                Branch = branchDeposit.Branch,
                Date = branchDeposit.Date,
                CodTotal = branchDeposit.CodTotal,
                DepositAmount = branchDeposit.DepositAmount,
                RunningDebt = branchDeposit.RunningDebt,
                Status = branchDeposit.Status,
                RejectionReason = branchDeposit.RejectionReason,
                ApprovedById = branchDeposit.ApprovedById,
                ApprovedByName = branchDeposit.ApprovedBy != null ? $"{branchDeposit.ApprovedBy.FirstName} {branchDeposit.ApprovedBy.LastName}" : null,
                ApprovedAt = branchDeposit.ApprovedAt,
                CreatedAt = branchDeposit.CreatedAt,
                UpdatedAt = branchDeposit.UpdatedAt,
                CreatedById = branchDeposit.CreatedById,
                CreatedByName = branchDeposit.CreatedBy != null ? $"{branchDeposit.CreatedBy.FirstName} {branchDeposit.CreatedBy.LastName}" : null
            };

            return result;
        }

        // GET: api/BranchDeposits/summary
        [HttpGet("summary")]
        public async Task<ActionResult<IEnumerable<BranchDepositSummaryDto>>> GetBranchDepositSummary(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var query = _context.BranchDeposits.AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(bd => bd.Date >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                query = query.Where(bd => bd.Date <= endDate.Value.Date);
            }

            var summary = await query
                .GroupBy(bd => bd.Branch)
                .Select(g => new BranchDepositSummaryDto
                {
                    Branch = g.Key,
                    TotalCod = g.Sum(bd => bd.CodTotal),
                    TotalDeposits = g.Sum(bd => bd.DepositAmount),
                    TotalDebt = g.Sum(bd => bd.RunningDebt),
                    RecordCount = g.Count(),
                    LastDepositDate = g.Max(bd => bd.Date)
                })
                .OrderBy(s => s.Branch)
                .ToListAsync();

            return Ok(summary);
        }
        // PUT: api/BranchDeposits/{id}/approve
        [HttpPut("{id}/approve")]
        public async Task<IActionResult> ApproveBranchDeposit(int id)
        {
            var branchDeposit = await _context.BranchDeposits.FindAsync(id);
            if (branchDeposit == null) return NotFound();

            branchDeposit.Status = "approved";
            branchDeposit.RejectionReason = null;
            branchDeposit.ApprovedById = GetCurrentUserId();
            branchDeposit.ApprovedAt = DateTime.UtcNow;
            branchDeposit.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // PUT: api/BranchDeposits/{id}/reject
        [HttpPut("{id}/reject")]
        public async Task<IActionResult> RejectBranchDeposit(int id, ApproveBranchDepositDto dto)
        {
            var branchDeposit = await _context.BranchDeposits.FindAsync(id);
            if (branchDeposit == null) return NotFound();

            branchDeposit.Status = "rejected";
            branchDeposit.RejectionReason = dto.RejectionReason;
            branchDeposit.ApprovedById = GetCurrentUserId();
            branchDeposit.ApprovedAt = DateTime.UtcNow;
            branchDeposit.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // POST: api/BranchDeposits
        [HttpPost]
        public async Task<ActionResult<BranchDepositDto>> CreateBranchDeposit(CreateBranchDepositDto createDto)
        {
            // Check if a record already exists for this branch and date
            var existingRecord = await _context.BranchDeposits
                .FirstOrDefaultAsync(bd => bd.Branch == createDto.Branch && bd.Date.Date == createDto.Date.Date);

            if (existingRecord != null)
            {
                return Conflict(new { message = "A deposit record already exists for this branch and date." });
            }

            // Calculate running debt by getting the previous balance and adding the new debt
            var runningDebt = await CalculateRunningDebt(createDto.Branch, createDto.Date, createDto.CodTotal, createDto.DepositAmount);

            var branchDeposit = new BranchDeposit
            {
                Branch = createDto.Branch,
                Date = createDto.Date.Date,
                CodTotal = createDto.CodTotal,
                DepositAmount = createDto.DepositAmount,
                RunningDebt = runningDebt,
                Status = "pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedById = GetCurrentUserId()
            };

            _context.BranchDeposits.Add(branchDeposit);
            await _context.SaveChangesAsync();

            // Recalculate running debt for all subsequent records
            await RecalculateSubsequentRunningDebts(createDto.Branch, createDto.Date);

            var result = new BranchDepositDto
            {
                Id = branchDeposit.Id,
                Branch = branchDeposit.Branch,
                Date = branchDeposit.Date,
                CodTotal = branchDeposit.CodTotal,
                DepositAmount = branchDeposit.DepositAmount,
                RunningDebt = branchDeposit.RunningDebt,
                CreatedAt = branchDeposit.CreatedAt,
                UpdatedAt = branchDeposit.UpdatedAt,
                CreatedById = branchDeposit.CreatedById
            };

            return CreatedAtAction(nameof(GetBranchDeposit), new { id = branchDeposit.Id }, result);
        }

        // PUT: api/BranchDeposits/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBranchDeposit(int id, UpdateBranchDepositDto updateDto)
        {
            var branchDeposit = await _context.BranchDeposits.FindAsync(id);

            if (branchDeposit == null)
            {
                return NotFound();
            }

            branchDeposit.CodTotal = updateDto.CodTotal;
            branchDeposit.DepositAmount = updateDto.DepositAmount;
            branchDeposit.UpdatedAt = DateTime.UtcNow;
            branchDeposit.Status = "pending";
            branchDeposit.RejectionReason = null;

            // Recalculate running debt for this record and all subsequent records
            var runningDebt = await CalculateRunningDebt(branchDeposit.Branch, branchDeposit.Date, updateDto.CodTotal, updateDto.DepositAmount);
            branchDeposit.RunningDebt = runningDebt;

            try
            {
                await _context.SaveChangesAsync();
                
                // Recalculate running debt for all subsequent records
                await RecalculateSubsequentRunningDebts(branchDeposit.Branch, branchDeposit.Date);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BranchDepositExists(id))
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

        // DELETE: api/BranchDeposits/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBranchDeposit(int id)
        {
            var branchDeposit = await _context.BranchDeposits.FindAsync(id);
            if (branchDeposit == null)
            {
                return NotFound();
            }

            var branch = branchDeposit.Branch;
            var date = branchDeposit.Date;

            _context.BranchDeposits.Remove(branchDeposit);
            await _context.SaveChangesAsync();

            // Recalculate running debt for all subsequent records
            await RecalculateSubsequentRunningDebts(branch, date);

            return NoContent();
        }

        private bool BranchDepositExists(int id)
        {
            return _context.BranchDeposits.Any(e => e.Id == id);
        }

        private async Task<decimal> CalculateRunningDebt(string branch, DateTime date, decimal codTotal, decimal depositAmount)
        {
            // Get the previous running debt (latest record before this date for the same branch)
            var previousRecord = await _context.BranchDeposits
                .Where(bd => bd.Branch == branch && bd.Date < date.Date)
                .OrderByDescending(bd => bd.Date)
                .FirstOrDefaultAsync();

            var previousDebt = previousRecord?.RunningDebt ?? 0;
            return previousDebt + codTotal - depositAmount;
        }

        private async Task RecalculateSubsequentRunningDebts(string branch, DateTime fromDate)
        {
            // Get all records for this branch from the given date onwards, ordered by date
            var subsequentRecords = await _context.BranchDeposits
                .Where(bd => bd.Branch == branch && bd.Date >= fromDate.Date)
                .OrderBy(bd => bd.Date)
                .ToListAsync();

            if (!subsequentRecords.Any()) return;

            // Get the previous running debt (latest record before fromDate for the same branch)
            var previousRecord = await _context.BranchDeposits
                .Where(bd => bd.Branch == branch && bd.Date < fromDate.Date)
                .OrderByDescending(bd => bd.Date)
                .FirstOrDefaultAsync();

            var runningDebt = previousRecord?.RunningDebt ?? 0;

            // Recalculate running debt for each subsequent record
            foreach (var record in subsequentRecords)
            {
                runningDebt = runningDebt + record.CodTotal - record.DepositAmount;
                record.RunningDebt = runningDebt;
                record.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
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