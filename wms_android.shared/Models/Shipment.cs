using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace wms_android.shared.Models
{
    public class Shipment
    {
        [Key]
        public Guid Id { get; set; } // Primary Key

        [Required]
        [StringLength(50)] // Adjust length as needed
        public string WaybillNumber { get; set; } // This will have the unique constraint

        public DateTimeOffset CreatedAt { get; set; }

        public int? CreatedById { get; set; } // Foreign key to User
        [ForeignKey("CreatedById")]
        [JsonIgnore] // Ignore navigation property to prevent circular reference
        public virtual User Creator { get; set; } // Navigation property to User

        [Required]
        public string CreatorLastNameSnapshot { get; set; } // Snapshot for performance/history

        [JsonIgnore] // Ignore navigation property to prevent circular reference
        public virtual ICollection<Parcel> Parcels { get; set; } // Navigation property for related parcels

        public Shipment()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTimeOffset.UtcNow;
            Parcels = new HashSet<Parcel>();
            CreatorLastNameSnapshot = "Unknown"; // Default value to prevent null constraint violations
        }
    }
} 