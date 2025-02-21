using Looplex.DotNet.Core.Application.Abstractions.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Looplex.DotNet.Services.SqlDatabases;

public class SqlDatabasesHealthCheck(
    IHttpContextAccessor httpContextAccessor,
    IDbConnectionProvider dbConnectionProvider) : IHealthCheck
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
            var (dbConnection, databaseName) = await dbConnectionProvider.GetConnectionAsync(tenant.ToString());

            await using var conn = (SqlConnection) dbConnection;
            await conn.OpenAsync(cancellationToken);
            await conn.ChangeDatabaseAsync(databaseName, cancellationToken);

            return HealthCheckResult.Healthy($"Sql database for {tenant} is healthy.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Sql database is unreachable.", ex);
        }
    }
}