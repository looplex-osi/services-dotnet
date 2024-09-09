using System.Dynamic;
using FluentAssertions;
using Looplex.DotNet.Core.Common.Exceptions;
using Looplex.DotNet.Core.Domain;
using Looplex.DotNet.Middlewares.Clients.Application.Abstractions.Services;
using Looplex.DotNet.Middlewares.ScimV2.Domain.Entities.Schemas;
using Looplex.DotNet.Middlewares.Clients.Domain.Entities.Clients;
using Looplex.DotNet.Services.Clients.InMemory.Services;
using Looplex.OpenForExtension.Abstractions.Contexts;
using Newtonsoft.Json;
using NSubstitute;

namespace Looplex.DotNet.Services.Clients.InMemory.UnitTests.Services;

[TestClass]
public class ClientServiceTests
{
    private IClientService _clientService = null!;
    private IContext _context = null!;
    private CancellationToken _cancellationToken;

    [TestInitialize]
    public void Setup()
    {
        ClientService.Clients = [];
        _clientService = new ClientService();
        _context = Substitute.For<IContext>();
        var state = new ExpandoObject();
        _context.State.Returns(state);
        var roles = new Dictionary<string, dynamic>();
        _context.Roles.Returns(roles);
        _cancellationToken = new CancellationToken();
    }

    [TestMethod]
    public async Task GetAllAsync_ShouldReturnPaginatedCollection()
    {
        // Arrange
        _context.State.Pagination = new ExpandoObject();
        _context.State.Pagination.Page = 1;
        _context.State.Pagination.PerPage = 10;
        var existingClient = new Client
        {
            Id = null,
            UniqueId = Guid.NewGuid(),
            Secret = "secret1",
            DisplayName = "displayName1",
            ExpirationTime = new DateTimeOffset(2024, 12,20,0,0,0,TimeSpan.Zero),
            NotBefore = new DateTimeOffset(2024, 12,1,0,0,0,TimeSpan.Zero),
        };
        ClientService.Clients.Add(existingClient);
            
        // Act
        await _clientService.GetAllAsync(_context, _cancellationToken);

        // Assert
        var result = JsonConvert.DeserializeObject<PaginatedCollection>((string)_context.Result!)!;
        Assert.AreEqual(1, result.TotalCount);
        JsonConvert.DeserializeObject<Client>(result.Records[0].ToString()!).Should()
            .BeEquivalentTo(existingClient, options => options
            .Using<DateTime>(ctx => ctx.Subject.ToUniversalTime().Should().Be(ctx.Expectation.ToUniversalTime()))
            .WhenTypeIs<DateTime>());
    }

    [TestMethod]
    public async Task GetByIdAsync_ShouldThrowEntityNotFoundException_WhenClientDoesNotExist()
    {
        // Arrange
        _context.State.Id = Guid.NewGuid().ToString();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<EntityNotFoundException>(() => _clientService.GetByIdAsync(_context, _cancellationToken));
    }

    [TestMethod]
    public async Task GetByIdAsync_ShouldReturnClient_WhenClientDoesExist()
    {
        // Arrange
        var existingClient = new Client
        {
            Id = null,
            UniqueId = Guid.NewGuid(),
            Secret = "secret1",
            DisplayName = "displayName1"
        };
        _context.State.Id = existingClient.UniqueId.ToString()!;
        ClientService.Clients.Add(existingClient);

        // Act
        await _clientService.GetByIdAsync(_context, _cancellationToken);
            
        // Assert
        JsonConvert.DeserializeObject<Client>(_context.Result!.ToString()!).Should().BeEquivalentTo(existingClient);
    }
    
    [TestMethod]
    public async Task CreateAsync_ShouldAddClientToList()
    {
        // Arrange
        var id = Guid.NewGuid();
        var clientJson = $"{{ \"id\": \"{id}\", \"secret\": \"Test Client\" }}";
        _context.State.Resource = clientJson;
        Schema.Add<Client>("{}");
        
        // Act
        await _clientService.CreateAsync(_context, _cancellationToken);

        // Assert
        Assert.AreEqual(id, _context.Result);
        ClientService.Clients.Should().Contain(u => u.UniqueId == id);
    }

