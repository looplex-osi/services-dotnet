using Looplex.DotNet.Core.Application.Abstractions.Services;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Looplex.DotNet.Services.Redis.ExtensionMethods;

public static class RedisExtensionMethods
{
    public static void AddRedisServices(this IServiceCollection services, string redisConnectionString)
    {
        services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(redisConnectionString));
        services.AddSingleton<IRedisService, RedisService>();
    }
    
    public static void AddRedisHealthChecks(this IServiceCollection services, string name = "Redis")
    {
        services.AddHealthChecks()
            .AddCheck<RedisHealthCheck>(name);
    }
}