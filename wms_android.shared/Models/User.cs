using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace wms_android.shared.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        public byte[] PasswordHash { get; set; }

        public byte[] PasswordSalt { get; set; }

        // Temporary property for backward compatibility with deprecated code
        // This should be removed once the deprecated wms_android.data project is fully migrated
        [NotMapped]
        public string? Password { get; set; }

        public DateTime CreatedAt { get; set; }

        [ForeignKey("Role")]
        public int RoleId { get; set; }

        public virtual Role Role { get; set; }

        // Add direct branch relationship
        [ForeignKey("Branch")]
        public int? BranchId { get; set; }

        public virtual Branch? Branch { get; set; }

        // User activation status
        public bool IsActive { get; set; } = true;

        // Per-user permission overrides
        public bool CanConfirm { get; set; } = false;
        public bool CanDispatch { get; set; } = false;

        // Override ToString for proper dropdown display
        public override string ToString()
        {
            return !string.IsNullOrEmpty(FirstName) && !string.IsNullOrEmpty(LastName) 
                ? $"{FirstName} {LastName}" 
                : Username;
        }
    }
}
