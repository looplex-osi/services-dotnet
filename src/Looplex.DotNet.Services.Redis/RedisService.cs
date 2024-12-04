using Looplex.DotNet.Core.Application.Abstractions.Services;
using StackExchange.Redis;

namespace Looplex.DotNet.Services.Redis;

public class RedisService(IConnectionMultiplexer connectionMultiplexer) : IRedisService
{
    const string? KeyCannotBeNullOrEmpty = "Key cannot be null or empty.";

    private readonly IDatabase _database = connectionMultiplexer.GetDatabase(); // Get a database instance

    public async Task SetAsync(string key, string value)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentException(KeyCannotBeNullOrEmpty);
        await _database.StringSetAsync(key, value);
    }

    public async Task<string?> GetAsync(string key)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentException(KeyCannotBeNullOrEmpty);
        var value = await _database.StringGetAsync(key);
        return value.IsNullOrEmpty ? null : value.ToString();
    }

    public async Task<bool> DeleteAsync(string key)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentException(KeyCannotBeNullOrEmpty);
        return await _database.KeyDeleteAsync(key);
    }
}