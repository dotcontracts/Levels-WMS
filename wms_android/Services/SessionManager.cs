using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;
using wms_android.Interfaces;
using wms_android.shared.DTOs;
using wms_android.Extensions;

namespace wms_android.Services
{
    public class SessionManager : ISessionManager, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<SessionManager> _logger;
        private Timer? _sessionValidationTimer;
        private static readonly TimeSpan ValidationInterval = TimeSpan.FromMinutes(30); // Extend session every 30 minutes

        public event EventHandler? SessionExpired;
        public event EventHandler? UserLoggedOut;

        public SessionManager(HttpClient httpClient, ILogger<SessionManager> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task CreateSessionAsync(string sessionId, int userId, string username, DateTime expiresAt)
        {
            try
            {
                _logger?.LogInformation($"Creating session for user {username}");

                // Store session information in secure storage
                await SecureStorage.SetAsync("SessionId", sessionId);
                await SecureStorage.SetAsync("UserId", userId.ToString());
                await SecureStorage.SetAsync("Username", username);
                await SecureStorage.SetAsync("SessionExpiresAt", expiresAt.ToString("O")); // ISO 8601 format

                // Start periodic validation timer
                StartPeriodicValidation();

                _logger?.LogInformation($"Session {sessionId} created successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating session");
                throw;
            }
        }

        public async Task<bool> ValidateSessionAsync()
        {
            try
            {
                var sessionId = await SecureStorage.GetAsync("SessionId");
                var userIdStr = await SecureStorage.GetAsync("UserId");

                if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(userIdStr))
                {
                    _logger?.LogWarning("No session information found");
                    return false;
                }

                if (!int.TryParse(userIdStr, out var userId))
                {
                    _logger?.LogError("Invalid user ID in session storage");
                    return false;
                }

                var request = new SessionValidationRequest
                {
                    SessionId = sessionId,
                    UserId = userId
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Add session header using extension method
                await _httpClient.AddSessionHeaderAsync();

                var response = await _httpClient.PostAsync("/api/session/validate", content);

                if (response.IsSuccessStatusCode)
                {
                    _logger?.LogDebug($"Session {sessionId} is valid");
                    return true;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger?.LogWarning($"Session {sessionId} is invalid or expired");
                    await HandleSessionExpiry();
                    return false;
                }

                _logger?.LogError($"Session validation failed with status: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error validating session");
                return false;
            }
        }

        public async Task<bool> UpdateActivityAsync()
        {
            try
            {
                var sessionId = await SecureStorage.GetAsync("SessionId");
                if (string.IsNullOrEmpty(sessionId))
                {
                    return false;
                }

                var request = new UpdateSessionActivityRequest
                {
                    SessionId = sessionId
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Ensure session header is present using extension method
                await _httpClient.AddSessionHeaderAsync();

                var response = await _httpClient.PostAsync("/api/session/activity", content);

                if (response.IsSuccessStatusCode)
                {
                    _logger?.LogDebug($"Session activity updated for {sessionId}");
                    return true;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger?.LogWarning($"Session {sessionId} expired during activity update");
                    await HandleSessionExpiry();
                    return false;
                }

                _logger?.LogWarning($"Failed to update session activity: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating session activity");
                return false;
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                var sessionId = await SecureStorage.GetAsync("SessionId");
                if (!string.IsNullOrEmpty(sessionId))
                {
                    _logger?.LogInformation($"Logging out session {sessionId}");

                    var request = new RevokeSessionRequest
                    {
                        SessionId = sessionId,
                        Reason = "User logout"
                    };

                    var json = JsonSerializer.Serialize(request);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    // Add session header for the logout request using extension method
                    await _httpClient.AddSessionHeaderAsync();

                    var response = await _httpClient.PostAsync("/api/auth/logout", content);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger?.LogWarning($"Logout request failed: {response.StatusCode}");
                    }
                }

                // Clear session data regardless of API call result
                ClearSession();

                // Notify logout
                UserLoggedOut?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during logout");
                // Still clear session data even if logout call fails
                ClearSession();
                UserLoggedOut?.Invoke(this, EventArgs.Empty);
            }
        }

        public void ClearSession()
        {
            try
            {
                _logger?.LogInformation("Clearing session data");

                // Remove session headers from HTTP client using extension method
                _httpClient.RemoveSessionHeader();

                // Clear all session-related data from secure storage
                SecureStorage.Remove("SessionId");
                SecureStorage.Remove("UserId");
                SecureStorage.Remove("Username");
                SecureStorage.Remove("SessionExpiresAt");

                // Clear preferences as well
                Preferences.Remove("AuthToken");
                Preferences.Remove("CurrentUsername");
                Preferences.Remove("UserRole");
                Preferences.Remove("CurrentUserId");

                // Stop periodic validation
                StopPeriodicValidation();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error clearing session data");
            }
        }

        public string? GetCurrentSessionId()
        {
            try
            {
                return SecureStorage.GetAsync("SessionId").Result;
            }
            catch
            {
                return null;
            }
        }

        public bool HasActiveSession()
        {
            try
            {
                var sessionId = SecureStorage.GetAsync("SessionId").Result;
                var expiresAtStr = SecureStorage.GetAsync("SessionExpiresAt").Result;

                if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(expiresAtStr))
                {
                    return false;
                }

                if (DateTime.TryParse(expiresAtStr, out var expiresAt))
                {
                    return DateTime.UtcNow < expiresAt;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private async Task HandleSessionExpiry()
        {
            _logger?.LogWarning("Handling session expiry");
            
            // Clear session data
            ClearSession();
            
            // Notify session expiry on main thread
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                SessionExpired?.Invoke(this, EventArgs.Empty);
            });
        }

        private void StartPeriodicValidation()
        {
            if (_sessionValidationTimer != null)
            {
                StopPeriodicValidation();
            }

            _logger?.LogDebug("Starting periodic session validation every {Minutes} minutes", ValidationInterval.TotalMinutes);
            _sessionValidationTimer = new Timer(OnValidationTimerElapsed, null, ValidationInterval, ValidationInterval);
        }

        private void StopPeriodicValidation()
        {
            if (_sessionValidationTimer != null)
            {
                _logger?.LogDebug("Stopping periodic session validation");
                _sessionValidationTimer.Dispose();
                _sessionValidationTimer = null;
            }
        }

        private async void OnValidationTimerElapsed(object? state)
        {
            try
            {
                if (!HasActiveSession())
                {
                    StopPeriodicValidation();
                    return;
                }

                _logger?.LogDebug("Performing periodic session activity update");
                await UpdateActivityAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during periodic session activity update");
            }
        }

        public void Dispose()
        {
            StopPeriodicValidation();
        }
    }
}