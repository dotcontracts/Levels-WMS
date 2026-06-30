using Microsoft.Extensions.Logging;
using wms_android.cache;
using wms_android.cache.Interfaces;

namespace wms_android.api.Services
{
    public interface ICacheInvalidationService
    {
        Task InvalidateParcelsCache();
        Task InvalidateBranchesCache();
        Task InvalidateUsersCache();
        Task InvalidateStaticDataCache();
        Task InvalidateDispatchesCache();
        Task InvalidateUserDashboardCache(int userId);
    }

    public class CacheInvalidationService : ICacheInvalidationService
    {
        private readonly ICacheService _cache;
        private readonly ILogger<CacheInvalidationService> _logger;

        public CacheInvalidationService(ICacheService cache, ILogger<CacheInvalidationService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task InvalidateParcelsCache()
        {
            await _cache.RemoveByTagAsync(CacheTags.Parcels);
            _logger.LogInformation("Parcels cache invalidated");
        }

        public async Task InvalidateBranchesCache()
        {
            await _cache.RemoveByTagAsync(CacheTags.Branches);
            _logger.LogInformation("Branches cache invalidated");
        }

        public async Task InvalidateUsersCache()
        {
            await _cache.RemoveByTagAsync(CacheTags.Users);
            _logger.LogInformation("Users cache invalidated");
        }

        public async Task InvalidateStaticDataCache()
        {
            await _cache.RemoveByTagAsync(CacheTags.StaticData);
            _logger.LogInformation("Static data cache invalidated");
        }

        public async Task InvalidateDispatchesCache()
        {
            await _cache.RemoveByTagAsync(CacheTags.Dispatches);
            _logger.LogInformation("Dispatches cache invalidated");
        }

        public async Task InvalidateUserDashboardCache(int userId)
        {
            await _cache.RemoveByTagAsync(CacheKeys.UserDashboard.UserTag(userId));
            _logger.LogInformation("Dashboard cache invalidated for user {UserId}", userId);
        }
    }
}
