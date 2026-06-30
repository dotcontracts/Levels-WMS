using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using wms_android.cache.Interfaces;

namespace wms_android.cache.Services
{
    public class MemoryCacheFallbackService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<MemoryCacheFallbackService> _logger;
        private readonly TimeSpan _defaultExpiration = TimeSpan.FromMinutes(5);

        private readonly Dictionary<string, HashSet<string>> _tagIndex = new();
        private readonly object _tagLock = new();

        public MemoryCacheFallbackService(IMemoryCache memoryCache, ILogger<MemoryCacheFallbackService> logger)
        {
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public Task<T?> GetAsync<T>(string key) where T : class
        {
            var value = _memoryCache.Get<T>(key);
            return Task.FromResult(value);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, IEnumerable<string>? tags = null) where T : class
        {
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? _defaultExpiration
            };

            _memoryCache.Set(key, value, options);

            if (tags != null)
            {
                lock (_tagLock)
                {
                    foreach (var tag in tags)
                    {
                        if (!_tagIndex.TryGetValue(tag, out var keys))
                        {
                            keys = new HashSet<string>();
                            _tagIndex[tag] = keys;
                        }
                        keys.Add(key);
                    }
                }
            }

            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key)
        {
            _memoryCache.Remove(key);
            return Task.CompletedTask;
        }

        public Task RemoveByTagAsync(string tag)
        {
            List<string> keysToRemove;

            lock (_tagLock)
            {
                if (!_tagIndex.TryGetValue(tag, out var keys))
                    return Task.CompletedTask;

                keysToRemove = keys.ToList();
                _tagIndex.Remove(tag);
            }

            foreach (var key in keysToRemove)
                _memoryCache.Remove(key);

            _logger.LogInformation("Invalidated {Count} memory cache entries for tag: {Tag}", keysToRemove.Count, tag);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string key)
        {
            return Task.FromResult(_memoryCache.TryGetValue(key, out _));
        }

        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, IEnumerable<string>? tags = null) where T : class
        {
            var cached = await GetAsync<T>(key);
            if (cached != null)
                return cached;

            var value = await factory();
            await SetAsync(key, value, expiration, tags);
            return value;
        }
    }
}
