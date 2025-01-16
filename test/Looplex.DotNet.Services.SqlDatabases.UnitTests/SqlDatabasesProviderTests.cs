using FluentAssertions;
using Looplex.DotNet.Core.Application.Abstractions.Services;
using Looplex.DotNet.Middlewares.ScimV2.Domain.Entities.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Looplex.DotNet.Services.SqlDatabases.UnitTests;

[TestClass]
public class SqlDatabasesProviderTests
{
    private IConfiguration _configuration = null!;
    private ISecretsService _secretsService = null!;
    private ISqlDatabaseService _routingDatabaseService = null!;
    private SqlDatabasesProvider _sqlDatabasesProvider = null!;
    private IHostEnvironment _hostEnvironment = null!;
    private ILogger<SqlDatabasesProvider> _logger = null!;

    
    [TestInitialize]
    public void Setup()
    {
        _configuration = Substitute.For<IConfiguration>();
        _configuration["RoutingDatabaseConnectionString"] = "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;";
        _secretsService = Substitute.For<ISecretsService>();
        _routingDatabaseService = Substitute.For<ISqlDatabaseService>();
        _hostEnvironment = Substitute.For<IHostEnvironment>();
        _logger = Substitute.For<ILogger<SqlDatabasesProvider>>();
        _sqlDatabasesProvider = new SqlDatabasesProvider(_hostEnvironment, _logger, _configuration, _secretsService);
    }

    [TestMethod]
    public void GetDatabase_ShouldThrowError_WhenNoConnectionStringFoundForDomain()
    {
        // Arrange
        var domain = "example.com";
        var query = "query";
        
        _sqlDatabasesProvider.RoutingDatabaseService = _routingDatabaseService;
        
        _routingDatabaseService
            .QueryFirstOrDefaultAsync<string>(query, Arg.Any<object>())
            .Returns(Task.FromResult<string?>(null));

        // Act
        var act = () => _sqlDatabasesProvider.GetDatabaseAsync(domain);

        // Assert
        act.Should().ThrowAsync<Error>()
            .WithMessage($"Unable to connect to database for domain {domain}");
    }

    [TestMethod]
    public async Task GetDatabase_ShouldReturnDatabaseService_WhenConnectionStringFound()
    {
        // Arrange
        var domain = "example.com";
        var customerConnStringKeyVaultId = "keyvault-id";
        string customerConnString = "Server=localhost;Database=myDataBase;User Id=myUsername;Password=myPassword;";
        
        _sqlDatabasesProvider.RoutingDatabaseService = _routingDatabaseService;
        
        _routingDatabaseService
            .QueryFirstOrDefaultAsync<string>(Arg.Any<string>(), Arg.Any<object>())
            .Returns(customerConnStringKeyVaultId);

        _secretsService
            .GetSecretAsync(customerConnStringKeyVaultId)
            .Returns(customerConnString);

        // Act
        var result = await _sqlDatabasesProvider.GetDatabaseAsync(domain);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<SqlDatabaseService>();
    }

    [TestMethod]
    public async Task GetDatabase_ShouldThrowException_WhenKeyVaultIdNotFound()
    {
        // Arrange
        var domain = "example.com";
        var customerConnStringKeyVaultId = "keyvault-id";
        var customerConnString = "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;";

        _sqlDatabasesProvider.RoutingDatabaseService = _routingDatabaseService;
        
        _routingDatabaseService
            .QueryFirstOrDefaultAsync<string>(Arg.Any<string>(), Arg.Any<object>())
            .Returns((string?)null);

        _secretsService
            .GetSecretAsync(customerConnStringKeyVaultId)
            .Returns(customerConnString);

        // Act
        var action = () => _sqlDatabasesProvider.GetDatabaseAsync(domain);

        // Assert
        var ex = await Assert.ThrowsExceptionAsync<Error>(action);
        ex.Message.Should().Be("Unable to connect to database for domain example.com");
    }
}