using System.ComponentModel.DataAnnotations;

namespace wms_android.shared.DTOs
{
    public class BranchDepositDto
    {
        public int Id { get; set; }
        public string Branch { get; set; }
        public DateTime Date { get; set; }
        public decimal CodTotal { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal RunningDebt { get; set; }
        public string Status { get; set; }
        public string? RejectionReason { get; set; }
        public int? ApprovedById { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int? CreatedById { get; set; }
        public string? CreatedByName { get; set; }
    }

    public class CreateBranchDepositDto
    {
        [Required]
        [MaxLength(100)]
        public string Branch { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "COD total must be non-negative")]
        public decimal CodTotal { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Deposit amount must be non-negative")]
        public decimal DepositAmount { get; set; }
    }

    public class UpdateBranchDepositDto
    {
        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "COD total must be non-negative")]
        public decimal CodTotal { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Deposit amount must be non-negative")]
        public decimal DepositAmount { get; set; }
    }

    public class ApproveBranchDepositDto
    {
        public string? RejectionReason { get; set; }
    }

    public class BranchDepositSummaryDto
    {
        public string Branch { get; set; }
        public decimal TotalCod { get; set; }
        public decimal TotalDeposits { get; set; }
        public decimal TotalDebt { get; set; }
        public int RecordCount { get; set; }
        public DateTime? LastDepositDate { get; set; }
    }
}