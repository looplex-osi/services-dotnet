using Looplex.DotNet.Core.Application.Abstractions.Services;
using StackExchange.Redis;

namespace Looplex.DotNet.Services.Redis;

public class RedisService(IConnectionMultiplexer connectionMultiplexer) : IRedisService
{
    const string? KeyCannotBeNullOrEmpty = "Key cannot be null or empty.";

    private IDatabase? _database;

    private IDatabase Database
    {
        get
        {
            if (_database == null)
            {
                _database = connectionMultiplexer.GetDatabase(); // Get a database instance
            }
            return _database;
        }
    } 

    public async Task SetAsync(string key, string value)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentException(KeyCannotBeNullOrEmpty);
        await Database.StringSetAsync(key, value);
    }

    public async Task<string?> GetAsync(string key)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentException(KeyCannotBeNullOrEmpty);
        var value = await Database.StringGetAsync(key);
        return value.IsNullOrEmpty ? null : value.ToString();
    }

    public async Task<bool> DeleteAsync(string key)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentException(KeyCannotBeNullOrEmpty);
        return await Database.KeyDeleteAsync(key);
    }
}