using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Looplex.DotNet.Services.SqlDatabases;

public class RoutingDatabaseHealthCheck(IConfiguration configuration) : IHealthCheck
{
    const string RoutingDatabaseConnectionStringKey = "RoutingDatabaseConnectionString";

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var routingConnString = configuration[Constants.RoutingDatabaseConnectionStringKey];

            using var connection = new SqlConnection(routingConnString);
            connection.Open();
            
            return Task.FromResult(HealthCheckResult.Healthy($"Routing database is healthy."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Routing database is unreachable.", ex));
        }
    }
}