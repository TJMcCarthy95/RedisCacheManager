# Redis Cache Manager

A simple wrapper on [StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis) for connecting and interacting with a redis cache instance
offering APIs for fetching, setting and invalidating cache items.

---

## Registration

To register the required services simply call `AddCacheManager` on the service collection i.e:
  * Using hard coded values:
    ```c#
    services.AddCacheManager(config =>
    {
        config.ConnectionString = "localhost:6379";
    });

  * Binding with CacheManagerSettings from Configuration
    ```c#
    services.AddCacheManager(config => Configuration.GetSection("CacheManager").bind(config));
    ```

## CacheKey

The `CacheKey` class creates a unique key by suffixing the provide `key` with a named structure of the class in which is based off i.e:

```c#
var simpleCacheKey = new CacheKey<MyClass>("123"); // Produces: MyClass-123

var genericCacheKey = new CacheKey<IEnumerable<MyClass>>("123"); // Produces: IEnumerable`1<MyClass>-123

var nestedGenericCacheKey = new CacheKey<IDictionary<string, IEnumerable<MyClass>>>("123"); // Produces: IDictionary`2<String|IEnumerable`1<MyClass>>-123
```

## APIs

### GetCacheItemAsync

This API has two overloads:
* The first accepts a cacheKey and attempts to retrieve and deserialize the item from the cache.

    ```C#
    public Task<MyClass?> GetCachedByIdAsync(string id)
        => _cacheManager.GetCacheItemAsync(new CacheKey<MyClass>(id));
    ```

* The second overload provide additional parameters for retrieving and caching an item if not already in the cache.

  ```c#
  public Task<MyClass> GetCachedByIdAsync(string id)
      => _cacheManager.GetCacheItemAsync(
          new CacheKey<MyClass>(id),
          async () => await _repository.GetByIdAsync(id),
          TimeSpan.FromMinutes(10));
  ```
  
### CacheItemAsync

API for adding an item to the cache:
```c#
public async Task PreloadIntegrationData()
{
    var enumerator = _integration.GetAllResults().GetAsyncEnumerator();
    var tasks = new List<Task>();

    try
    {
        while (await enumerator.MoveNextAsync())
        {
            var currentResult = enumerator.Current;
            var item = _mapper.Map<ApiResponse, MyClass>(currentResult);
            tasks.Add(Task.Run(() => _cacheManager.CacheItemAsync(
                new CacheKey<MyClass>($"integration-{item.Id}"),
                item,
                TimeSpan.FromMinutes(30))));
        }
    }
    finally
    {
        await enumerator.DisposeAsync();
    }

    await Task.WhenAll(tasks);
}
```

### InvalidateCacheItemAsync

API for invalidating an item in the cache which matches exactly the derived key:

```c#
public async Task<MyClass> UpdateByIdAsync(string id, string property)
{
    var item = await _repository.SaveAsync((await _repository.GetByIdAsync(id)).Update(property));
    await _cacheManager.InvalidateCacheItemAsync(new CacheKey<MyClass>(item.Id));
    return item;
}
```

### InvalidateCacheItemsByKeyPatternAsync

API for invalidating items with a key that contains the provided key:

```c#
public Task InvalidateIntegrationData()
    =>_cacheManager.InvalidateCacheItemsByKeyPatternAsync("integration");
```