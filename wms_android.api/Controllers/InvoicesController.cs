using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using wms_android.shared.Data;
using wms_android.shared.Models;
using wms_android.shared.DTOs;

namespace wms_android.api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class InvoicesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public InvoicesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Invoices
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Invoice>>> GetInvoices()
        {
            try
            {
                var invoices = await _context.Invoices
                    .Include(i => i.Customer)
                    .Include(i => i.CreatedBy)
                    .Include(i => i.Items)
                    .OrderByDescending(i => i.CreatedAt)
                    .ToListAsync();

                return Ok(invoices);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to retrieve invoices", error = ex.Message });
            }
        }

        // GET: api/Invoices/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Invoice>> GetInvoice(int id)
        {
            try
            {
                var invoice = await _context.Invoices
                    .Include(i => i.Customer)
                    .Include(i => i.CreatedBy)
                    .Include(i => i.Items)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (invoice == null)
                {
                    return NotFound(new { message = "Invoice not found" });
                }

                return Ok(invoice);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to retrieve invoice", error = ex.Message });
            }
        }

        // POST: api/Invoices
        [HttpPost]
        public async Task<ActionResult<Invoice>> CreateInvoice(CreateInvoiceDto createInvoiceDto)
        {
            try
            {
                // Validate contract customer exists
                var customer = await _context.ContractCustomers.FindAsync(createInvoiceDto.ContractCustomerId);
                if (customer == null)
                {
                    return BadRequest(new { message = "Contract customer not found" });
                }

                // Validate and get selected parcels
                if (createInvoiceDto.ParcelIds == null || !createInvoiceDto.ParcelIds.Any())
                {
                    return BadRequest(new { message = "At least one parcel must be selected" });
                }

                // Convert string parcel IDs to Guids
                var parcelGuids = createInvoiceDto.ParcelIds.Select(id => Guid.Parse(id)).ToList();
                
                var parcels = await _context.Parcels
                    .Where(p => parcelGuids.Contains(p.Id))
                    .ToListAsync();

                if (parcels.Count != createInvoiceDto.ParcelIds.Count)
                {
                    return BadRequest(new { message = "One or more selected parcels not found" });
                }

                // Generate invoice number
                var invoiceNumber = await GenerateInvoiceNumber();

                // Calculate totals - prices are VAT-inclusive
                var totalAmount = parcels.Sum(p => p.TotalAmount); // VAT-inclusive total
                var taxRate = 0.16m; // 16% VAT
                var subtotal = totalAmount / (1 + taxRate); // Extract base amount
                var taxAmount = totalAmount - subtotal; // VAT component

                var invoice = new Invoice
                {
                    InvoiceNumber = invoiceNumber,
                    CustomerId = createInvoiceDto.ContractCustomerId,
                    ContractCustomerId = createInvoiceDto.ContractCustomerId,
                    IssueDate = createInvoiceDto.IssueDate,
                    DueDate = createInvoiceDto.DueDate,
                    BillingPeriodStart = createInvoiceDto.BillingPeriodStart,
                    BillingPeriodEnd = createInvoiceDto.BillingPeriodEnd,
                    Status = "draft",
                    Subtotal = subtotal,
                    TaxAmount = taxAmount,
                    TotalAmount = totalAmount,
                    PaidAmount = 0,
                    Notes = createInvoiceDto.Notes,
                    CreatedById = createInvoiceDto.CreatedById,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();

                // Create invoice items for each selected parcel
                var invoiceItems = parcels.Select(p => new InvoiceItem
                {
                    InvoiceId = invoice.Id,
                    ParcelId = p.Id.ToString(),
                    WaybillNumber = p.WaybillNumber ?? "",
                    Description = p.Description ?? "",
                    Destination = p.Destination ?? "",
                    ParcelCreatedAt = p.CreatedAt,
                    Quantity = p.Quantity ?? 1,
                    TotalPrice = p.TotalAmount,
                    CreatedAt = DateTime.UtcNow
                }).ToList();

                _context.InvoiceItems.AddRange(invoiceItems);
                await _context.SaveChangesAsync();

                // Reload with relationships
                var createdInvoice = await _context.Invoices
                    .Include(i => i.Customer)
                    .Include(i => i.CreatedBy)
                    .Include(i => i.Items)
                    .FirstAsync(i => i.Id == invoice.Id);

                return CreatedAtAction(nameof(GetInvoice), new { id = createdInvoice.Id }, createdInvoice);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to create invoice", error = ex.Message });
            }
        }

        // PUT: api/Invoices/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateInvoice(int id, UpdateInvoiceDto updateInvoiceDto)
        {
            try
            {
                var invoice = await _context.Invoices.FindAsync(id);
                if (invoice == null)
                {
                    return NotFound(new { message = "Invoice not found" });
                }

                // Update properties
                if (updateInvoiceDto.IssueDate.HasValue)
                    invoice.IssueDate = updateInvoiceDto.IssueDate.Value;
                
                if (updateInvoiceDto.DueDate.HasValue)
                    invoice.DueDate = updateInvoiceDto.DueDate.Value;
                
                if (updateInvoiceDto.BillingPeriodStart.HasValue)
                    invoice.BillingPeriodStart = updateInvoiceDto.BillingPeriodStart.Value;
                
                if (updateInvoiceDto.BillingPeriodEnd.HasValue)
                    invoice.BillingPeriodEnd = updateInvoiceDto.BillingPeriodEnd.Value;
                
                if (!string.IsNullOrEmpty(updateInvoiceDto.Status))
                    invoice.Status = updateInvoiceDto.Status;
                
                if (!string.IsNullOrEmpty(updateInvoiceDto.Notes))
                    invoice.Notes = updateInvoiceDto.Notes;

                invoice.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to update invoice", error = ex.Message });
            }
        }

        // POST: api/Invoices/5/payment
        [HttpPost("{id}/payment")]
        public async Task<IActionResult> RecordPayment(int id, RecordPaymentDto paymentDto)
        {
            try
            {
                var invoice = await _context.Invoices.FindAsync(id);
                if (invoice == null)
                {
                    return NotFound(new { message = "Invoice not found" });
                }

                // Update paid amount
                invoice.PaidAmount += paymentDto.Amount;
                
                // Update status based on payment
                if (invoice.PaidAmount >= invoice.TotalAmount)
                {
                    invoice.Status = "paid";
                }
                else if (invoice.PaidAmount > 0)
                {
                    invoice.Status = "partial";
                }

                invoice.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Payment recorded successfully", paidAmount = invoice.PaidAmount });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to record payment", error = ex.Message });
            }
        }

        // GET: api/Invoices/eligible-parcels
        [HttpGet("eligible-parcels")]
        public async Task<ActionResult<IEnumerable<Parcel>>> GetEligibleParcels([FromQuery] DateTime? billingPeriodStart = null, [FromQuery] DateTime? billingPeriodEnd = null)
        {
            try
            {
                var query = _context.Parcels.AsQueryable();
                
                // Filter for parcels eligible for invoicing:
                // 1. Status must be Confirmed (1) or Delivered (3)
                // 2. Payment method must be "contract"
                // 3. Must have been dispatched (DispatchedAt is not null)
                query = query.Where(p => 
                    (p.Status == ParcelStatus.Confirmed || p.Status == ParcelStatus.Delivered) &&
                    p.PaymentMethods.ToLower().Contains("contract") &&
                    p.DispatchedAt != null
                );
                
                // Filter by billing period if provided
                if (billingPeriodStart.HasValue && billingPeriodEnd.HasValue)
                {
                    // Use DispatchedAt for billing period filtering as this represents when service was delivered
                    query = query.Where(p => p.DispatchedAt >= billingPeriodStart && p.DispatchedAt <= billingPeriodEnd.Value.AddDays(1).AddTicks(-1));
                }
                
                var eligibleParcels = await query
                    .AsNoTracking() // Remove circular reference tracking
                    .OrderByDescending(p => p.DispatchedAt ?? p.CreatedAt)
                    .ToListAsync();
                    
                // Load related entities separately to avoid circular references
                foreach (var parcel in eligibleParcels)
                {
                    if (parcel.CreatedById.HasValue)
                    {
                        parcel.CreatedBy = await _context.Users
                            .AsNoTracking()
                            .FirstOrDefaultAsync(u => u.Id == parcel.CreatedById.Value);
                    }
                    
                    if (parcel.ShipmentId.HasValue)
                    {
                        parcel.Shipment = await _context.Shipments
                            .AsNoTracking()
                            .FirstOrDefaultAsync(s => s.Id == parcel.ShipmentId.Value);
                    }
                }

                return Ok(eligibleParcels);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to retrieve eligible parcels", error = ex.Message });
            }
        }

        // GET: api/Invoices/customer/5
        [HttpGet("customer/{customerId}")]
        public async Task<ActionResult<IEnumerable<Invoice>>> GetInvoicesByCustomer(int customerId)
        {
            try
            {
                var invoices = await _context.Invoices
                    .Include(i => i.Customer)
                    .Include(i => i.CreatedBy)
                    .Include(i => i.Items)
                    .Where(i => i.ContractCustomerId == customerId)
                    .OrderByDescending(i => i.CreatedAt)
                    .ToListAsync();

                return Ok(invoices);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to retrieve customer invoices", error = ex.Message });
            }
        }

        // DELETE: api/Invoices/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInvoice(int id)
        {
            try
            {
                var invoice = await _context.Invoices
                    .Include(i => i.Items)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (invoice == null)
                {
                    return NotFound(new { message = "Invoice not found" });
                }

                // Only allow deletion if status is draft
                if (invoice.Status != "draft")
                {
                    return BadRequest(new { message = "Only draft invoices can be deleted" });
                }

                _context.Invoices.Remove(invoice);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to delete invoice", error = ex.Message });
            }
        }

        // POST: api/Invoices/5/send
        [HttpPost("{id}/send")]
        public async Task<IActionResult> SendInvoice(int id)
        {
            try
            {
                var invoice = await _context.Invoices.FindAsync(id);
                if (invoice == null)
                {
                    return NotFound(new { message = "Invoice not found" });
                }

                if (invoice.Status != "draft")
                {
                    return BadRequest(new { message = "Only draft invoices can be sent" });
                }

                invoice.Status = "sent";
                invoice.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Invoice sent successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to send invoice", error = ex.Message });
            }
        }

        private async Task<string> GenerateInvoiceNumber()
        {
            var currentYear = DateTime.UtcNow.Year;
            var currentMonth = DateTime.UtcNow.Month;
            
            // Count invoices this month
            var monthlyCount = await _context.Invoices
                .Where(i => i.CreatedAt.Year == currentYear && i.CreatedAt.Month == currentMonth)
                .CountAsync();

            var invoiceNumber = $"INV-{currentYear:D4}{currentMonth:D2}-{(monthlyCount + 1):D4}";
            
            // Ensure uniqueness
            while (await _context.Invoices.AnyAsync(i => i.InvoiceNumber == invoiceNumber))
            {
                monthlyCount++;
                invoiceNumber = $"INV-{currentYear:D4}{currentMonth:D2}-{(monthlyCount + 1):D4}";
            }

            return invoiceNumber;
        }
    }
}