using Looplex.DotNet.Core.Application.Abstractions.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Looplex.DotNet.Services.SqlDatabases;

public class SqlDatabasesHealthCheck(
    IHttpContextAccessor httpContextAccessor,
    ISqlDatabaseProvider sqlDatabaseProvider) : IHealthCheck
{
    const string LooplexTenantKeyHeader = "X-looplex-tenant";
        
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (httpContextAccessor.HttpContext == null ||
                !httpContextAccessor.HttpContext.Request.Headers
                    .TryGetValue(LooplexTenantKeyHeader, out var tenant))
            {
                return HealthCheckResult.Unhealthy($"Missing {LooplexTenantKeyHeader} header.");
            }
            
            using var db = await sqlDatabaseProvider.GetDatabaseAsync(tenant.ToString());
            db.OpenConnection();

            return HealthCheckResult.Healthy($"Sql database for {tenant} is healthy.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Sql database is unreachable.", ex);
        }
    }
}