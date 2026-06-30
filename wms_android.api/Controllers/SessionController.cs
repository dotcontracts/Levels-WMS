using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using wms_android.shared.DTOs;
using wms_android.shared.Interfaces;

namespace wms_android.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SessionController : ControllerBase
    {
        private readonly ISessionService _sessionService;
        private readonly ILogger<SessionController> _logger;

        public SessionController(ISessionService sessionService, ILogger<SessionController> logger)
        {
            _sessionService = sessionService;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new session for a user. Revokes any existing active sessions.
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Get client information from request
                request.IPAddress = GetClientIpAddress();
                request.UserAgent = Request.Headers.UserAgent.ToString();

                var sessionResponse = await _sessionService.CreateSessionAsync(request);
                
                _logger.LogInformation($"Session created successfully for user {request.Username}");
                return Ok(sessionResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating session for user {request.Username}");
                return StatusCode(500, new { error = "An error occurred while creating the session" });
            }
        }

        /// <summary>
        /// Validates if a session is still active and valid
        /// </summary>
        [HttpPost("validate")]
        [Authorize]
        public async Task<IActionResult> ValidateSession([FromBody] SessionValidationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var validationResponse = await _sessionService.ValidateSessionAsync(request.SessionId, request.UserId);
                
                if (!validationResponse.IsValid)
                {
                    return Unauthorized(validationResponse);
                }

                return Ok(validationResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating session {request.SessionId}");
                return StatusCode(500, new { error = "An error occurred while validating the session" });
            }
        }

        /// <summary>
        /// Updates the last activity time for a session, extending its expiry
        /// </summary>
        [HttpPost("activity")]
        [Authorize]
        public async Task<IActionResult> UpdateSessionActivity([FromBody] UpdateSessionActivityRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var updated = await _sessionService.UpdateSessionActivityAsync(request.SessionId);
                
                if (!updated)
                {
                    return NotFound(new { error = "Session not found or expired" });
                }

                return Ok(new { message = "Session activity updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating session activity for {request.SessionId}");
                return StatusCode(500, new { error = "An error occurred while updating session activity" });
            }
        }

        /// <summary>
        /// Revokes a specific session
        /// </summary>
        [HttpPost("revoke")]
        [Authorize]
        public async Task<IActionResult> RevokeSession([FromBody] RevokeSessionRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var revoked = await _sessionService.RevokeSessionAsync(request.SessionId, request.Reason);
                
                if (!revoked)
                {
                    return NotFound(new { error = "Session not found" });
                }

                return Ok(new { message = "Session revoked successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error revoking session {request.SessionId}");
                return StatusCode(500, new { error = "An error occurred while revoking the session" });
            }
        }

        /// <summary>
        /// Revokes all active sessions for a user
        /// </summary>
        [HttpPost("revoke-all/{userId}")]
        [Authorize]
        public async Task<IActionResult> RevokeAllUserSessions(int userId, [FromQuery] string? reason = null)
        {
            try
            {
                var revokedCount = await _sessionService.RevokeAllUserSessionsAsync(userId, reason);
                
                return Ok(new { message = $"Successfully revoked {revokedCount} sessions", revokedCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error revoking all sessions for user {userId}");
                return StatusCode(500, new { error = "An error occurred while revoking user sessions" });
            }
        }

        /// <summary>
        /// Gets the active session for a user
        /// </summary>
        [HttpGet("active/{userId}")]
        [Authorize]
        public async Task<IActionResult> GetActiveUserSession(int userId)
        {
            try
            {
                var session = await _sessionService.GetActiveUserSessionAsync(userId);
                
                if (session == null)
                {
                    return NotFound(new { error = "No active session found for user" });
                }

                return Ok(session);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting active session for user {userId}");
                return StatusCode(500, new { error = "An error occurred while retrieving the session" });
            }
        }

        /// <summary>
        /// Gets all sessions for a user
        /// </summary>
        [HttpGet("user/{userId}")]
        [Authorize]
        public async Task<IActionResult> GetUserSessions(int userId, [FromQuery] bool activeOnly = false)
        {
            try
            {
                var sessions = await _sessionService.GetUserSessionsAsync(userId, activeOnly);
                
                return Ok(sessions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting sessions for user {userId}");
                return StatusCode(500, new { error = "An error occurred while retrieving sessions" });
            }
        }

        /// <summary>
        /// Gets session statistics (Admin only)
        /// </summary>
        [HttpGet("statistics")]
        [Authorize] // Add role-based authorization here if needed
        public async Task<IActionResult> GetSessionStatistics()
        {
            try
            {
                var statistics = await _sessionService.GetSessionStatisticsAsync();
                
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting session statistics");
                return StatusCode(500, new { error = "An error occurred while retrieving session statistics" });
            }
        }

        /// <summary>
        /// Manually triggers cleanup of expired sessions (Admin only)
        /// </summary>
        [HttpPost("cleanup")]
        [Authorize] // Add role-based authorization here if needed
        public async Task<IActionResult> CleanupExpiredSessions()
        {
            try
            {
                var cleanedCount = await _sessionService.CleanupExpiredSessionsAsync();
                
                return Ok(new { message = $"Cleaned up {cleanedCount} expired sessions", cleanedCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired sessions");
                return StatusCode(500, new { error = "An error occurred while cleaning up sessions" });
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
}