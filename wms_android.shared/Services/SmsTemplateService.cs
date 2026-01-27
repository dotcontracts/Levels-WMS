using System;
using System.Diagnostics;
using wms_android.shared.Models;

namespace wms_android.shared.Services
{
    /// <summary>
    /// Service for generating SMS message templates based on parcel information
    /// </summary>
    public class SmsTemplateService
    {
        /// <summary>
        /// Generates a pickup notification message based on the parcel's payment method
        /// </summary>
        /// <param name="parcel">The parcel to generate notification for</param>
        /// <returns>Formatted message string</returns>
        public static string GeneratePickupNotification(Parcel parcel)
        {
            if (parcel == null)
            {
                Debug.WriteLine("Cannot generate SMS template for null parcel");
                return string.Empty;
            }
            
            // Hash the sender phone number partially for privacy
            string hashedSenderPhone = HashPhoneNumber(parcel.SenderTelephone);
            
            // Build the message based on payment method
            switch (parcel.PaymentMethods?.ToUpper() ?? string.Empty)
            {
                case "COD":
                    return $"Dear {parcel.Receiver}, your parcel {parcel.WaybillNumber} from {parcel.Sender} ({hashedSenderPhone}) " +
                           $"containing {parcel.Quantity} parcel(s) has been picked up for delivery to {parcel.Destination}. " +
                           $"Amount to pay on delivery Ksh {parcel.TotalAmount}. " +
                           $"Thank you for choosing Ficma Home Logistics.";
                    
                case "PAID":
                    return $"Dear {parcel.Receiver}, your parcel {parcel.WaybillNumber} from {parcel.Sender} ({hashedSenderPhone}) " +
                           $"containing {parcel.Quantity} parcel(s) has been picked up for delivery to {parcel.Destination}. " +
                           $"The parcel has been fully paid for. " +
                           $"Thank you for choosing Ficma Home Logistics.";
                    
                case "CONTRACT":
                    return $"Dear {parcel.Receiver}, your parcel {parcel.WaybillNumber} from {parcel.Sender} ({hashedSenderPhone}) " +
                           $"containing {parcel.Quantity} parcel(s) has been picked up for delivery to {parcel.Destination}. " +
                           $"Thank you for choosing Ficma Home Logistics.";
                    
                default:
                    // Default to a generic message if payment method not recognized
                    return $"Dear {parcel.Receiver}, your parcel {parcel.WaybillNumber} from {parcel.Sender} ({hashedSenderPhone}) " +
                           $"containing {parcel.Quantity} parcel(s) has been picked up for delivery to {parcel.Destination}. " +
                           $"Thank you for choosing Ficma Home Logistics.";
            }
        }
        
        /// <summary>
        /// Partially hashes a phone number for privacy while keeping it recognizable
        /// </summary>
        private static string HashPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
                return "Unknown";
                
            // Format: Show first 3 and last 2 digits, mask the rest with asterisks
            // e.g., 0712345678 -> 071****78
            if (phoneNumber.Length > 5)
            {
                int visiblePrefix = 3;
                int visibleSuffix = 2;
                
                string prefix = phoneNumber.Substring(0, Math.Min(visiblePrefix, phoneNumber.Length));
                string suffix = phoneNumber.Length > visibleSuffix 
                    ? phoneNumber.Substring(phoneNumber.Length - visibleSuffix) 
                    : "";
                    
                int maskedLength = Math.Max(0, phoneNumber.Length - visiblePrefix - visibleSuffix);
                string masked = new string('*', maskedLength);
                
                return prefix + masked + suffix;
            }
            
            // For very short numbers, just return as is
            return phoneNumber;
        }
        
        /// <summary>
        /// Generates a delivery confirmation message
        /// </summary>
        /// <param name="parcel">The parcel that was delivered</param>
        /// <returns>Formatted message string</returns>
        public static string GenerateDeliveryConfirmation(Parcel parcel)
        {
            if (parcel == null)
            {
                Debug.WriteLine("Cannot generate delivery confirmation for null parcel");
                return string.Empty;
            }
            
            return $"Dear {parcel.Sender}, your parcel {parcel.WaybillNumber} has been delivered to {parcel.Receiver} " +
                   $"at {parcel.Destination}. Thank you for choosing NID Home Logistics.";
        }
    }
} 