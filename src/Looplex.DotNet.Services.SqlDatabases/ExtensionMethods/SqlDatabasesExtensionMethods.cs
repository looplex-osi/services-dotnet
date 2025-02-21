using Looplex.DotNet.Core.Application.Abstractions.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Looplex.DotNet.Services.SqlDatabases.ExtensionMethods;

public static class SqlDatabasesExtensionMethods
{
    public static void AddSqlDatabaseServices(this IServiceCollection services)
    {
        services.AddSingleton<IDbConnectionProvider, DbConnectionProvider>();
    }
    
    public static void AddRoutingDatabaseHealthChecks(this IServiceCollection services, string name = "RoutingDatabase")
    {
        services.AddHealthChecks()
            .AddCheck<RoutingDatabaseHealthCheck>(name);
    }
    
    public static void AddSqlDatabaseHealthChecks(this IServiceCollection services, string name = "SqlDatabase")
    {
        services.AddHealthChecks()
            .AddCheck<SqlDatabasesHealthCheck>(name);
    }
}