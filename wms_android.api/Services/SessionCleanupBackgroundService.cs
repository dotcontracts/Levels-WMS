using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using wms_android.shared.Interfaces;

namespace wms_android.api.Services
{
    public class SessionCleanupBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SessionCleanupBackgroundService> _logger;
        
        // Run cleanup every 30 minutes
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(30);

        public SessionCleanupBackgroundService(
            IServiceProvider serviceProvider, 
            ILogger<SessionCleanupBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Session cleanup background service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PerformCleanup();
                    await Task.Delay(_cleanupInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when the service is stopping
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during session cleanup");
                    // Continue running even if cleanup fails
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Wait 5 minutes before retry
                }
            }

            _logger.LogInformation("Session cleanup background service stopped");
        }

        private async Task PerformCleanup()
        {
            using var scope = _serviceProvider.CreateScope();
            var sessionService = scope.ServiceProvider.GetRequiredService<ISessionService>();

            try
            {
                _logger.LogDebug("Starting session cleanup");
                var cleanedCount = await sessionService.CleanupExpiredSessionsAsync();
                
                if (cleanedCount > 0)
                {
                    _logger.LogInformation("Cleaned up {CleanedCount} expired sessions", cleanedCount);
                }
                else
                {
                    _logger.LogDebug("No expired sessions to clean up");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to perform session cleanup");
                throw;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Session cleanup background service is stopping");
            await base.StopAsync(cancellationToken);
        }
    }
}