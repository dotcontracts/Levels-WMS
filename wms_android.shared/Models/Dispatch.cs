using System;
using System.Collections.Generic;
using System.Linq;

namespace wms_android.shared.Models
{
    public class Dispatch
    {
        public Guid Id { get; set; }
        public string? DispatchCode { get; set; }
        public string SourceBranch { get; set; } = string.Empty;
        public string? Destination { get; set; } // Kept for backward compatibility, now primary destination
        public string VehicleNumber { get; set; } = string.Empty;
        public string Driver { get; set; } = string.Empty;
        public string? DriverPhone { get; set; } = string.Empty;
        public List<Guid> ParcelIds { get; set; } = new();
        public DateTime DispatchTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<Parcel> Parcels { get; set; } = new();
        public List<DispatchSegment> Segments { get; set; } = new();
        
        // Computed properties for backward compatibility
        public string EffectiveDestination => Destination ?? SourceBranch ?? "Unknown";
        
        public List<string> AllDestinations => Segments.Any() 
            ? Segments.Select(s => s.Destination).Distinct().ToList() 
            : new List<string> { EffectiveDestination };
        
        public int TotalParcels => Parcels.Count;
        public decimal TotalAmount => Parcels.Sum(p => p.TotalAmount);
    }
} 