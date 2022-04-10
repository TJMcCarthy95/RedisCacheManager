using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace RedisCacheManager
{
    internal class CacheManager : ICacheManager
    {
        private readonly Lazy<IDatabase> _database;
        private readonly Lazy<IServer> _server;

        public CacheManager(
            IOptions<CacheManagerSettings> cacheManagerSettings,
            IConnectionMultiplexer connectionMultiplexer)
        {
            _database = new Lazy<IDatabase>(connectionMultiplexer.GetDatabase());
            _server = new Lazy<IServer>(connectionMultiplexer.GetServer(cacheManagerSettings.Value.ConnectionString));
        }

        private IDatabase Db => _database.Value;

        private IServer Server => _server.Value;

        public async Task<TItem?> GetCacheItemAsync<TItem>(CacheKey<TItem> cacheKey)
            where TItem : class
        {
            if (!cacheKey.IsValid)
            {
                throw new ArgumentException("Provided cacheKey is invalid.", nameof(cacheKey));
            }

            var result = await Db.StringGetAsync(cacheKey.Key);
            return result.HasValue
                ? JsonSerializer.Deserialize<TItem>(result)
                : default;
        }

        public async Task<TItem> GetCacheItemAsync<TItem>(
            CacheKey<TItem> cacheKey,
            Func<Task<TItem>> action,
            TimeSpan cacheExpiry)
            where TItem : class
        {
            var item = await GetCacheItemAsync(cacheKey);

            if (item != null)
            {
                return item;
            }

            item = await action.Invoke();
            await CacheItemAsync(cacheKey, item, cacheExpiry);
            return item;
        }

        public Task CacheItemAsync<TItem>(CacheKey<TItem> cacheKey, TItem item, TimeSpan cacheExpiry)
            where TItem : class
            => cacheKey.IsValid
                ? Db.StringSetAsync(cacheKey.Key, JsonSerializer.Serialize(item), cacheExpiry)
                : throw new ArgumentException("Provided cacheKey is invalid.", nameof(cacheKey));

        public Task InvalidateCacheItemAsync<TItem>(CacheKey<TItem> cacheKey)
            where TItem : class
            => cacheKey.IsValid
                ? Db.KeyDeleteAsync(cacheKey.Key)
                : throw new ArgumentException("Provided cacheKey is invalid.", nameof(cacheKey));

        public async Task InvalidateCacheItemsByKeyPatternAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            var keyEnumerator = Server.KeysAsync(pattern: $"*{key}*").GetAsyncEnumerator();
            var tasks = new List<Task>();

            try
            {
                while (await keyEnumerator.MoveNextAsync())
                {
                    var currentKey = keyEnumerator.Current;
                    tasks.Add(Task.Run(() => Db.KeyDeleteAsync(currentKey)));
                }
            }
            finally
            {
                await keyEnumerator.DisposeAsync();
            }

            await Task.WhenAll(tasks);
        }
    }
}
