using System;
using System.ComponentModel.DataAnnotations;

namespace wms_android.shared.Models
{
    public class UserSession
    {
        [Key]
        public string SessionId { get; set; } = string.Empty;
        
        public int UserId { get; set; }
        
        [Required]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        public string DeviceInfo { get; set; } = string.Empty;
        
        [Required]
        public string ClientType { get; set; } = string.Empty; // "Android", "Web"
        
        public string? IPAddress { get; set; }
        
        public string? UserAgent { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime LastActivityAt { get; set; }
        
        public DateTime ExpiresAt { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public string? RevokedReason { get; set; }
        
        public DateTime? RevokedAt { get; set; }
        
        // Navigation property
        public virtual User? User { get; set; }
        
        // Computed properties
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
        
        public bool IsValid => IsActive && !IsExpired;
        
        public TimeSpan TimeUntilExpiry => ExpiresAt > DateTime.UtcNow ? ExpiresAt - DateTime.UtcNow : TimeSpan.Zero;
        
        public TimeSpan TimeSinceLastActivity => DateTime.UtcNow - LastActivityAt;
    }
}