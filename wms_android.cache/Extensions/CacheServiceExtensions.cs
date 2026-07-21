using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using wms_android.cache.Interfaces;
using wms_android.cache.Services;

namespace wms_android.cache.Extensions
{
    public static class CacheServiceExtensions
    {
        public static IServiceCollection AddWmsCaching(this IServiceCollection services, IConfiguration configuration)
        {
            var redisConnectionString = configuration["Redis:ConnectionString"];
            var instanceName = configuration["Redis:InstanceName"] ?? "wms:";

            if (string.IsNullOrEmpty(redisConnectionString))
                throw new InvalidOperationException("Redis:ConnectionString is not configured. Redis is required for caching.");

            var configOptions = ConfigurationOptions.Parse(redisConnectionString);
            configOptions.AbortOnConnectFail = false;
            configOptions.ConnectRetry = 3;
            configOptions.ConnectTimeout = 5000;
            configOptions.SyncTimeout = 5000;
            configOptions.ReconnectRetryPolicy = new ExponentialRetry(5000);

            var multiplexer = ConnectionMultiplexer.Connect(configOptions);
            services.AddSingleton<IConnectionMultiplexer>(multiplexer);
            services.AddSingleton<ICacheService>(sp =>
                new RedisCacheService(
                    sp.GetRequiredService<IConnectionMultiplexer>(),
                    sp.GetRequiredService<ILogger<RedisCacheService>>(),
                    instanceName));

            services.AddStackExchangeRedisCache(options =>
            {
                options.ConnectionMultiplexerFactory = () => Task.FromResult<IConnectionMultiplexer>(multiplexer);
                options.InstanceName = instanceName;
            });

            Console.WriteLine($"✓ Redis cache configured: {redisConnectionString.Split('@').Last()} (instance: {instanceName})");

            return services;
        }
    }
}
