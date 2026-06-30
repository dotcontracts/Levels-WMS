using Microsoft.AspNetCore.Mvc;
using wms_android.api.Interfaces;
using wms_android.shared.Interfaces;
using wms_android.shared.Models;
using wms_android.shared.Services;
using wms_android.shared.DTOs;
using System;
using System.Threading.Tasks;

namespace wms_android.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService; // API's DB-backed user service
        private readonly ITokenService _tokenService; // Inject API's ITokenService
        private readonly ISessionService _sessionService; // Session management

        public AuthController(IUserService userService, ITokenService tokenService, ISessionService sessionService)
        {
            _userService = userService;
            _tokenService = tokenService;
            _sessionService = sessionService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto loginRequest)
        {
            if (loginRequest == null || string.IsNullOrWhiteSpace(loginRequest.Username) || string.IsNullOrWhiteSpace(loginRequest.Password))
            {
                return BadRequest("Username, password, device info, and client type are required.");
            }

            // Use the API's UserService to check against DB
            var user = await _userService.AuthenticateAsync(loginRequest.Username, loginRequest.Password);

            if (user == null)
            {
                return Unauthorized("Invalid credentials.");
            }

            try
            {
                // Use the injected ITokenService to generate the token
                var token = await _tokenService.GenerateJwtTokenAsync(loginRequest.Username, loginRequest.Password);

                // Create a new session (this will automatically revoke any existing sessions)
                var sessionRequest = new CreateSessionRequest
                {
                    UserId = user.Id,
                    Username = user.Username,
                    DeviceInfo = loginRequest.DeviceInfo ?? "Unknown Device",
                    ClientType = loginRequest.ClientType ?? "Unknown",
                    IPAddress = GetClientIpAddress(),
                    UserAgent = Request.Headers.UserAgent.ToString()
                };

                var session = await _sessionService.CreateSessionAsync(sessionRequest);

                // Return token, user info, and session info
                return Ok(new
                {
                    Token = token,
                    Username = user.Username,
                    Role = user.Role?.Name ?? "User",
                    UserId = user.Id,
                    SessionId = session.SessionId,
                    SessionExpiresAt = session.ExpiresAt,
                    SessionRemainingMinutes = session.RemainingMinutes
                });
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred during authentication.");
            }
        }

        [HttpPost("verify-password")]
        public async Task<IActionResult> VerifyPassword([FromBody] VerifyPasswordRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Username and password are required.");
            }

            var user = await _userService.AuthenticateAsync(request.Username, request.Password);

            if (user == null)
            {
                return Unauthorized("Invalid credentials.");
            }

            return Ok(new
            {
                UserId = user.Id,
                Username = user.Username,
                Role = user.Role?.Name ?? "User"
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RevokeSessionRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.SessionId))
                {
                    return BadRequest("Session ID is required.");
                }

                var revoked = await _sessionService.RevokeSessionAsync(request.SessionId, "User logout");
                
                if (!revoked)
                {
                    return NotFound("Session not found.");
                }

                return Ok(new { message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred during logout.");
            }
        }

        private string GetClientIpAddress()
        {
            // Check for forwarded IP first (when behind proxy/load balancer)
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (!string.IsNullOrEmpty(forwardedFor))
                {
                    // Take the first IP address
                    return forwardedFor.Split(',')[0].Trim();
                }
            }

            // Check for real IP header
            if (Request.Headers.ContainsKey("X-Real-IP"))
            {
                var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
                if (!string.IsNullOrEmpty(realIp))
                {
                    return realIp;
                }
            }

            // Fall back to connection remote IP
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }
    }

    public class VerifyPasswordRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
