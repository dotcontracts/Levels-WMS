namespace wms_android.shared.DTOs
{
    public class BranchCodLockStatusDto
    {
        public bool IsLocked { get; set; }

        // "none" | "pending_submission" | "pending_approval" | "rejected"
        public string Reason { get; set; } = "none";

        public decimal PreviousDayCodTotal { get; set; }
        public DateTime PreviousDate { get; set; }

        public int? DepositRecordId { get; set; }
        public string? DepositStatus { get; set; }
        public decimal? DepositAmount { get; set; }
        public string? DepositRejectionReason { get; set; }

        public int? ExpenseRecordId { get; set; }
        public string? ExpenseStatus { get; set; }
        public decimal? ExpenseAmount { get; set; }
        public string? ExpenseRejectionReason { get; set; }
    }
}
