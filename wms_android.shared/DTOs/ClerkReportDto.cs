using System;
using System.Collections.Generic;

namespace wms_android.shared.DTOs
{
    public class ClerkReportDto
    {
        public DateTime ReportDate { get; set; }
        public string ClerkName { get; set; } = string.Empty;

        public int DailyParcelCount { get; set; }
        public decimal DailySalesTotal { get; set; }

        public decimal MonthlyTotal { get; set; }
        public int MonthlyRank { get; set; }
        public int TotalClerksRanked { get; set; }

        public List<ClerkReportExpenseItem> Deposits { get; set; } = new();
        public List<ClerkReportExpenseItem> Expenses { get; set; } = new();
    }

    public class ClerkReportExpenseItem
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? RejectionReason { get; set; }
        public DateTime Date { get; set; }
    }
}
