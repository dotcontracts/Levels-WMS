using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using wms_android.shared.DTOs;
using wms_android.shared.Models;

namespace wms_android.shared.Interfaces
{
    public interface ISessionService
    {
        /// <summary>
        /// Creates a new session for a user. If user has existing active sessions, they will be revoked.
        /// </summary>
        /// <param name="request">Session creation request</param>
        /// <returns>New session response</returns>
        Task<SessionResponse> CreateSessionAsync(CreateSessionRequest request);
        
        /// <summary>
        /// Validates if a session is still active and valid
        /// </summary>
        /// <param name="sessionId">Session ID to validate</param>
        /// <param name="userId">User ID for additional validation</param>
        /// <returns>Session validation response</returns>
        Task<SessionValidationResponse> ValidateSessionAsync(string sessionId, int userId);
        
        /// <summary>
        /// Updates the last activity time for a session, extending its expiry
        /// </summary>
        /// <param name="sessionId">Session ID to update</param>
        /// <returns>True if session was found and updated</returns>
        Task<bool> UpdateSessionActivityAsync(string sessionId);
        
        /// <summary>
        /// Revokes a specific session
        /// </summary>
        /// <param name="sessionId">Session ID to revoke</param>
        /// <param name="reason">Optional reason for revocation</param>
        /// <returns>True if session was found and revoked</returns>
        Task<bool> RevokeSessionAsync(string sessionId, string? reason = null);
        
        /// <summary>
        /// Revokes all active sessions for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="reason">Optional reason for revocation</param>
        /// <returns>Number of sessions revoked</returns>
        Task<int> RevokeAllUserSessionsAsync(int userId, string? reason = null);
        
        /// <summary>
        /// Gets the active session for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Active session or null if none exists</returns>
        Task<SessionResponse?> GetActiveUserSessionAsync(int userId);
        
        /// <summary>
        /// Gets all sessions for a user (active and revoked)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="activeOnly">If true, returns only active sessions</param>
        /// <returns>List of sessions</returns>
        Task<List<SessionResponse>> GetUserSessionsAsync(int userId, bool activeOnly = false);
        
        /// <summary>
        /// Cleans up expired sessions from the database
        /// </summary>
        /// <returns>Number of sessions cleaned up</returns>
        Task<int> CleanupExpiredSessionsAsync();
        
        /// <summary>
        /// Gets session statistics
        /// </summary>
        /// <returns>Session statistics</returns>
        Task<SessionStatistics> GetSessionStatisticsAsync();
    }
    
    public class SessionStatistics
    {
        public int TotalActiveSessions { get; set; }
        public int TotalExpiredSessions { get; set; }
        public int TotalRevokedSessions { get; set; }
        public int AndroidSessions { get; set; }
        public int WebSessions { get; set; }
        public DateTime? OldestActiveSession { get; set; }
        public DateTime? NewestActiveSession { get; set; }
    }
}