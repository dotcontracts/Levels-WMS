using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace wms_android.shared.Models
{
    public class BranchDeposit
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Branch { get; set; }
        
        [Required]
        [Column(TypeName = "date")]
        public DateTime Date { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CodTotal { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DepositAmount { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal RunningDebt { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "pending"; // pending, approved, rejected

        [MaxLength(500)]
        public string? RejectionReason { get; set; }

        public int? ApprovedById { get; set; }

        [ForeignKey("ApprovedById")]
        public virtual User? ApprovedBy { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public int? CreatedById { get; set; }
        
        // Navigation properties
        [ForeignKey("CreatedById")]
        public virtual User? CreatedBy { get; set; }
        
        // Static method to configure entity relationships and indexes
        public static void ConfigureEntity(ModelBuilder modelBuilder)
        {
            // Unique constraint: one deposit record per branch per date
            modelBuilder.Entity<BranchDeposit>()
                .HasIndex(bd => new { bd.Branch, bd.Date })
                .IsUnique()
                .HasDatabaseName("IX_BranchDeposit_Branch_Date");
                
            // Index for efficient branch queries
            modelBuilder.Entity<BranchDeposit>()
                .HasIndex(bd => bd.Branch)
                .HasDatabaseName("IX_BranchDeposit_Branch");
                
            // Index for efficient date range queries
            modelBuilder.Entity<BranchDeposit>()
                .HasIndex(bd => bd.Date)
                .HasDatabaseName("IX_BranchDeposit_Date");

            modelBuilder.Entity<BranchDeposit>()
                .Property(bd => bd.Status)
                .HasMaxLength(20)
                .HasDefaultValue("pending");

            modelBuilder.Entity<BranchDeposit>()
                .Property(bd => bd.RejectionReason)
                .HasMaxLength(500);
                
            // Configure relationships
            modelBuilder.Entity<BranchDeposit>()
                .HasOne(bd => bd.CreatedBy)
                .WithMany()
                .HasForeignKey(bd => bd.CreatedById)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<BranchDeposit>()
                .HasOne(bd => bd.ApprovedBy)
                .WithMany()
                .HasForeignKey(bd => bd.ApprovedById)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}