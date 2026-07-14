using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using wms_android.shared.Data;
using wms_android.shared.Models;
using wms_android.shared.DTOs;
using System.Security.Claims;

namespace wms_android.api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ExpensesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ExpensesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Expenses
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DailyExpense>>> GetExpenses()
        {
            try
            {
                var expenses = await _context.DailyExpenses
                    .Include(e => e.Branch)
                    .Include(e => e.Clerk)
                    .Include(e => e.CreatedBy)
                    .Include(e => e.ApprovedBy)
                    .OrderByDescending(e => e.CreatedAt)
                    .ToListAsync();

                return Ok(expenses);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to retrieve expenses", error = ex.Message });
            }
        }

        // GET: api/Expenses/5
        [HttpGet("{id}")]
        public async Task<ActionResult<DailyExpense>> GetExpense(int id)
        {
            try
            {
                var expense = await _context.DailyExpenses
                    .Include(e => e.Branch)
                    .Include(e => e.Clerk)
                    .Include(e => e.CreatedBy)
                    .Include(e => e.ApprovedBy)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (expense == null)
                {
                    return NotFound(new { message = "Expense not found" });
                }

                return Ok(expense);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to retrieve expense", error = ex.Message });
            }
        }

        // POST: api/Expenses
        [HttpPost]
        public async Task<ActionResult<DailyExpense>> CreateExpense(CreateDailyExpenseDto createDto)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized("User authentication required");
                }
                
                var currentUser = await _context.Users.FindAsync(currentUserId);
                
                var expense = new DailyExpense
                {
                    Category = createDto.Category,
                    Description = createDto.Description,
                    Amount = createDto.Amount,
                    Date = createDto.Date,
                    Vendor = createDto.Vendor ?? "",
                    ReceiptNumber = createDto.ReceiptNumber ?? "",
                    Status = "pending",
                    BranchId = createDto.BranchId,
                    BranchName = createDto.BranchName ?? await GetBranchName(createDto.BranchId) ?? "",
                    ClerkId = createDto.ClerkId,
                    ClerkName = createDto.ClerkName ?? await GetUserName(createDto.ClerkId) ?? "",
                    CreatedById = currentUserId.Value,
                    CreatedByName = $"{currentUser?.FirstName} {currentUser?.LastName}".Trim(),
                    ApprovalNotes = "", // Initialize to empty string to satisfy NOT NULL constraint
                    ApprovedByName = "", // Initialize to empty string to satisfy NOT NULL constraint  
                    RejectionReason = "", // Also set this to be safe
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.DailyExpenses.Add(expense);
                await _context.SaveChangesAsync();

                // Reload with relationships
                var createdExpense = await _context.DailyExpenses
                    .Include(e => e.Branch)
                    .Include(e => e.Clerk)
                    .Include(e => e.CreatedBy)
                    .FirstAsync(e => e.Id == expense.Id);

                return CreatedAtAction(nameof(GetExpense), new { id = createdExpense.Id }, createdExpense);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to create expense", error = ex.Message });
            }
        }

        // PUT: api/Expenses/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateExpense(int id, UpdateDailyExpenseDto updateDto)
        {
            try
            {
                var expense = await _context.DailyExpenses.FindAsync(id);
                if (expense == null)
                {
                    return NotFound(new { message = "Expense not found" });
                }

                // Only allow updates if status is pending
                if (expense.Status != "pending")
                {
                    return BadRequest(new { message = "Cannot update expense that has been approved or rejected" });
                }

                // Update properties
                if (!string.IsNullOrEmpty(updateDto.Category))
                    expense.Category = updateDto.Category;
                
                if (!string.IsNullOrEmpty(updateDto.Description))
                    expense.Description = updateDto.Description;
                
                if (updateDto.Amount.HasValue)
                    expense.Amount = updateDto.Amount.Value;
                
                if (updateDto.Date.HasValue)
                    expense.Date = updateDto.Date.Value;
                
                if (!string.IsNullOrEmpty(updateDto.Vendor))
                    expense.Vendor = updateDto.Vendor;
                
                if (!string.IsNullOrEmpty(updateDto.ReceiptNumber))
                    expense.ReceiptNumber = updateDto.ReceiptNumber;
                
                if (updateDto.BranchId.HasValue)
                {
                    expense.BranchId = updateDto.BranchId;
                    expense.BranchName = updateDto.BranchName ?? await GetBranchName(updateDto.BranchId);
                }
                
                if (updateDto.ClerkId.HasValue)
                {
                    expense.ClerkId = updateDto.ClerkId;
                    expense.ClerkName = updateDto.ClerkName ?? await GetUserName(updateDto.ClerkId);
                }

                expense.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to update expense", error = ex.Message });
            }
        }

        // POST: api/Expenses/approve
        [HttpPost("approve")]
        public async Task<IActionResult> ApproveExpense([FromBody] dynamic requestBody)
        {
            try
            {
                Console.WriteLine($"[DEBUG] Raw request body: {requestBody}");
                
                // Extract values from the dynamic object
                int expenseId = 0;
                bool approved = false;
                string rejectionReason = "";
                string approvalNotes = "";
                
                try 
                {
                    var jsonElement = (System.Text.Json.JsonElement)requestBody;
                    expenseId = jsonElement.GetProperty("expenseId").GetInt32();
                    approved = jsonElement.GetProperty("approved").GetBoolean();
                    
                    // Try to get rejection reason if present
                    if (jsonElement.TryGetProperty("rejectionReason", out var rejectionElement))
                    {
                        rejectionReason = rejectionElement.GetString() ?? "";
                    }
                    
                    // Try to get approval notes if present
                    if (jsonElement.TryGetProperty("notes", out var notesElement))
                    {
                        approvalNotes = notesElement.GetString() ?? "";
                    }
                    
                    Console.WriteLine($"[DEBUG] Parsed - ExpenseId: {expenseId}, Approved: {approved}, Rejection: {rejectionReason}");
                }
                catch (Exception parseEx)
                {
                    Console.WriteLine($"[DEBUG] Parse error: {parseEx.Message}");
                    return BadRequest("Invalid request format");
                }
                
                if (expenseId <= 0)
                {
                    Console.WriteLine($"[DEBUG] Invalid ExpenseId: {expenseId}");
                    return BadRequest("Invalid expense ID");
                }
                
                var expense = await _context.DailyExpenses.FindAsync(expenseId);
                if (expense == null)
                {
                    return NotFound(new { message = "Expense not found" });
                }

                if (expense.Status != "pending")
                {
                    return BadRequest(new { message = "Expense is not pending approval" });
                }

                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    // Use fallback user for development
                    var fallbackUser = await _context.Users
                        .Where(u => u.IsActive && u.Role.Name == "Admin")
                        .Include(u => u.Role)
                        .FirstOrDefaultAsync();
                    if (fallbackUser != null)
                    {
                        currentUserId = fallbackUser.Id;
                        Console.WriteLine($"[DEBUG] Using fallback user for approval: {currentUserId}");
                    }
                    else
                    {
                        return StatusCode(500, new { message = "No active admin user found" });
                    }
                }
                
                var currentUser = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == currentUserId);

                // Check if user has approval permissions
                if (!CanApproveExpenses(currentUser))
                {
                    return Forbid("You don't have permission to approve expenses");
                }

                // Update expense status
                expense.Status = approved ? "approved" : "rejected";
                expense.ApprovedById = currentUserId.Value;
                expense.ApprovedByName = $"{currentUser?.FirstName} {currentUser?.LastName}".Trim();
                expense.ApprovedAt = DateTime.UtcNow;
                
                if (approved)
                {
                    expense.ApprovalNotes = approvalNotes;
                }
                else
                {
                    expense.RejectionReason = rejectionReason;
                }

                expense.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { message = $"Expense {(approved ? "approved" : "rejected")} successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to process expense approval", error = ex.Message });
            }
        }

        // GET: api/Expenses/pending
        [HttpGet("pending")]
        public async Task<ActionResult<IEnumerable<DailyExpense>>> GetPendingExpenses()
        {
            try
            {
                var pendingExpenses = await _context.DailyExpenses
                    .Include(e => e.Branch)
                    .Include(e => e.Clerk)
                    .Include(e => e.CreatedBy)
                    .Where(e => e.Status == "pending")
                    .OrderByDescending(e => e.CreatedAt)
                    .ToListAsync();

                return Ok(pendingExpenses);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to retrieve pending expenses", error = ex.Message });
            }
        }

        // GET: api/Expenses/summary
        [HttpGet("summary")]
        public async Task<ActionResult> GetExpensesSummary()
        {
            try
            {
                var currentMonth = DateTime.UtcNow.Month;
                var currentYear = DateTime.UtcNow.Year;

                var totalExpenses = await _context.DailyExpenses.SumAsync(e => e.Amount);
                var pendingExpenses = await _context.DailyExpenses
                    .Where(e => e.Status == "pending")
                    .SumAsync(e => e.Amount);
                var approvedExpenses = await _context.DailyExpenses
                    .Where(e => e.Status == "approved")
                    .SumAsync(e => e.Amount);
                var rejectedExpenses = await _context.DailyExpenses
                    .Where(e => e.Status == "rejected")
                    .SumAsync(e => e.Amount);
                var monthlyTotal = await _context.DailyExpenses
                    .Where(e => e.Date.Month == currentMonth && e.Date.Year == currentYear && e.Status == "approved")
                    .SumAsync(e => e.Amount);

                var categoryBreakdown = await _context.DailyExpenses
                    .Where(e => e.Status == "approved")
                    .GroupBy(e => e.Category)
                    .Select(g => new
                    {
                        Category = g.Key,
                        Amount = g.Sum(e => e.Amount),
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Amount)
                    .ToListAsync();

                return Ok(new
                {
                    TotalExpenses = totalExpenses,
                    PendingExpenses = pendingExpenses,
                    ApprovedExpenses = approvedExpenses,
                    RejectedExpenses = rejectedExpenses,
                    MonthlyTotal = monthlyTotal,
                    CategoryBreakdown = categoryBreakdown
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to retrieve expenses summary", error = ex.Message });
            }
        }

        // DELETE: api/Expenses/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExpense(int id)
        {
            try
            {
                var expense = await _context.DailyExpenses.FindAsync(id);
                if (expense == null)
                {
                    return NotFound(new { message = "Expense not found" });
                }

                // Only allow deletion if status is pending
                if (expense.Status != "pending")
                {
                    return BadRequest(new { message = "Cannot delete expense that has been approved or rejected" });
                }

                _context.DailyExpenses.Remove(expense);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to delete expense", error = ex.Message });
            }
        }

        private bool CanApproveExpenses(User user)
        {
            if (user == null) return false;
            
            // Check by roleId (most reliable)
            if (user.RoleId == 1) return true; // Admin
            
            // Check by role name
            var roleName = user.Role?.Name?.ToLower() ?? "";
            var allowedRoles = new[] { "admin", "administrator", "accountant" };
            return allowedRoles.Contains(roleName);
        }

        private async Task<string> GetBranchName(int? branchId)
        {
            if (!branchId.HasValue) return null;
            var branch = await _context.Branches.FindAsync(branchId.Value);
            return branch?.Name;
        }

        private async Task<string> GetUserName(int? userId)
        {
            if (!userId.HasValue) return null;
            var user = await _context.Users.FindAsync(userId.Value);
            return user != null ? $"{user.FirstName} {user.LastName}".Trim() : null;
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