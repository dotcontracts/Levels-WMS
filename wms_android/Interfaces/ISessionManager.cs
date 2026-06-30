using System;
using System.Threading.Tasks;

namespace wms_android.Interfaces
{
    public interface ISessionManager : IDisposable
    {
        /// <summary>
        /// Creates a new session after successful login
        /// </summary>
        /// <param name="sessionId">Session ID from login response</param>
        /// <param name="userId">User ID</param>
        /// <param name="username">Username</param>
        /// <param name="expiresAt">Session expiry date</param>
        Task CreateSessionAsync(string sessionId, int userId, string username, DateTime expiresAt);

        /// <summary>
        /// Validates if the current session is still active
        /// </summary>
        /// <returns>True if session is valid</returns>
        Task<bool> ValidateSessionAsync();

        /// <summary>
        /// Updates session activity (extends session)
        /// </summary>
        /// <returns>True if activity was updated successfully</returns>
        Task<bool> UpdateActivityAsync();

        /// <summary>
        /// Logs out and revokes the current session
        /// </summary>
        Task LogoutAsync();

        /// <summary>
        /// Clears all session data from local storage
        /// </summary>
        void ClearSession();

        /// <summary>
        /// Gets the current session ID
        /// </summary>
        /// <returns>Session ID or null if no session</returns>
        string? GetCurrentSessionId();

        /// <summary>
        /// Checks if user has an active session
        /// </summary>
        /// <returns>True if user has active session</returns>
        bool HasActiveSession();

        /// <summary>
        /// Event fired when session expires or becomes invalid
        /// </summary>
        event EventHandler SessionExpired;

        /// <summary>
        /// Event fired when user is logged out
        /// </summary>
        event EventHandler UserLoggedOut;
    }
}