using System.Dynamic;
using System.Text;
using FluentAssertions;
using Looplex.DotNet.Core.Common.Exceptions;
using Looplex.DotNet.Middlewares.ApiKeys.Application.Abstractions.Services;
using Looplex.DotNet.Middlewares.ApiKeys.Domain.Entities.ClientCredentials;
using Looplex.DotNet.Middlewares.ScimV2.Domain;
using Looplex.DotNet.Middlewares.ScimV2.Domain.Entities;
using Looplex.DotNet.Middlewares.ScimV2.Domain.Entities.Messages;
using Looplex.DotNet.Services.ApiKeys.InMemory.Dtos;
using Looplex.DotNet.Services.ApiKeys.InMemory.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using NSubstitute;

namespace Looplex.DotNet.Services.ApiKeys.InMemory.UnitTests.Services;

[TestClass]
public class ApiKeyServiceTests
{
    private IConfiguration _configuration = null!;
    private IApiKeyService _apiKeyService = null!;
    private IScimV2Context _context = null!;
    private CancellationToken _cancellationToken;
    private HttpContext _httpContext = null!;
    private Stream _memoryStream = null!;
    
    [TestInitialize]
    public void Setup()
    {
        ApiKeyService.ClientCredentials = [];
        _httpContext = new DefaultHttpContext();
        _memoryStream = new MemoryStream();
        _httpContext.Response.Body = _memoryStream;
        _configuration = Substitute.For<IConfiguration>();
        _apiKeyService = new ApiKeyService(_configuration);
        _context = Substitute.For<IScimV2Context>();
        dynamic state = new ExpandoObject();
        _context.State.Returns(state);
        state.HttpContext = _httpContext;
        var roles = new Dictionary<string, dynamic>();
        _context.Roles.Returns(roles);
        _cancellationToken = new CancellationToken();
        
        if (!Schemas.ContainsKey(typeof(ClientCredential)))
            Schemas.Add(typeof(ClientCredential), "{}");
    }

    [TestMethod]
    public async Task GetAllAsync_ShouldReturnPaginatedCollection()
    {
        // Arrange
        _context.State.Pagination = new ExpandoObject();
        _context.State.Pagination.StartIndex = 1;
        _context.State.Pagination.ItemsPerPage = 10;
        var existingClientCredential = new ClientCredential
        {
            Id = null,
            UniqueId = Guid.NewGuid(),
            ClientName = "clientName1",
            ExpirationTime = new DateTimeOffset(2024, 12,20,0,0,0,TimeSpan.Zero),
            NotBefore = new DateTimeOffset(2024, 12,1,0,0,0,TimeSpan.Zero),
        };
        ApiKeyService.ClientCredentials.Add(existingClientCredential);
            
        // Act
        await _apiKeyService.GetAllAsync(_context, _cancellationToken);

        // Assert
        var result = JsonConvert.DeserializeObject<ListResponse>((string)_context.Result!)!;
        Assert.AreEqual(1, result.TotalResults);
        JsonConvert.DeserializeObject<ClientCredential>(result.Resources[0].ToString()!).Should()
            .BeEquivalentTo(existingClientCredential, options => options
            .Using<DateTime>(ctx => ctx.Subject.ToUniversalTime().Should().Be(ctx.Expectation.ToUniversalTime()))
            .WhenTypeIs<DateTime>());
    }

    [TestMethod]
    public async Task GetByIdAsync_ShouldThrowEntityNotFoundException_WhenClientCredentialDoesNotExist()
    {
        // Arrange
        _context.RouteValues = new Dictionary<string, object?>
        {
            { "ClientCredentialId", Guid.NewGuid().ToString() }
        };
        
        // Act & Assert
        await Assert.ThrowsExceptionAsync<EntityNotFoundException>(() => _apiKeyService.GetByIdAsync(_context, _cancellationToken));
    }

