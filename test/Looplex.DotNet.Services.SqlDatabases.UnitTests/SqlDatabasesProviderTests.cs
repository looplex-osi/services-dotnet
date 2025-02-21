using Looplex.DotNet.Core.Application.Abstractions.Services;
using Looplex.DotNet.Middlewares.ScimV2.Domain.Entities.Messages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Looplex.DotNet.Core.Application.Abstractions.Providers;

namespace Looplex.DotNet.Services.SqlDatabases.UnitTests;

[TestClass]
[Ignore("Skipping all tests in this class until we configure ci/cd integration test (vpn for db access).")]
public class DbConnectionProviderTests
{
    private IHostEnvironment _hostEnvironment = null!;
    private ILogger<DbConnectionProvider> _logger = null!;
    private IConfiguration _configuration = null!;
    private ISecretsService _secretsService = null!;
    private IDbConnectionProvider _dbConnectionProvider = null!;

    private const string RoutingConnectionString = "Server=34.39.130.8;Database=LawOffice;User Id=sqlserver;Password=EF6i5jEAt3szuMKt5cdK;TrustServerCertificate=True;";
    private const string ConnString =
        "Server=35.199.80.88;User ID=sqlserver;Password=ezzyvdBFQIpRKG2iq1lv;TrustServerCertificate=True;";
    
    [TestInitialize]
    public void Setup()
    {
        _hostEnvironment = Substitute.For<IHostEnvironment>();
        _logger = Substitute.For<ILogger<DbConnectionProvider>>();
        _configuration = Substitute.For<IConfiguration>();
        _secretsService = Substitute.For<ISecretsService>();
        _configuration[Constants.RoutingDatabaseConnectionStringKey] = RoutingConnectionString;
        _dbConnectionProvider = new DbConnectionProvider(
            _hostEnvironment,
            _logger,
            _configuration,
            _secretsService
        );
    }

    [TestMethod]
    public async Task GetDatabase_ShouldReturnDatabaseService_WhenDatabaseIsCached()
    {
        var domain = "example.com";
        var expectedDatabase = new LawOfficeDatabase { Name = "TestDB", ConnectionString = ConnString };
        
        var dbProvider = (DbConnectionProvider)_dbConnectionProvider;
        dbProvider.GetType().GetField("_connectionStringsCache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(dbProvider, new Dictionary<string, LawOfficeDatabase> { { domain, expectedDatabase } });
        
        var (connection, dbName) = await _dbConnectionProvider.GetConnectionAsync(domain);
        
        Assert.AreEqual(expectedDatabase.Name, dbName);
        Assert.IsInstanceOfType(connection, typeof(SqlConnection));
    }
    
    [TestMethod]
    public async Task GetDatabase_ShouldThrowError_WhenNoConnectionStringFoundForDomain()
    {
        var domain = "invalid.com";
        await Assert.ThrowsExceptionAsync<Error>(async () => await _dbConnectionProvider.GetConnectionAsync(domain));
    }
    
    [TestMethod]
    public async Task GetDatabase_ShouldReturnDatabaseService_WhenConnectionStringFound()
    {
        var domain = "DominioTeste";
        var keyVaultId = "lawoffice-suporte-connection-string";
        
        _secretsService.GetSecretAsync(keyVaultId)!.Returns(Task.FromResult(ConnString));
        
        var (connection, dbName) = await _dbConnectionProvider.GetConnectionAsync(domain);
        
        Assert.IsNotNull(connection);
        Assert.AreEqual(ConnString, ((SqlConnection)connection).ConnectionString);
    }
    
    [TestMethod]
    public async Task GetDatabase_ShouldThrowException_WhenKeyVaultIdNotFound()
    {
        var domain = "example.com";
        
        _secretsService.GetSecretAsync(Arg.Any<string>())!.Returns(Task.FromResult<string>(null));
        
        await Assert.ThrowsExceptionAsync<Error>(async () => await _dbConnectionProvider.GetConnectionAsync(domain));
    }
    
    [TestMethod]
    public async Task GetDatabase_ShouldLogPassword_WhenIsDev()
    {
        var domain = "DominioTeste";
        var keyVaultId = "lawoffice-suporte-connection-string";
        
        _hostEnvironment.EnvironmentName.Returns(Environments.Development);
        _secretsService.GetSecretAsync(keyVaultId)!.Returns(Task.FromResult(ConnString));
        
        await _dbConnectionProvider.GetConnectionAsync(domain);
        
        string expectedCustomerConnString = new SqlConnectionStringBuilder(ConnString).ToString();
        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString() ==
                                $"Database: Getting connection string for tenant {domain}. Result: {expectedCustomerConnString}"),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }
    
    [TestMethod]
    public async Task GetDatabase_ShouldLogPassword_WhenIsNotDev()
    {
        var domain = "DominioTeste";
        var keyVaultId = "lawoffice-suporte-connection-string";
        
        _hostEnvironment.EnvironmentName.Returns(Environments.Production);
        _secretsService.GetSecretAsync(keyVaultId)!.Returns(Task.FromResult(ConnString));
        
        await _dbConnectionProvider.GetConnectionAsync(domain);

        var connStringBuilder = new SqlConnectionStringBuilder(ConnString);
        connStringBuilder.Password = "****";
        string expectedCustomerConnString = connStringBuilder.ToString();
        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString() ==
                                $"Database: Getting connection string for tenant {domain}. Result: {expectedCustomerConnString}"),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }
}
