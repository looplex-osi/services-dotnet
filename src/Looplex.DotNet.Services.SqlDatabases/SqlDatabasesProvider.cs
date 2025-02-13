using System.Net;
using Looplex.DotNet.Core.Application.Abstractions.Providers;
using Looplex.DotNet.Core.Application.Abstractions.Services;
using Looplex.DotNet.Middlewares.ScimV2.Domain.Entities.Messages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Looplex.DotNet.Services.SqlDatabases;

public class SqlDatabasesProvider(
    IHostEnvironment hostEnvironment,
    ILogger<SqlDatabasesProvider> logger,
    IConfiguration configuration,
    ISecretsService secretsService) : ISqlDatabaseProvider
{
    private ISqlDatabaseService? _routingDatabaseService;

    internal ISqlDatabaseService RoutingDatabaseService
    {
        private get
        {
            if (_routingDatabaseService == null)
            {
                var routingConnString = configuration[Constants.RoutingDatabaseConnectionStringKey];

                var connection = new SqlConnection(routingConnString);
                _routingDatabaseService = new SqlDatabaseService(connection);
            }

            return _routingDatabaseService;
        }
        set => _routingDatabaseService = value;
    }

    public async Task<ISqlDatabaseService> GetDatabaseAsync(string domain)
    {
        var query = @"
            SELECT d.name AS Name, d.keyvault_id AS KeyVaultId
                
            FROM lawoffice.databases d
            JOIN lawoffice.customers_databases cd ON 
                cd.database_id = d.id
            JOIN lawoffice.lawoffice.customers c ON 
                cd.customer_id = c.id
            WHERE 
                    c.domain = @Domain
                AND c.status = @Status
        ";

        var database = await RoutingDatabaseService
            .QueryFirstOrDefaultAsync<LawOfficeDatabase>(query, new { Domain = domain, Status = (int)CustomerStatus.Active });

        if (string.IsNullOrEmpty(database?.KeyVaultId) || string.IsNullOrEmpty(database?.Name))
        {
            logger.LogError("Unable to connect to database for tenant {Tenant}. Key vault id is null or empty", domain);
            throw new Error($"Unable to connect to database for domain {domain}",
                (int)HttpStatusCode.InternalServerError);
        }

        var customerConnString = await secretsService
            .GetSecretAsync(database.KeyVaultId);
        var databaseName = database.Name;
        
        try
        {
            var builder = new SqlConnectionStringBuilder(customerConnString);

            if (!hostEnvironment.IsDevelopment())
                builder.Password = "****";

            logger.LogInformation(
                "Database: Getting connection string for tenant {Tenant}. Result: {ConnectionString}",
                domain, builder.ToString());
        }
        catch (Exception e)
        {
            logger.LogError("Unable to connect to database for tenant {Tenant}. Exception: {Exception}", domain, e);
            throw new Error($"Unable to connect to database for tenant {domain}",
                (int)HttpStatusCode.InternalServerError, e);
        }
        
        var connection = new SqlConnection(customerConnString);
        var db = new SqlDatabaseService(connection);
        db.DatabaseName = databaseName;
        return db;
    }
}

internal class LawOfficeDatabase
{
    public string? Name { get; set; }
    public string? KeyVaultId { get; set; }
}