    [TestMethod]
    public async Task PatchAsync_ShouldApplyOperationsToClient()
    {
        // Arrange
        var existingClient = new Client
        {
            Id = 1,
            UniqueId = Guid.NewGuid(),
            Secret = "secret1",
            DisplayName = "displayName1"
        };
        ClientService.Clients.Add(existingClient);
        _context.State.Operations = "[ { \"op\": \"add\", \"path\": \"Secret\", \"value\": \"Updated Client\" } ]";
        _context.State.Id = existingClient.UniqueId.ToString()!;

        // Act
        await _clientService.PatchAsync(_context, _cancellationToken);

        // Assert
        var client = ClientService.Clients.First(u => u.UniqueId == existingClient.UniqueId);
        client.Secret.Should().Be("Updated Client");
        ((Client)_context.Roles["Client"]).ChangedPropertyNotification.ChangedProperties.Should().BeEquivalentTo(["Secret"]);
    }
    
    [TestMethod]
    public async Task DeleteAsync_ShouldThrowEntityNotFoundException_WhenClientDoesNotExist()
    {
        // Arrange
        _context.State.Id = Guid.NewGuid().ToString();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<EntityNotFoundException>(() => _clientService.DeleteAsync(_context, _cancellationToken));
    }
    
    [TestMethod]
    public async Task DeleteAsync_ShouldRemoveClientFromList_WhenClientDoesExist()
    {
        // Arrange
        var existingClient = new Client
        {
            Id = 1,
            UniqueId = Guid.NewGuid(),
            Secret = "secret1",
            DisplayName = "displayName1"
        };
        _context.State.Id = existingClient.UniqueId.ToString()!;
        ClientService.Clients.Add(existingClient);

        // Act
        await _clientService.DeleteAsync(_context, _cancellationToken);

        // Assert
        ClientService.Clients.Should().NotContain(u => u.Id == existingClient.Id);
    }
    
    [TestMethod]
    public async Task GetByIdAndSecretOrDefaultAsync_ShouldReturnNull_WhenClientDoesNotExist()
    {
        // Arrange
        _context.State.ClientId = Guid.NewGuid().ToString();
        _context.State.ClientSecret = Guid.NewGuid().ToString();

        // Act
        await _clientService.GetByIdAndSecretOrDefaultAsync(_context, _cancellationToken);
            
        // Assert
        _context.Roles.Should().NotContainKey("Client");
        Assert.IsNull(_context.Result);
    }
    
    [TestMethod]
    public async Task GetByIdAndSecretOrDefaultAsync_ShouldRemoveClientFromList_WhenClientDoesExist()
    {
        // Arrange
        var existingClient = new Client
        {
            Id = null,
            UniqueId = Guid.NewGuid(),
            Secret = Guid.NewGuid().ToString(),
            DisplayName = "displayName1",
            ExpirationTime = new DateTimeOffset(2024, 12,20,0,0,0,TimeSpan.Zero),
            NotBefore = new DateTimeOffset(2024, 12,1,0,0,0,TimeSpan.Zero),
        };
        _context.State.ClientId = existingClient.UniqueId.ToString()!;
        _context.State.ClientSecret = existingClient.Secret;
        ClientService.Clients.Add(existingClient);

        // Act
        await _clientService.GetByIdAndSecretOrDefaultAsync(_context, _cancellationToken);

        // Assert
        Assert.IsNotNull(_context.Result);
        JsonConvert.DeserializeObject<Client>((string)_context.Result!).Should()
            .BeEquivalentTo(existingClient, options => options
                .Using<DateTime>(ctx => ctx.Subject.ToUniversalTime().Should().Be(ctx.Expectation.ToUniversalTime()))
                .WhenTypeIs<DateTime>());
        
        _context.Roles.Should().ContainKey("Client");
        ((Client)_context.Roles["Client"]).Should().BeEquivalentTo(existingClient);
    }
}