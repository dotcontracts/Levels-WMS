namespace wms_android.cache.Interfaces
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key) where T : class;
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, IEnumerable<string>? tags = null) where T : class;
        Task RemoveAsync(string key);
        Task RemoveByTagAsync(string tag);
        Task<bool> ExistsAsync(string key);
        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, IEnumerable<string>? tags = null) where T : class;
    }
}
