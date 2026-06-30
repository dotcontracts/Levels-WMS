using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using wms_android.shared.Interfaces;

namespace wms_android.api.Middleware
{
    public class SessionValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SessionValidationMiddleware> _logger;
        
        // Endpoints that don't require session validation
        private readonly string[] _excludedPaths = {
            "/api/auth/login",
            "/api/session/create",
            "/api/session/validate",
            "/swagger",
            "/health",
            "/api/auth/register"
        };

        public SessionValidationMiddleware(RequestDelegate next, ILogger<SessionValidationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant();

            // Skip session validation for excluded paths
            if (_excludedPaths.Any(excluded => path?.StartsWith(excluded.ToLowerInvariant()) == true))
            {
                await _next(context);
                return;
            }

            // Skip session validation for OPTIONS requests (CORS preflight)
            if (context.Request.Method == "OPTIONS")
            {
                await _next(context);
                return;
            }

            // Check if endpoint requires authorization
            var endpoint = context.GetEndpoint();
            if (endpoint?.Metadata?.GetMetadata<AuthorizeAttribute>() == null && 
                endpoint?.Metadata?.GetMetadata<IAuthorizeData>() == null)
            {
                // Endpoint doesn't require authorization, skip session validation
                await _next(context);
                return;
            }

            // Get session service
            var sessionService = context.RequestServices.GetRequiredService<ISessionService>();

            // Extract session ID from custom header or JWT token
            var sessionId = context.Request.Headers["X-Session-ID"].FirstOrDefault();
            var userId = GetUserIdFromToken(context);

            if (string.IsNullOrEmpty(sessionId))
            {
                // Web panel requests don't send X-Session-ID — allow them through if JWT is valid
                if (userId != null)
                {
                    await _next(context);
                    return;
                }

                _logger.LogWarning("Missing session ID or user ID in request to {Path}", path);
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Session validation failed: Missing session information");
                return;
            }

            if (userId == null)
            {
                _logger.LogWarning("Missing user ID in request to {Path}", path);
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Session validation failed: Missing session information");
                return;
            }

            try
            {
                // Validate session
                var validationResult = await sessionService.ValidateSessionAsync(sessionId, userId.Value);

                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Invalid session {SessionId} for user {UserId}: {Error}", 
                        sessionId, userId, validationResult.ErrorMessage);
                    
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync($"Session validation failed: {validationResult.ErrorMessage}");
                    return;
                }

                // Update session activity (extend expiry)
                await sessionService.UpdateSessionActivityAsync(sessionId);

                // Add session info to context for controllers to use
                context.Items["SessionId"] = sessionId;
                context.Items["SessionInfo"] = validationResult.Session;

                _logger.LogDebug("Session {SessionId} validated successfully for user {UserId}", sessionId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating session {SessionId} for user {UserId}", sessionId, userId);
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("Internal server error during session validation");
                return;
            }

            await _next(context);
        }

        private int? GetUserIdFromToken(HttpContext context)
        {
            try
            {
                var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
                if (authHeader?.StartsWith("Bearer ") != true)
                    return null;

                var token = authHeader.Substring("Bearer ".Length).Trim();
                var handler = new JwtSecurityTokenHandler();
                
                if (!handler.CanReadToken(token))
                    return null;

                var jwtToken = handler.ReadJwtToken(token);
                var userIdClaim = jwtToken.Claims.FirstOrDefault(x => 
                    x.Type == ClaimTypes.NameIdentifier || 
                    x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" ||
                    x.Type == "sub" || 
                    x.Type == "userId" ||
                    x.Type == "nameid");
                
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
                    return userId;

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting user ID from JWT token");
                return null;
            }
        }
    }

    public static class SessionValidationMiddlewareExtensions
    {
        public static IApplicationBuilder UseSessionValidation(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SessionValidationMiddleware>();
        }
    }
}