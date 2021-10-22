# ICacheStrategy
The `ICacheStrategy` type is an interface that defines a caching strategy for storing and retrieving data in a multi-tenant SQLite database. It provides a set of methods for getting, setting, removing, and clearing cached data, as well as retrieving cache statistics. Implementations of this interface, such as `LruCacheStrategy` and `TimeBasedCacheStrategy`, can be used to customize the caching behavior of the database.

## API
* `GetAsync<T>`: Retrieves a cached value of type `T` asynchronously. Parameters: none. Return value: `T`. Throws: not specified.
* `SetAsync<T>`: Sets a cached value of type `T` asynchronously. Parameters: not specified. Return value: `Task`. Throws: not specified.
* `RemoveAsync`: Removes a cached value asynchronously. Parameters: not specified. Return value: `Task`. Throws: not specified.
* `ClearAsync`: Clears all cached values asynchronously. Parameters: none. Return value: `Task`. Throws: not specified.
* `GetStatistics`: Retrieves cache statistics. Parameters: none. Return value: `Dictionary<string, CacheStatistics>`. Throws: not specified.
* `Value`: Gets the cached value. Parameters: none. Return value: `object`. Throws: not specified.
* `CreatedAt`: Gets the date and time when the cached value was created. Parameters: none. Return value: `DateTime`. Throws: not specified.
* `LastAccessedAt`: Gets the date and time when the cached value was last accessed. Parameters: none. Return value: `DateTime`. Throws: not specified.
* `ExpiresAt`: Gets the date and time when the cached value expires. Parameters: none. Return value: `DateTime?`. Throws: not specified.
* `AccessCount`: Gets the number of times the cached value has been accessed. Parameters: none. Return value: `int`. Throws: not specified.

## Usage
```csharp
// Example 1: Using LruCacheStrategy to cache a string value
var cacheStrategy = new LruCacheStrategy();
await cacheStrategy.SetAsync<string>("key", "value");
var cachedValue = await cacheStrategy.GetAsync<string>("key");
Console.WriteLine(cachedValue); // Output: value

// Example 2: Using TimeBasedCacheStrategy to cache an integer value with expiration
var cacheStrategy2 = new TimeBasedCacheStrategy();
await cacheStrategy2.SetAsync<int>("key", 42, DateTime.Now.AddMinutes(10));
var cachedValue2 = await cacheStrategy2.GetAsync<int>("key");
Console.WriteLine(cachedValue2); // Output: 42
```

## Notes
The `ICacheStrategy` interface does not specify thread-safety guarantees, so implementations should be designed with thread-safety in mind. Additionally, the `GetAsync` and `SetAsync` methods may throw exceptions if the cache is not properly initialized or if the cached value is not found. The `ExpiresAt` property may be null if the cached value does not have an expiration time. Implementations should handle these edge cases accordingly.
