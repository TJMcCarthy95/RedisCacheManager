using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace RedisCacheManager
{
    [ExcludeFromCodeCoverage]
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCacheManager(this IServiceCollection services, Action<CacheManagerSettings> config)
        {
            services.Configure(config);
            services.AddSingleton<IConnectionMultiplexer>(sp
                => ConnectionMultiplexer.Connect(sp.GetService<IOptions<CacheManagerSettings>>()!.Value.ConnectionString));
            services.AddTransient<ICacheManager, CacheManager>();

            return services;
        }
    }
}
