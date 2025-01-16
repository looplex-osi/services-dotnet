using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Looplex.DotNet.Services.Redis;

public class RedisHealthCheck(IConnectionMultiplexer redisConnection) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var db = redisConnection.GetDatabase();
            var pingResult = await db.PingAsync();

            return HealthCheckResult.Healthy($"Redis is healthy. Ping: {pingResult.TotalMilliseconds}ms");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis is unreachable.", ex);
        }
    }
}