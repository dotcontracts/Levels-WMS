using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;
using wms_android.cache.Interfaces;

namespace wms_android.cache.Services
{
    public class RedisCacheService : ICacheService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _db;
        private readonly ILogger<RedisCacheService> _logger;
        private readonly string _instanceName;
        private readonly TimeSpan _defaultExpiration = TimeSpan.FromMinutes(5);

        private static readonly JsonSerializerSettings _jsonSettings = new()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };

        public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger, string instanceName = "wms:")
        {
            _redis = redis;
            _db = redis.GetDatabase();
            _logger = logger;
            _instanceName = instanceName;
        }

        private string PrefixKey(string key) => $"{_instanceName}{key}";
        private string TagSetKey(string tag) => $"{_instanceName}tag:{tag}";

        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            try
            {
                var value = await _db.StringGetAsync(PrefixKey(key));
                if (!value.HasValue)
                    return null;

                return JsonConvert.DeserializeObject<T>(value!, _jsonSettings);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis GET failed for key: {Key}", key);
                return null;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, IEnumerable<string>? tags = null) where T : class
        {
            try
            {
                var prefixedKey = PrefixKey(key);
                var serialized = JsonConvert.SerializeObject(value, _jsonSettings);
                var ttl = expiration ?? _defaultExpiration;

                await _db.StringSetAsync(prefixedKey, serialized, ttl);

                if (tags != null)
                {
                    var batch = _db.CreateBatch();
                    var tasks = new List<Task>();
                    foreach (var tag in tags)
                    {
                        tasks.Add(batch.SetAddAsync(TagSetKey(tag), prefixedKey));
                        tasks.Add(batch.KeyExpireAsync(TagSetKey(tag), ttl.Add(TimeSpan.FromMinutes(1))));
                    }
                    batch.Execute();
                    await Task.WhenAll(tasks);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis SET failed for key: {Key}", key);
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                await _db.KeyDeleteAsync(PrefixKey(key));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis DELETE failed for key: {Key}", key);
            }
        }

        public async Task RemoveByTagAsync(string tag)
        {
            try
            {
                var tagKey = TagSetKey(tag);
                var members = await _db.SetMembersAsync(tagKey);

                if (members.Length == 0)
                    return;

                var batch = _db.CreateBatch();
                var tasks = members.Select(m => batch.KeyDeleteAsync((string)m!)).ToList();
                tasks.Add(batch.KeyDeleteAsync(tagKey));
                batch.Execute();
                await Task.WhenAll(tasks);

                _logger.LogInformation("Invalidated {Count} cache entries for tag: {Tag}", members.Length, tag);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis tag invalidation failed for tag: {Tag}", tag);
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            try
            {
                return await _db.KeyExistsAsync(PrefixKey(key));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis EXISTS failed for key: {Key}", key);
                return false;
            }
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
