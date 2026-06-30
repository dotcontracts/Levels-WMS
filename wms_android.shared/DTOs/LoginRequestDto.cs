using System;
using System.ComponentModel.DataAnnotations;

namespace wms_android.shared.DTOs
{
    public class LoginRequest
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }

        // Device information
        public string? DeviceSerialNumber { get; set; }
        public string? DeviceName { get; set; }
        public string? DeviceModel { get; set; }
        public string? DeviceManufacturer { get; set; }
        public string? AndroidVersion { get; set; }
        public string? AppVersion { get; set; }
        public string? Location { get; set; }
    }

    public class LoginRequestDto
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string ClientType { get; set; } = string.Empty; // "Android", "Web"

        public string? DeviceInfo { get; set; } // Device-specific information

        // Legacy device information (for backward compatibility)
        public string? DeviceSerialNumber { get; set; }
        public string? DeviceName { get; set; }
        public string? DeviceModel { get; set; }
        public string? DeviceManufacturer { get; set; }
        public string? AndroidVersion { get; set; }
        public string? AppVersion { get; set; }
        public string? Location { get; set; }
    }

    public class LogoutRequest
    {
        public Guid? SessionId { get; set; }
    }
}