using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using wms_android.shared.Data;
using wms_android.shared.DTOs;
using wms_android.shared.Interfaces;
using wms_android.shared.Models;

namespace wms_android.shared.Services
{
    public class SessionService : ISessionService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SessionService> _logger;
        
        // Session configuration
        private static readonly TimeSpan SessionDuration = TimeSpan.FromHours(8);
        private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(1);
        
        public SessionService(AppDbContext context, ILogger<SessionService> logger)
        {
            _context = context;
            _logger = logger;
        }
        
        public async Task<SessionResponse> CreateSessionAsync(CreateSessionRequest request)
        {
            _logger.LogInformation($"Creating new session for user {request.Username} on {request.ClientType}");
            
            // Use EF Core's execution strategy for retry-compatible transactions
            var strategy = _context.Database.CreateExecutionStrategy();
            
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Revoke existing active sessions for this user on the same client type only
                    // This allows Web and Android sessions to coexist independently
                    var existingSessions = await _context.UserSessions
                        .Where(s => s.UserId == request.UserId && s.IsActive && s.ClientType == request.ClientType)
                        .ToListAsync();
                    
                    if (existingSessions.Any())
                    {
                        _logger.LogInformation($"Revoking {existingSessions.Count} existing sessions for user {request.Username}");
                        
                        foreach (var session in existingSessions)
                        {
                            session.IsActive = false;
                            session.RevokedReason = "New session created";
                            session.RevokedAt = DateTime.UtcNow;
                        }
                        
                        await _context.SaveChangesAsync();
                    }
                    
                    // Create new session
                    var newSession = new UserSession
                    {
                        SessionId = Guid.NewGuid().ToString(),
                        UserId = request.UserId,
                        Username = request.Username,
                        DeviceInfo = request.DeviceInfo,
                        ClientType = request.ClientType,
                        IPAddress = request.IPAddress,
                        UserAgent = request.UserAgent,
                        CreatedAt = DateTime.UtcNow,
                        LastActivityAt = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.Add(SessionDuration),
                        IsActive = true
                    };
                    
                    _context.UserSessions.Add(newSession);
                    await _context.SaveChangesAsync();
                    
                    await transaction.CommitAsync();
                    
                    _logger.LogInformation($"Successfully created session {newSession.SessionId} for user {request.Username}");
                    
                    return new SessionResponse
                    {
                        SessionId = newSession.SessionId,
                        Username = newSession.Username,
                        DeviceInfo = newSession.DeviceInfo,
                        ClientType = newSession.ClientType,
                        CreatedAt = newSession.CreatedAt,
                        ExpiresAt = newSession.ExpiresAt,
                        IsActive = newSession.IsActive,
                        RemainingMinutes = (int)newSession.TimeUntilExpiry.TotalMinutes
                    };
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, $"Failed to create session for user {request.Username}");
                    throw;
                }
            });
        }
        
        public async Task<SessionValidationResponse> ValidateSessionAsync(string sessionId, int userId)
        {
            _logger.LogDebug($"Validating session {sessionId} for user {userId}");
            
            var session = await _context.UserSessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.UserId == userId);
            
            if (session == null)
            {
                _logger.LogWarning($"Session {sessionId} not found for user {userId}");
                return new SessionValidationResponse
                {
                    IsValid = false,
                    ErrorMessage = "Session not found",
                    RequiresReauthentication = true
                };
            }
            
            // Check if session is active
            if (!session.IsActive)
            {
                _logger.LogWarning($"Session {sessionId} is not active (revoked: {session.RevokedReason})");
                return new SessionValidationResponse
                {
                    IsValid = false,
                    ErrorMessage = $"Session was revoked: {session.RevokedReason}",
                    RequiresReauthentication = true
                };
            }
            
            // Check if session is expired
            if (session.IsExpired)
            {
                _logger.LogWarning($"Session {sessionId} has expired");
                
                // Auto-revoke expired session
                session.IsActive = false;
                session.RevokedReason = "Session expired";
                session.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                return new SessionValidationResponse
                {
                    IsValid = false,
                    ErrorMessage = "Session has expired",
                    RequiresReauthentication = true
                };
            }
            
            _logger.LogDebug($"Session {sessionId} is valid");
            
            return new SessionValidationResponse
            {
                IsValid = true,
                Session = new SessionResponse
                {
                    SessionId = session.SessionId,
                    Username = session.Username,
                    DeviceInfo = session.DeviceInfo,
                    ClientType = session.ClientType,
                    CreatedAt = session.CreatedAt,
                    ExpiresAt = session.ExpiresAt,
                    IsActive = session.IsActive,
                    RemainingMinutes = (int)session.TimeUntilExpiry.TotalMinutes
                },
                RequiresReauthentication = false
            };
        }
        
        public async Task<bool> UpdateSessionActivityAsync(string sessionId)
        {
            _logger.LogDebug($"Updating activity for session {sessionId}");
            
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.IsActive);
            
            if (session == null || session.IsExpired)
            {
                _logger.LogDebug($"Session {sessionId} not found or expired, cannot update activity");
                return false;
            }
            
            // Update last activity and extend expiry
            session.LastActivityAt = DateTime.UtcNow;
            session.ExpiresAt = DateTime.UtcNow.Add(SessionDuration);
            
            await _context.SaveChangesAsync();
            
            _logger.LogDebug($"Updated activity for session {sessionId}, new expiry: {session.ExpiresAt}");
            return true;
        }
        
        public async Task<bool> RevokeSessionAsync(string sessionId, string? reason = null)
        {
            _logger.LogInformation($"Revoking session {sessionId}, reason: {reason ?? "Not specified"}");
            
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);
            
            if (session == null)
            {
                _logger.LogWarning($"Session {sessionId} not found for revocation");
                return false;
            }
            
            session.IsActive = false;
            session.RevokedReason = reason ?? "Manual revocation";
            session.RevokedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Successfully revoked session {sessionId}");
            return true;
        }
        
        public async Task<int> RevokeAllUserSessionsAsync(int userId, string? reason = null)
        {
            _logger.LogInformation($"Revoking all sessions for user {userId}, reason: {reason ?? "Not specified"}");
            
            var sessions = await _context.UserSessions
                .Where(s => s.UserId == userId && s.IsActive)
                .ToListAsync();
            
            foreach (var session in sessions)
            {
                session.IsActive = false;
                session.RevokedReason = reason ?? "All sessions revoked";
                session.RevokedAt = DateTime.UtcNow;
            }
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Successfully revoked {sessions.Count} sessions for user {userId}");
            return sessions.Count;
        }
        
        public async Task<SessionResponse?> GetActiveUserSessionAsync(int userId)
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive && !s.IsExpired);
            
            if (session == null)
                return null;
            
            return new SessionResponse
            {
                SessionId = session.SessionId,
                Username = session.Username,
                DeviceInfo = session.DeviceInfo,
                ClientType = session.ClientType,
                CreatedAt = session.CreatedAt,
                ExpiresAt = session.ExpiresAt,
                IsActive = session.IsActive,
                RemainingMinutes = (int)session.TimeUntilExpiry.TotalMinutes
            };
        }
        
        public async Task<List<SessionResponse>> GetUserSessionsAsync(int userId, bool activeOnly = false)
        {
            var query = _context.UserSessions.Where(s => s.UserId == userId);
            
            if (activeOnly)
            {
                query = query.Where(s => s.IsActive && !s.IsExpired);
            }
            
            var sessions = await query
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
            
            return sessions.Select(s => new SessionResponse
            {
                SessionId = s.SessionId,
                Username = s.Username,
                DeviceInfo = s.DeviceInfo,
                ClientType = s.ClientType,
                CreatedAt = s.CreatedAt,
                ExpiresAt = s.ExpiresAt,
                IsActive = s.IsActive,
                RemainingMinutes = s.IsActive ? (int)s.TimeUntilExpiry.TotalMinutes : 0
            }).ToList();
        }
        
        public async Task<int> CleanupExpiredSessionsAsync()
        {
            _logger.LogInformation("Starting cleanup of expired sessions");
            
            var expiredSessions = await _context.UserSessions
                .Where(s => s.IsActive && s.ExpiresAt < DateTime.UtcNow)
                .ToListAsync();
            
            foreach (var session in expiredSessions)
            {
                session.IsActive = false;
                session.RevokedReason = "Automatic cleanup - expired";
                session.RevokedAt = DateTime.UtcNow;
            }
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Cleaned up {expiredSessions.Count} expired sessions");
            return expiredSessions.Count;
        }
        
        public async Task<SessionStatistics> GetSessionStatisticsAsync()
        {
            var currentTime = DateTime.UtcNow;
            
            var stats = new SessionStatistics
            {
                TotalActiveSessions = await _context.UserSessions.CountAsync(s => s.IsActive && s.ExpiresAt > currentTime),
                TotalExpiredSessions = await _context.UserSessions.CountAsync(s => s.IsActive && s.ExpiresAt <= currentTime),
                TotalRevokedSessions = await _context.UserSessions.CountAsync(s => !s.IsActive),
                AndroidSessions = await _context.UserSessions.CountAsync(s => s.IsActive && s.ExpiresAt > currentTime && s.ClientType == "Android"),
                WebSessions = await _context.UserSessions.CountAsync(s => s.IsActive && s.ExpiresAt > currentTime && s.ClientType == "Web")
            };
            
            var activeSessions = await _context.UserSessions
                .Where(s => s.IsActive && s.ExpiresAt > currentTime)
                .Select(s => s.CreatedAt)
                .ToListAsync();
            
            if (activeSessions.Any())
            {
                stats.OldestActiveSession = activeSessions.Min();
                stats.NewestActiveSession = activeSessions.Max();
            }
            
            return stats;
        }
    }
}