    [TestMethod]
    public async Task GetByIdAsync_ShouldReturnClientCredential_WhenClientCredentialDoesExist()
    {
        // Arrange
        var existingClientCredential = new ClientCredential
        {
            Id = null,
            UniqueId = Guid.NewGuid(),
            ClientName = "clientName1"
        };
        _context.RouteValues = new Dictionary<string, object?>
        {
            { "ClientCredentialId", existingClientCredential.UniqueId.ToString() }
        };
        ApiKeyService.ClientCredentials.Add(existingClientCredential);

        // Act
        await _apiKeyService.GetByIdAsync(_context, _cancellationToken);
            
        // Assert
        JsonConvert.DeserializeObject<ClientCredential>(_context.Result!.ToString()!).Should().BeEquivalentTo(existingClientCredential);
    }
    
    [TestMethod]
    public async Task CreateAsync_ShouldAddClientCredentialToList()
    {
        // Arrange
        var clientCredentialJson = $"{{  }}";
        _context.State.Resource = clientCredentialJson;
        _configuration["ClientSecretByteLength"].Returns("72"); 
        _configuration["ClientSecretDigestCost"].Returns("4"); 
        
        // Act
        await _apiKeyService.CreateAsync(_context, _cancellationToken);

        // Assert
        var id = Guid.Parse((string)_context.Result!);
        ApiKeyService.ClientCredentials.Should().Contain(u => u.UniqueId == id);
        
        _memoryStream.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(_memoryStream, Encoding.UTF8).ReadToEndAsync(CancellationToken.None);
        var clientCredential = JsonConvert.DeserializeObject<ClientCredentialDto>(responseBody)!;
        clientCredential.ClientSecret.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    public async Task PatchAsync_ShouldApplyOperationsToClientCredential()
    {
        // Arrange
        var expirationTime = DateTime.UtcNow.AddDays(1);
        var existingClientCredential = new ClientCredential
        {
            Id = 1,
            UniqueId = Guid.NewGuid(),
            ClientName = "clientName1"
        };
        ApiKeyService.ClientCredentials.Add(existingClientCredential);
        _context.State.Operations = $"[ {{ \"op\": \"add\", \"path\": \"ExpirationTime\", \"value\": \"{expirationTime:yyyy-MM-ddTHH:mm:ss.ffffffZ}\" }} ]";
        _context.RouteValues = new Dictionary<string, object?>
        {
            { "ClientCredentialId", existingClientCredential.UniqueId.ToString() }
        };
        
        // Act
        await _apiKeyService.PatchAsync(_context, _cancellationToken);

        // Assert
        var clientCredential = ApiKeyService.ClientCredentials.First(u => u.UniqueId == existingClientCredential.UniqueId);
        clientCredential.ExpirationTime.Should().Be(expirationTime);
        ((ClientCredential)_context.Roles["ClientCredential"]).ChangedPropertyNotification.ChangedProperties.Should().BeEquivalentTo(["ExpirationTime"]);
    }
    
    [TestMethod]
    [DataRow("Digest", "value")]
    [DataRow("ClientId", "039d6c96-ae9e-45be-a99c-5a05533a3ff7")]
    public async Task PatchAsync_TryUpdateReadonlyPropertyes_ShouldThrowInvalidOperationException(string property, string value)
    {
        // Arrange
        var existingClientCredential = new ClientCredential
        {
            Id = 1,
            UniqueId = Guid.NewGuid(),
            ClientName = "clientName1"
        };
        ApiKeyService.ClientCredentials.Add(existingClientCredential);
        _context.State.Operations = $"[ {{ \"op\": \"add\", \"path\": \"{property}\", \"value\": \"{value}\" }} ]";
        _context.RouteValues = new Dictionary<string, object?>
        {
            { "ClientCredentialId", existingClientCredential.UniqueId.ToString() }
        };
        
        // Act & Assert
        var ex = await Assert
            .ThrowsExceptionAsync<InvalidOperationException>(() => _apiKeyService.PatchAsync(_context, _cancellationToken));
        ex.Message.Should().Be($"Cannot update {property}");
    }
    
    [TestMethod]
    public async Task DeleteAsync_ShouldThrowEntityNotFoundException_WhenClientCredentialDoesNotExist()
    {
        // Arrange
        _context.RouteValues = new Dictionary<string, object?>
        {
            { "ClientCredentialId", Guid.NewGuid().ToString() }
        };
        
        // Act & Assert
        await Assert.ThrowsExceptionAsync<EntityNotFoundException>(() => _apiKeyService.DeleteAsync(_context, _cancellationToken));
    }
    
    [TestMethod]
    public async Task DeleteAsync_ShouldRemoveClientCredentialFromList_WhenClientCredentialDoesExist()
    {
        // Arrange
        var existingClientCredential = new ClientCredential
        {
            Id = 1,
            UniqueId = Guid.NewGuid(),
            ClientName = "clientName1"
        };
        _context.RouteValues = new Dictionary<string, object?>
        {
            { "ClientCredentialId", existingClientCredential.UniqueId.ToString() }
        };
        ApiKeyService.ClientCredentials.Add(existingClientCredential);

        // Act
        await _apiKeyService.DeleteAsync(_context, _cancellationToken);

        // Assert
        ApiKeyService.ClientCredentials.Should().NotContain(u => u.Id == existingClientCredential.Id);
    }
    
    [TestMethod]
    public async Task GetByIdAndSecretOrDefaultAsync_ShouldReturnNull_WhenClientCredentialDoesNotExist()
    {
        // Arrange
        _context.State.ClientId = Guid.NewGuid().ToString();
        _context.State.ClientSecret = Convert.ToBase64String([1,0,1]);

        // Act
        await _apiKeyService.GetByIdAndSecretOrDefaultAsync(_context, _cancellationToken);
            
        // Assert
        _context.Roles.Should().NotContainKey("ClientCredential");
        Assert.IsNull(_context.Result);
    }
    
    [TestMethod]
    public async Task GetByIdAndSecretOrDefaultAsync_ResultAndRolesShouldContainClientCredential_WhenClientCredentialDoesExist()
    {
        // Arrange
        var notBefore = DateTime.UtcNow;
        var expirationTime = DateTime.UtcNow.AddDays(1);
        var clientCredentialJson = $"{{ \"notBefore\": \"{notBefore:yyyy-MM-ddTHH:mm:ss.ffffffZ}\", " +
                         $"\"expirationTime\": \"{expirationTime:yyyy-MM-ddTHH:mm:ss.ffffffZ}\"}}";
        _context.State.Resource = clientCredentialJson;
        _configuration["ClientSecretByteLength"].Returns("72"); 
        _configuration["ClientSecretDigestCost"].Returns("4"); 
        await _apiKeyService.CreateAsync(_context, _cancellationToken);
        _memoryStream.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(_memoryStream, Encoding.UTF8).ReadToEndAsync(CancellationToken.None);
        var clientCredentialDto = JsonConvert.DeserializeObject<ClientCredentialDto>(responseBody)!;
        _context.Roles.Remove("ClientCredential"); // Because we are using the same mock context
        _context.State.ClientId = clientCredentialDto.ClientId.ToString()!;
        _context.State.ClientSecret = clientCredentialDto.ClientSecret;

        // Act
        await _apiKeyService.GetByIdAndSecretOrDefaultAsync(_context, _cancellationToken);

        // Assert
        Assert.IsNotNull(_context.Result);
        var clientCredential = JsonConvert.DeserializeObject<ClientCredential>((string)_context.Result!)!;
        clientCredential.ClientId.Should().Be(clientCredentialDto.ClientId);
        clientCredential.NotBefore.ToUniversalTime().Should().Be(notBefore);
        clientCredential.ExpirationTime.ToUniversalTime().Should().Be(expirationTime);

        _context.Roles.Should().ContainKey("ClientCredential");
        ((ClientCredential)_context.Roles["ClientCredential"]).Should()
            .BeEquivalentTo(clientCredential, option => option.Excluding(ak => ak.Id).Excluding(ak => ak.Digest));
    }
}