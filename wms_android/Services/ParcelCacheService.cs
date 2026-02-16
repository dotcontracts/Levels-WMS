using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using wms_android.shared.Models;

namespace wms_android.Services
{
    /// <summary>
    /// Advanced caching service for parcel data with automatic expiration and memory management
    /// </summary>
    public class ParcelCacheService
    {
        private readonly ILogger<ParcelCacheService> _logger;
        
        // Cache storage with thread-safe collections
        private readonly ConcurrentDictionary<string, CachedItem<PaginatedResponse<Parcel>>> _parcelCache = new();
        private readonly ConcurrentDictionary<string, CachedItem<IEnumerable<string>>> _destinationsCache = new();
        private readonly ConcurrentDictionary<string, CachedItem<IEnumerable<string>>> _clerksCache = new();
        
        // Cache settings
        private readonly TimeSpan _defaultExpiration = TimeSpan.FromMinutes(5);
        private readonly TimeSpan _destinationsExpiration = TimeSpan.FromMinutes(15); // Destinations change less frequently
        private readonly int _maxCacheItems = 100; // Prevent memory bloat on mobile

        public ParcelCacheService(ILogger<ParcelCacheService> logger)
        {
            _logger = logger;
            
            // Start cleanup timer to prevent memory leaks
            _ = Task.Run(async () => await StartCleanupTimer());
        }

        #region Parcel Data Caching

        public void CacheParcels(string key, PaginatedResponse<Parcel> data, TimeSpan? customExpiration = null)
        {
            var expiration = customExpiration ?? _defaultExpiration;
            var item = new CachedItem<PaginatedResponse<Parcel>>(data, DateTime.UtcNow.Add(expiration));
            
            _parcelCache.AddOrUpdate(key, item, (k, v) => item);
            
            // Cleanup if cache is getting too large
            if (_parcelCache.Count > _maxCacheItems)
            {
                CleanupExpiredItems();
            }
            
            _logger.LogDebug($"Cached {data.Data?.Count() ?? 0} parcels with key: {key}");
        }

        public PaginatedResponse<Parcel>? GetCachedParcels(string key)
        {
            if (_parcelCache.TryGetValue(key, out var item) && !item.IsExpired)
            {
                _logger.LogDebug($"Cache hit for key: {key}");
                return item.Data;
            }

            if (item?.IsExpired == true)
            {
                _parcelCache.TryRemove(key, out _);
                _logger.LogDebug($"Removed expired cache item: {key}");
            }

            return null;
        }

        public void InvalidateParcelCache(string pattern = "")
        {
            var keysToRemove = string.IsNullOrEmpty(pattern)
                ? _parcelCache.Keys.ToList()
                : _parcelCache.Keys.Where(k => k.Contains(pattern)).ToList();

            foreach (var key in keysToRemove)
            {
                _parcelCache.TryRemove(key, out _);
            }

            _logger.LogInformation($"Invalidated {keysToRemove.Count} parcel cache items with pattern: '{pattern}'");
        }

        #endregion

        #region Destinations Caching

        public void CacheDestinations(int userId, IEnumerable<string> destinations)
        {
            var key = $"destinations_user_{userId}";
            var item = new CachedItem<IEnumerable<string>>(destinations, DateTime.UtcNow.Add(_destinationsExpiration));
            
            _destinationsCache.AddOrUpdate(key, item, (k, v) => item);
            _logger.LogDebug($"Cached {destinations.Count()} destinations for user {userId}");
        }

        public IEnumerable<string>? GetCachedDestinations(int userId)
        {
            var key = $"destinations_user_{userId}";
            
            if (_destinationsCache.TryGetValue(key, out var item) && !item.IsExpired)
            {
                _logger.LogDebug($"Cache hit for destinations user {userId}");
                return item.Data;
            }

            if (item?.IsExpired == true)
            {
                _destinationsCache.TryRemove(key, out _);
                _logger.LogDebug($"Removed expired destinations cache for user {userId}");
            }

            return null;
        }

        #endregion

        #region Clerks Caching

        public void CacheClerks(string destination, IEnumerable<string> clerks)
        {
            var key = $"clerks_{destination}";
            var item = new CachedItem<IEnumerable<string>>(clerks, DateTime.UtcNow.Add(_defaultExpiration));
            
            _clerksCache.AddOrUpdate(key, item, (k, v) => item);
            _logger.LogDebug($"Cached {clerks.Count()} clerks for destination: {destination}");
        }

