using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using wms_android.shared.Data;
using wms_android.shared.Models;
using wms_android.shared.DTOs;

namespace wms_android.api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PaymentsController(AppDbContext context)
        {
            _context = context;
        }

        // COD Collections Endpoints

        // GET: api/Payments/cod-collections
        [HttpGet("cod-collections")]
        public async Task<ActionResult<IEnumerable<CODCollection>>> GetCODCollections()
        {
            try
            {
                var codCollections = await _context.CODCollections
                    .Include(c => c.Dispatch)
                    .Include(c => c.Branch)
                    .Include(c => c.CreatedBy)
                    .Include(c => c.UpdatedBy)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();

                return Ok(codCollections);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to retrieve COD collections", error = ex.Message });
            }
        }

        // GET: api/Payments/cod-collections/5
        [HttpGet("cod-collections/{id}")]
        public async Task<ActionResult<CODCollection>> GetCODCollection(int id)
        {
            try
            {
                var codCollection = await _context.CODCollections
                    .Include(c => c.Dispatch)
                    .Include(c => c.Branch)
                    .Include(c => c.CreatedBy)
                    .Include(c => c.UpdatedBy)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (codCollection == null)
                {
                    return NotFound(new { message = "COD collection not found" });
                }

                return Ok(codCollection);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to retrieve COD collection", error = ex.Message });
            }
        }

        // POST: api/Payments/cod-collections
        [HttpPost("cod-collections")]
        public async Task<ActionResult<CODCollection>> CreateCODCollection(CreateCODCollectionDto createDto)
        {
            try
            {
                // Validate dispatch exists
                var dispatch = await _context.Dispatches.FindAsync(createDto.DispatchId);
                if (dispatch == null)
                {
                    return BadRequest(new { message = "Dispatch not found" });
                }

                // Check if COD collection already exists for this dispatch
                var existingCollection = await _context.CODCollections
                    .FirstOrDefaultAsync(c => c.DispatchId == createDto.DispatchId);
                
                if (existingCollection != null)
                {
                    return BadRequest(new { message = "COD collection already exists for this dispatch" });
                }

                var codCollection = new CODCollection
                {
                    DispatchCode = dispatch.DispatchCode, // Use the actual dispatch code from the dispatch entity
                    DispatchId = createDto.DispatchId,
                    DriverName = createDto.DriverName,
                    VehicleNumber = createDto.VehicleNumber,
                    BranchName = createDto.BranchName,
                    BranchId = createDto.BranchId,
                    TotalCODAmount = createDto.TotalCODAmount,
                    DepositedAmount = createDto.DepositedAmount,
                    Shortfall = createDto.TotalCODAmount - createDto.DepositedAmount,
                    CollectionDate = createDto.CollectionDate,
                    DepositDate = createDto.DepositDate,
                    Status = createDto.Status,
                    Notes = createDto.Notes,
                    CreatedById = GetCurrentUserId(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.CODCollections.Add(codCollection);
                await _context.SaveChangesAsync();

                // Reload with relationships
                var createdCollection = await _context.CODCollections
                    .Include(c => c.Dispatch)
                    .Include(c => c.Branch)
                    .Include(c => c.CreatedBy)
                    .FirstAsync(c => c.Id == codCollection.Id);

                return CreatedAtAction(nameof(GetCODCollection), new { id = createdCollection.Id }, createdCollection);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to create COD collection", error = ex.Message });
            }
        }

        // PUT: api/Payments/cod-collections/5
        [HttpPut("cod-collections/{id}")]
        public async Task<IActionResult> UpdateCODCollection(int id, UpdateCODCollectionDto updateDto)
        {
            try
            {
                var codCollection = await _context.CODCollections.FindAsync(id);
                if (codCollection == null)
                {
                    return NotFound(new { message = "COD collection not found" });
                }

                // Update properties
                codCollection.DepositedAmount = updateDto.DepositedAmount;
                codCollection.Shortfall = codCollection.TotalCODAmount - updateDto.DepositedAmount;
                
                if (updateDto.DepositDate.HasValue)
                    codCollection.DepositDate = updateDto.DepositDate;
                
                if (!string.IsNullOrEmpty(updateDto.Status))
                    codCollection.Status = updateDto.Status;
                
                if (!string.IsNullOrEmpty(updateDto.Notes))
                    codCollection.Notes = updateDto.Notes;

                codCollection.UpdatedById = GetCurrentUserId();
                codCollection.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to update COD collection", error = ex.Message });
            }
        }

        // Cheque Deposits Endpoints

        // GET: api/Payments/cheque-deposits
        [HttpGet("cheque-deposits")]
        public async Task<ActionResult<IEnumerable<ChequeDeposit>>> GetChequeDeposits()
        {
            try
            {
                var chequeDeposits = await _context.ChequeDeposits
                    .Include(c => c.RelatedInvoice)
                    .Include(c => c.ContractCustomer)
                    .Include(c => c.Branch)
                    .Include(c => c.CreatedBy)
                    .Include(c => c.UpdatedBy)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();

                return Ok(chequeDeposits);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to retrieve cheque deposits", error = ex.Message });
            }
        }

        // GET: api/Payments/cheque-deposits/5
        [HttpGet("cheque-deposits/{id}")]
        public async Task<ActionResult<ChequeDeposit>> GetChequeDeposit(int id)
        {
            try
            {
                var chequeDeposit = await _context.ChequeDeposits
                    .Include(c => c.RelatedInvoice)
                    .Include(c => c.ContractCustomer)
                    .Include(c => c.Branch)
                    .Include(c => c.CreatedBy)
                    .Include(c => c.UpdatedBy)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (chequeDeposit == null)
                {
                    return NotFound(new { message = "Cheque deposit not found" });
                }

                return Ok(chequeDeposit);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to retrieve cheque deposit", error = ex.Message });
            }
        }

        // POST: api/Payments/cheque-deposits
        [HttpPost("cheque-deposits")]
        public async Task<ActionResult<ChequeDeposit>> CreateChequeDeposit(CreateChequeDepositDto createDto)
        {
            try
            {
                // Check if cheque number already exists
                var existingCheque = await _context.ChequeDeposits
                    .FirstOrDefaultAsync(c => c.ChequeNumber == createDto.ChequeNumber);
                
                if (existingCheque != null)
                {
                    return BadRequest(new { message = "Cheque with this number already exists" });
                }

                var chequeDeposit = new ChequeDeposit
                {
                    ChequeNumber = createDto.ChequeNumber,
                    DrawerName = createDto.DrawerName,
                    BankName = createDto.BankName,
                    Amount = createDto.Amount,
                    DepositDate = createDto.DepositDate,
                    Status = createDto.Status,
                    RelatedInvoiceNumber = createDto.RelatedInvoiceNumber,
                    RelatedInvoiceId = createDto.RelatedInvoiceId,
                    ContractCustomerId = createDto.ContractCustomerId,
                    CustomerName = createDto.CustomerName,
                    BranchId = createDto.BranchId,
                    BranchName = createDto.BranchName,
                    Notes = createDto.Notes,
                    CreatedById = GetCurrentUserId(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.ChequeDeposits.Add(chequeDeposit);
                await _context.SaveChangesAsync();

                // Reload with relationships
                var createdDeposit = await _context.ChequeDeposits
                    .Include(c => c.RelatedInvoice)
                    .Include(c => c.ContractCustomer)
                    .Include(c => c.Branch)
                    .Include(c => c.CreatedBy)
                    .FirstAsync(c => c.Id == chequeDeposit.Id);

                return CreatedAtAction(nameof(GetChequeDeposit), new { id = createdDeposit.Id }, createdDeposit);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to create cheque deposit", error = ex.Message });
            }
        }

        // PUT: api/Payments/cheque-deposits/5
        [HttpPut("cheque-deposits/{id}")]
        public async Task<IActionResult> UpdateChequeDeposit(int id, UpdateChequeDepositDto updateDto)
        {
            try
            {
                var chequeDeposit = await _context.ChequeDeposits.FindAsync(id);
                if (chequeDeposit == null)
                {
                    return NotFound(new { message = "Cheque deposit not found" });
                }

                // Update properties
                if (updateDto.ClearanceDate.HasValue)
                    chequeDeposit.ClearanceDate = updateDto.ClearanceDate;
                
                if (!string.IsNullOrEmpty(updateDto.Status))
                    chequeDeposit.Status = updateDto.Status;
                
                if (!string.IsNullOrEmpty(updateDto.Notes))
                    chequeDeposit.Notes = updateDto.Notes;
                
                if (!string.IsNullOrEmpty(updateDto.BounceReason))
                    chequeDeposit.BounceReason = updateDto.BounceReason;

                chequeDeposit.UpdatedById = GetCurrentUserId();
                chequeDeposit.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to update cheque deposit", error = ex.Message });
            }
        }

        // GET: api/Payments/summary
        [HttpGet("summary")]
        public async Task<ActionResult> GetPaymentsSummary()
        {
            try
            {
                var codSummary = await _context.CODCollections
                    .GroupBy(x => 1)
                    .Select(g => new
                    {
                        TotalCollected = g.Sum(c => c.TotalCODAmount),
                        TotalDeposited = g.Sum(c => c.DepositedAmount),
                        TotalShortfall = g.Sum(c => c.Shortfall),
                        Count = g.Count()
                    })
                    .FirstOrDefaultAsync();

                var chequeSummary = await _context.ChequeDeposits
                    .GroupBy(x => 1)
                    .Select(g => new
                    {
                        TotalAmount = g.Sum(c => c.Amount),
                        ClearedAmount = g.Where(c => c.Status == "cleared").Sum(c => c.Amount),
                        PendingAmount = g.Where(c => c.Status == "deposited").Sum(c => c.Amount),
                        BouncedAmount = g.Where(c => c.Status == "bounced").Sum(c => c.Amount),
                        Count = g.Count()
                    })
                    .FirstOrDefaultAsync();

                return Ok(new
                {
                    CODSummary = codSummary ?? new { TotalCollected = 0m, TotalDeposited = 0m, TotalShortfall = 0m, Count = 0 },
                    ChequeSummary = chequeSummary ?? new { TotalAmount = 0m, ClearedAmount = 0m, PendingAmount = 0m, BouncedAmount = 0m, Count = 0 }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to retrieve payments summary", error = ex.Message });
            }
        }

        private int GetCurrentUserId()
        {
            // TODO: Implement proper authentication and get user ID from JWT token
            // For now, return a default user ID
            return 1; // Admin user
        }
    }
}