using System;

namespace wms_android.shared.DTOs
{
    public class CreateSessionRequest
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string DeviceInfo { get; set; } = string.Empty;
        public string ClientType { get; set; } = string.Empty; // "Android", "Web"
        public string? IPAddress { get; set; }
        public string? UserAgent { get; set; }
    }
    
    public class SessionResponse
    {
        public string SessionId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string DeviceInfo { get; set; } = string.Empty;
        public string ClientType { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsActive { get; set; }
        public int RemainingMinutes { get; set; }
    }
    
    public class UpdateSessionActivityRequest
    {
        public string SessionId { get; set; } = string.Empty;
    }
    
    public class SessionValidationRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public int UserId { get; set; }
    }
    
    public class SessionValidationResponse
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public SessionResponse? Session { get; set; }
        public bool RequiresReauthentication { get; set; }
    }
    
    public class RevokeSessionRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }
}