using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using RedisCacheManager;
using StackExchange.Redis;
using Xunit;

namespace RedisCacheManagerTests
{
    public class CacheManagerTests
    {
        private readonly IFixture _fixture;

        private readonly IDatabase _database;
        private readonly IServer _server;

        private readonly ICacheManager _sut;

        public CacheManagerTests()
        {
            _fixture = new Fixture().Customize(new AutoNSubstituteCustomization());

            var options = _fixture.Freeze<IOptions<CacheManagerSettings>>();
            options.Value.ReturnsForAnyArgs(_fixture.Create<CacheManagerSettings>());

            _database = _fixture.Freeze<IDatabase>();
            _server = _fixture.Freeze<IServer>();

            var connectionMultiplexer = _fixture.Freeze<IConnectionMultiplexer>();
            connectionMultiplexer.GetDatabase().ReturnsForAnyArgs(_database);
            connectionMultiplexer.GetServer((string)default!).ReturnsForAnyArgs(_server);

            _sut = _fixture.Create<CacheManager>();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task GetCacheItemAsync_NoActionInvalidKey_ThrowsException(string key)
        {
            // Arrange
            var cacheKey = new CacheKey<TestClass>(key);
            
            // Act
            Func<Task<TestClass?>> result = async () => await _sut.GetCacheItemAsync(cacheKey);
            
            // Assert
            await result.Should().ThrowExactlyAsync<ArgumentException>().WithMessage("Provided cacheKey is invalid. (Parameter 'cacheKey')");
        }

        [Fact]
        public async Task GetCacheItemAsync_NoActionHasCachedItem_ReturnsFromCache()
        {
            // Arrange
            var cacheKey = new CacheKey<TestClass>(_fixture.Create<string>());

            var item = _fixture.Create<TestClass>();
            var redisValue = new RedisValue(JsonSerializer.Serialize(item));
            _database.StringGetAsync((string)default!).ReturnsForAnyArgs(redisValue);

            // Act
            var result = await _sut.GetCacheItemAsync(cacheKey);
            
            // Assert
            await _database.Received(1).StringGetAsync(cacheKey.Key);
            result.Should().BeEquivalentTo(item);
        }

        [Fact]
        public async Task GetCacheItemAsync_NoActionNoCachedItem_ReturnsNull()
        {
            // Arrange
            var cacheKey = new CacheKey<TestClass>(_fixture.Create<string>());

            _database.StringGetAsync((string)default!).ReturnsForAnyArgs(new RedisValue());

            // Act
            var result = await _sut.GetCacheItemAsync(cacheKey);
            
            // Assert
            await _database.Received(1).StringGetAsync(cacheKey.Key);
            result.Should().BeNull();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task GetCacheItemAsync_InvalidKey_ThrowsException(string key)
        {
            // Arrange
            var cacheKey = new CacheKey<TestClass>(key);
            Task<TestClass> Action()
                => Task.FromResult(_fixture.Create<TestClass>());

            var cacheExpiry = _fixture.Create<TimeSpan>();
            
            // Act
            Func<Task<TestClass>> result = async () => await _sut.GetCacheItemAsync(cacheKey, Action, cacheExpiry);
            
            // Assert
            await result.Should().ThrowExactlyAsync<ArgumentException>().WithMessage("Provided cacheKey is invalid. (Parameter 'cacheKey')");
        }

        [Fact]
        public async Task GetCacheItemAsync_HasCachedItem_ReturnsFromCache()
        {
            // Arrange
            var cacheKey = new CacheKey<TestClass>(_fixture.Create<string>());
            var action = Substitute.For<Func<Task<TestClass>>>();
            var cacheExpiry = _fixture.Create<TimeSpan>();

            var item = _fixture.Create<TestClass>();
            var redisValue = new RedisValue(JsonSerializer.Serialize(item));
            _database.StringGetAsync((string)default!).ReturnsForAnyArgs(redisValue);

            // Act
            var result = await _sut.GetCacheItemAsync(cacheKey, action, cacheExpiry);
            
            // Assert
            await _database.Received(1).StringGetAsync(cacheKey.Key);
            result.Should().BeEquivalentTo(item);
            action.DidNotReceiveWithAnyArgs();
        }

        [Fact]
        public async Task GetCacheItemAsync_NoCachedItem_InvokesActionAndCachesValue()
        {
            // Arrange
            var cacheKey = new CacheKey<TestClass>(_fixture.Create<string>());
            var action = Substitute.For<Func<Task<TestClass>>>();
            var cacheExpiry = _fixture.Create<TimeSpan>();
            
            var item = _fixture.Create<TestClass>();
            action.Invoke().ReturnsForAnyArgs(item);

            _database.StringGetAsync((string)default!).ReturnsForAnyArgs(new RedisValue());

            var serializedItem = JsonSerializer.Serialize(item);

            // Act
            var result = await _sut.GetCacheItemAsync(cacheKey, action, cacheExpiry);
            
            // Assert
            await _database.Received(1).StringGetAsync(cacheKey.Key);
            await action.Received(1).Invoke();
            await _database.Received(1).StringSetAsync(cacheKey.Key, serializedItem, cacheExpiry);
            result.Should().BeEquivalentTo(item);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task CacheItemAsync_InvalidKey_ThrowsException(string key)
        {
            // Arrange
            var cacheKey = new CacheKey<TestClass>(key);
            var item = _fixture.Create<TestClass>();
            var cacheExpiry = _fixture.Create<TimeSpan>();

            // Act
            Func<Task> result = async () => await _sut.CacheItemAsync(cacheKey, item, cacheExpiry);
            
            // Assert
            await result.Should().ThrowExactlyAsync<ArgumentException>().WithMessage("Provided cacheKey is invalid. (Parameter 'cacheKey')");
        }

        [Fact]
        public async Task CacheItemAsync_ValidKey_CachesItem()
        {
            // Arrange
            var cacheKey = _fixture.Create<CacheKey<TestClass>>();
            var item = _fixture.Create<TestClass>();
            var cacheExpiry = _fixture.Create<TimeSpan>();

            var serializedItem = JsonSerializer.Serialize(item);

            // Act
            await _sut.CacheItemAsync(cacheKey, item, cacheExpiry);
            
            // Assert
            await _database.Received(1).StringSetAsync(cacheKey.Key, serializedItem, cacheExpiry);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task InvalidateCacheItemAsync_InvalidKey_ThrowsException(string key)
        {
            // Arrange
            var cacheKey = new CacheKey<TestClass>(key);

            // Act
            Func<Task> result = async () => await _sut.InvalidateCacheItemAsync(cacheKey);
            
            // Assert
            await result.Should().ThrowExactlyAsync<ArgumentException>().WithMessage("Provided cacheKey is invalid. (Parameter 'cacheKey')");
            _database.DidNotReceiveWithAnyArgs();
        }

        [Fact]
        public async Task InvalidateCacheItemAsync_ValidKey_CachesItem()
        {
            // Arrange
            var cacheKey = _fixture.Create<CacheKey<TestClass>>();

            // Act
            await _sut.InvalidateCacheItemAsync(cacheKey);
            
            // Assert
            await _database.Received(1).KeyDeleteAsync(cacheKey.Key);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task InvalidateCacheItemsByKeyPatternAsync_InvalidKey_DoesNothing(string key)
        {
            // Act
            await _sut.InvalidateCacheItemsByKeyPatternAsync(key);
            
            // Assert
            _server.DidNotReceiveWithAnyArgs();
        }

        [Fact]
        public async Task InvalidateCacheItemsByKeyPatternAsync_ValidKey_CachesItem()
        {
            // Arrange
            var key = _fixture.Create<string>();

            var keys = new List<RedisKey> {new("1"), new("2"), new("3")};
            _server.KeysAsync(default).ReturnsForAnyArgs(keys.ToAsyncEnumerable());

            // Act
            await _sut.InvalidateCacheItemsByKeyPatternAsync(key);
            
            // Assert
            foreach (var redisKey in keys)
            {
                await _database.Received(1).KeyDeleteAsync(redisKey);
            }
        }
    }
}
