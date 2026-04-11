using System;
using System.Collections.Generic;

namespace wms_android.shared.Models
{
    public class DispatchSegment
    {
        public Guid Id { get; set; }
        public Guid DispatchId { get; set; }
        public Dispatch Dispatch { get; set; } = null!;
        public string Destination { get; set; } = string.Empty;
        public int TotalParcels { get; set; }
        public decimal TotalAmount { get; set; }
        public int SequenceNumber { get; set; }
        public List<Parcel> Parcels { get; set; } = new();
    }
}