using System.Net;
using Looplex.DotNet.Core.Application.Abstractions.Providers;
using Looplex.DotNet.Core.Application.Abstractions.Services;
using Looplex.DotNet.Middlewares.ScimV2.Domain.Entities.Messages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Looplex.DotNet.Services.SqlDatabases;

public class SqlDatabasesProvider(
    IConfiguration configuration,
    ISecretsService secretsService) : ISqlDatabaseProvider
{
    const string RoutingDatabaseConnectionStringKey = "RoutingDatabaseConnectionString";

    private ISqlDatabaseService? _routingDatabaseService;

    internal ISqlDatabaseService RoutingDatabaseService
    {
        private get
        {
            if (_routingDatabaseService == null)
            {
                var routingConnString = configuration[RoutingDatabaseConnectionStringKey];

                var connection = new SqlConnection(routingConnString);
                _routingDatabaseService = new SqlDatabasesService(connection);
            }

            return _routingDatabaseService;
        }
        set => _routingDatabaseService = value;
    }
    
    public async Task<ISqlDatabaseService> GetDatabaseAsync(string domain)
    {
        var query = @"
            SELECT d.keyvault_id AS keyvault_id
                
            FROM lawoffice.databases d
            JOIN lawoffice.customers_databases cd ON 
                cd.database_id = d.id
            JOIN lawoffice.lawoffice.customers c ON 
                cd.customer_id = c.id
            WHERE 
                    c.domain = @Domain
                AND c.status = @Status
        ";

        var customerConnStringKeyVaultId = await RoutingDatabaseService
            .QueryFirstOrDefaultAsync<string>(query, new { Domain = domain, Status = (int)CustomerStatus.Active });

        if (string.IsNullOrEmpty(customerConnStringKeyVaultId))
            throw new Error($"Unable to connect to database for domain {domain}", (int)HttpStatusCode.InternalServerError);

        var customerConnString = await secretsService
            .GetSecretAsync(customerConnStringKeyVaultId);
        
        var connection = new SqlConnection(customerConnString);
        return new SqlDatabasesService(connection);
    }
}