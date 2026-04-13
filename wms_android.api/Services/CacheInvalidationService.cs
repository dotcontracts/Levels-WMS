using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Memory;

namespace wms_android.api.Services
{
    public interface ICacheInvalidationService
    {
        Task InvalidateParcelsCache();
        Task InvalidateBranchesCache();
        Task InvalidateUsersCache();
        Task InvalidateStaticDataCache();
        void InvalidateMemoryCache(string key);
    }

    public class CacheInvalidationService : ICacheInvalidationService
    {
        private readonly IOutputCacheStore _outputCacheStore;
        private readonly IMemoryCache _memoryCache;

        public CacheInvalidationService(IOutputCacheStore outputCacheStore, IMemoryCache memoryCache)
        {
            _outputCacheStore = outputCacheStore;
            _memoryCache = memoryCache;
        }

        public async Task InvalidateParcelsCache()
        {
            await _outputCacheStore.EvictByTagAsync("parcels", CancellationToken.None);
        }

        public async Task InvalidateBranchesCache()
        {
            await _outputCacheStore.EvictByTagAsync("branches", CancellationToken.None);
        }

        public async Task InvalidateUsersCache()
        {
            await _outputCacheStore.EvictByTagAsync("users", CancellationToken.None);
        }

        public async Task InvalidateStaticDataCache()
        {
            await _outputCacheStore.EvictByTagAsync("static-data", CancellationToken.None);
        }

        public void InvalidateMemoryCache(string key)
        {
            _memoryCache.Remove(key);
        }
    }
}