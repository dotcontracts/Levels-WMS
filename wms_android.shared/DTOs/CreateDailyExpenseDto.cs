using System;
using System.ComponentModel.DataAnnotations;

namespace wms_android.shared.DTOs
{
    public class CreateDailyExpenseDto
    {
        public string Category { get; set; } // fuel, casual_labor, hired_cars, maintenance, office_supplies, utilities, transport, other
        
        public string Description { get; set; }
        
        public decimal Amount { get; set; }
        
        public DateTime? Date { get; set; }
        
        public string Vendor { get; set; }
        
        public string ReceiptNumber { get; set; }
        
        public int? BranchId { get; set; }
        
        public string BranchName { get; set; }
        
        public int? ClerkId { get; set; }
        
        public string ClerkName { get; set; }
    }
    
    public class ApproveExpenseDto
    {
        [Required]
        public int ExpenseId { get; set; }
        
        [Required]
        public bool Approved { get; set; }
        
        public string ApprovalNotes { get; set; }
        
        public string RejectionReason { get; set; }
    }
    
    public class UpdateDailyExpenseDto
    {
        public string Category { get; set; }
        
        public string Description { get; set; }
        
        public decimal? Amount { get; set; }
        
        public DateTime? Date { get; set; }
        
        public string Vendor { get; set; }
        
        public string ReceiptNumber { get; set; }
        
        public int? BranchId { get; set; }
        
        public string BranchName { get; set; }
        
        public int? ClerkId { get; set; }
        
        public string ClerkName { get; set; }
    }
}