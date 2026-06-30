using Microsoft.Extensions.Caching.Memory;
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

            if (!string.IsNullOrEmpty(redisConnectionString))
            {
                try
                {
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
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠ Redis connection failed, falling back to in-memory cache: {ex.Message}");
                    RegisterMemoryFallback(services);
                }
            }
            else
            {
                Console.WriteLine("ℹ No Redis connection string configured — using in-memory cache fallback");
                RegisterMemoryFallback(services);
            }

            return services;
        }

        private static void RegisterMemoryFallback(IServiceCollection services)
        {
            services.AddMemoryCache(options =>
            {
                options.SizeLimit = 2048;
                options.CompactionPercentage = 0.25;
                options.ExpirationScanFrequency = TimeSpan.FromMinutes(5);
            });

            services.AddSingleton<ICacheService>(sp =>
                new MemoryCacheFallbackService(
                    sp.GetRequiredService<IMemoryCache>(),
                    sp.GetRequiredService<ILogger<MemoryCacheFallbackService>>()));
        }
    }
}
