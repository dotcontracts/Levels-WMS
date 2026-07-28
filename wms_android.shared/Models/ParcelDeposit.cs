using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace wms_android.shared.Models
{
    /// <summary>
    /// Tracks deposit amounts and expenses for parcels in the Cash-in management system
    /// </summary>
    public class ParcelDeposit
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to the Parcel
        /// </summary>
        [Required]
        public Guid ParcelId { get; set; }
        
        /// <summary>
        /// Navigation property to the associated parcel
        /// </summary>
        [ForeignKey("ParcelId")]
        public virtual Parcel Parcel { get; set; }

        /// <summary>
        /// Amount deposited by the clerk for this parcel
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal DepositedAmount { get; set; } = 0;

        /// <summary>
        /// Expenses associated with this parcel (e.g., delivery costs, handling fees)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Expenses { get; set; } = 0;

        /// <summary>
        /// Optional notes about the deposit or expenses
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Date when this deposit record was created
        /// </summary>
        [Column(TypeName = "timestamp with time zone")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date when this deposit record was last updated
        /// </summary>
        [Column(TypeName = "timestamp with time zone")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// ID of the user who created this deposit record
        /// </summary>
        public int? CreatedById { get; set; }
        
        /// <summary>
        /// Navigation property to the user who created this record
        /// </summary>
        [ForeignKey("CreatedById")]
        public virtual User? CreatedBy { get; set; }

        /// <summary>
        /// ID of the user who last updated this deposit record
        /// </summary>
        public int? UpdatedById { get; set; }
        
        /// <summary>
        /// Navigation property to the user who last updated this record
        /// </summary>
        [ForeignKey("UpdatedById")]
        public virtual User? UpdatedBy { get; set; }

        /// <summary>
        /// Calculated property: Remaining debt for this parcel
        /// (Parcel total amount - deposited amount - expenses)
        /// </summary>
        [NotMapped]
        public decimal RemainingDebt => (Parcel?.TotalAmount ?? 0) - DepositedAmount - Expenses;
    }
}