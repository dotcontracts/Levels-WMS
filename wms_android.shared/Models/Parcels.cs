using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace wms_android.shared.Models
{
    public class Parcel
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column(TypeName = "timestamp with time zone")]
        public DateTime CreatedAt { get; set; }
        public string? WaybillNumber { get; set; }
        public string? QRCode { get; set; }

        [Column(TypeName = "timestamp with time zone")]
        public DateTime? DispatchedAt { get; set; }

        public string? DispatchTrackingCode { get; set; }

        // User who created the parcel
        public int? CreatedById { get; set; }
        
        [ForeignKey("CreatedById")]
        [JsonIgnore] // Ignore navigation property to prevent circular reference
        public virtual User? CreatedBy { get; set; }

        public string? CreatorLastNameSnapshot { get; set; }

        // New Shipment relationship
        public Guid? ShipmentId { get; set; } 
        [ForeignKey("ShipmentId")]
        [JsonIgnore] // Ignore this navigation property to prevent circular reference
        public virtual Shipment? Shipment { get; set; }

        // Sender Details
        public string Sender { get; set; }
        public string SenderTelephone { get; set; }

        // Receiver Details
        public string Receiver { get; set; }
        public string ReceiverTelephone { get; set; }
        public string Destination { get; set; }

        // Parcel Description
        public int? Quantity { get; set; }
        public string Description { get; set; }
        public decimal? Amount { get; set; }
        public decimal? Rate { get; set; }
        public string PaymentMethods { get; set; }

        //was a Calculated Properties
        public decimal TotalAmount { get; set; }
        public decimal TotalRate { get; set; }
        
        // Payment tracking fields
        public decimal? AmountPaid { get; set; }
        public string? TransactionCode { get; set; }
        
        [Column(TypeName = "timestamp with time zone")]
        public DateTime? PaymentUpdatedAt { get; set; }
        
        public int? PaymentUpdatedById { get; set; }
        
        [ForeignKey("PaymentUpdatedById")]
        [JsonIgnore] // Ignore navigation property to prevent circular reference
        public virtual User? PaymentUpdatedBy { get; set; }
        
        // Status confirmation tracking fields
        public int? ConfirmedById { get; set; }
        
        [ForeignKey("ConfirmedById")]
        [JsonIgnore]
        public virtual User? ConfirmedBy { get; set; }

        [NotMapped]
        public string? ConfirmedByName { get; set; }
        
        [Column(TypeName = "timestamp with time zone")]
        public DateTime? ConfirmedAt { get; set; }

        // Dispatch tracking fields
        public int? DispatchedById { get; set; }

        [ForeignKey("DispatchedById")]
        [JsonIgnore]
        public virtual User? DispatchedBy { get; set; }

        [NotMapped]
        public string? DispatchedByName { get; set; }

        // Delivery tracking fields
        public int? DeliveredById { get; set; }

        [ForeignKey("DeliveredById")]
        [JsonIgnore]
        public virtual User? DeliveredBy { get; set; }

        [NotMapped]
        public string? DeliveredByName { get; set; }

        [Column(TypeName = "timestamp with time zone")]
        public DateTime? DeliveredAt { get; set; }
        
        // Status
        public ParcelStatus Status { get; set; } = ParcelStatus.Pending;
    }

    public enum ParcelStatus
    {
        Pending,
        Confirmed,
        InTransit,
        Delivered,
        Cancelled
    }
}