        public IEnumerable<string>? GetCachedClerks(string destination)
        {
            var key = $"clerks_{destination}";
            
            if (_clerksCache.TryGetValue(key, out var item) && !item.IsExpired)
            {
                _logger.LogDebug($"Cache hit for clerks destination: {destination}");
                return item.Data;
            }

            if (item?.IsExpired == true)
            {
                _clerksCache.TryRemove(key, out _);
                _logger.LogDebug($"Removed expired clerks cache for destination: {destination}");
            }

            return null;
        }

        #endregion

        #region Cache Management

        public string GenerateParcelCacheKey(int? userId, ParcelQueryParams queryParams)
        {
            var keyParts = new List<string>();
            
            if (userId.HasValue)
                keyParts.Add($"user_{userId}");
            
            keyParts.Add($"page_{queryParams.Page}");
            keyParts.Add($"size_{queryParams.PageSize}");
            
            if (!string.IsNullOrEmpty(queryParams.Destination))
                keyParts.Add($"dest_{queryParams.Destination}");
            
            if (queryParams.Status.HasValue)
                keyParts.Add($"status_{queryParams.Status}");
            
            if (queryParams.Statuses?.Any() == true)
                keyParts.Add($"statuses_{string.Join(",", queryParams.Statuses)}");
            
            if (queryParams.FromDate.HasValue)
                keyParts.Add($"from_{queryParams.FromDate.Value:yyyyMMdd}");
            
            if (queryParams.ToDate.HasValue)
                keyParts.Add($"to_{queryParams.ToDate.Value:yyyyMMdd}");
            
            if (!string.IsNullOrEmpty(queryParams.CreatedByUsername))
                keyParts.Add($"clerk_{queryParams.CreatedByUsername}");
            
            if (!string.IsNullOrEmpty(queryParams.SearchTerm))
                keyParts.Add($"search_{queryParams.SearchTerm.GetHashCode()}");
            
            return string.Join("|", keyParts);
        }

        public void ClearAll()
        {
            _parcelCache.Clear();
            _destinationsCache.Clear();
            _clerksCache.Clear();
            _logger.LogInformation("Cleared all cache data");
        }

        public CacheStats GetCacheStats()
        {
            return new CacheStats
            {
                ParcelCacheCount = _parcelCache.Count,
                DestinationsCacheCount = _destinationsCache.Count,
                ClerksCacheCount = _clerksCache.Count,
                ExpiredParcelItems = _parcelCache.Values.Count(v => v.IsExpired),
                ExpiredDestinationItems = _destinationsCache.Values.Count(v => v.IsExpired),
                ExpiredClerkItems = _clerksCache.Values.Count(v => v.IsExpired)
            };
        }

        private void CleanupExpiredItems()
        {
            var expiredParcelKeys = _parcelCache.Where(kvp => kvp.Value.IsExpired).Select(kvp => kvp.Key).ToList();
            var expiredDestinationKeys = _destinationsCache.Where(kvp => kvp.Value.IsExpired).Select(kvp => kvp.Key).ToList();
            var expiredClerkKeys = _clerksCache.Where(kvp => kvp.Value.IsExpired).Select(kvp => kvp.Key).ToList();

            foreach (var key in expiredParcelKeys)
                _parcelCache.TryRemove(key, out _);
            
            foreach (var key in expiredDestinationKeys)
                _destinationsCache.TryRemove(key, out _);
            
            foreach (var key in expiredClerkKeys)
                _clerksCache.TryRemove(key, out _);

            var totalRemoved = expiredParcelKeys.Count + expiredDestinationKeys.Count + expiredClerkKeys.Count;
            if (totalRemoved > 0)
            {
                _logger.LogDebug($"Cleaned up {totalRemoved} expired cache items");
            }
        }

        private async Task StartCleanupTimer()
        {
            while (true)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(5)); // Cleanup every 5 minutes
                    CleanupExpiredItems();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during cache cleanup");
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Cached item wrapper with expiration
    /// </summary>
    internal class CachedItem<T>
    {
        public T Data { get; }
        public DateTime ExpiresAt { get; }
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;

        public CachedItem(T data, DateTime expiresAt)
        {
            Data = data;
            ExpiresAt = expiresAt;
        }
    }

    /// <summary>
    /// Cache statistics for monitoring
    /// </summary>
    public class CacheStats
    {
        public int ParcelCacheCount { get; set; }
        public int DestinationsCacheCount { get; set; }
        public int ClerksCacheCount { get; set; }
        public int ExpiredParcelItems { get; set; }
        public int ExpiredDestinationItems { get; set; }
        public int ExpiredClerkItems { get; set; }
        
        public int TotalItems => ParcelCacheCount + DestinationsCacheCount + ClerksCacheCount;
        public int TotalExpiredItems => ExpiredParcelItems + ExpiredDestinationItems + ExpiredClerkItems;
    }
}