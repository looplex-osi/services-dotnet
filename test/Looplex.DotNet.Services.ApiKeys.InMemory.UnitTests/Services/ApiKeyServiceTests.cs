using System.Dynamic;
using System.Text;
using FluentAssertions;
using Looplex.DotNet.Core.Common.Exceptions;
using Looplex.DotNet.Core.Domain;
using Looplex.DotNet.Middlewares.ApiKeys.Application.Abstractions.Services;
using Looplex.DotNet.Middlewares.ApiKeys.Domain.Entities.ApiKeys;
using Looplex.DotNet.Middlewares.ScimV2.Domain.Entities.Schemas;
using Looplex.DotNet.Services.ApiKeys.InMemory.Dtos;
using Looplex.DotNet.Services.ApiKeys.InMemory.Services;
using Looplex.OpenForExtension.Abstractions.Contexts;
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
    private IContext _context = null!;
    private CancellationToken _cancellationToken;
    private HttpContext _httpContext = null!;
    private Stream _memoryStream = null!;
    
    [TestInitialize]
    public void Setup()
    {
        ApiKeyService.ApiKeys = [];
        _httpContext = new DefaultHttpContext();
        _memoryStream = new MemoryStream();
        _httpContext.Response.Body = _memoryStream;
        _configuration = Substitute.For<IConfiguration>();
        _apiKeyService = new ApiKeyService(_configuration);
        _context = Substitute.For<IContext>();
        dynamic state = new ExpandoObject();
        _context.State.Returns(state);
        state.HttpContext = _httpContext;
        var roles = new Dictionary<string, dynamic>();
        _context.Roles.Returns(roles);
        _cancellationToken = new CancellationToken();
        
        if (!Schema.Schemas.ContainsKey(typeof(ApiKey)))
            Schema.Add<ApiKey>("{}");
    }

    [TestMethod]
    public async Task GetAllAsync_ShouldReturnPaginatedCollection()
    {
        // Arrange
        _context.State.Pagination = new ExpandoObject();
        _context.State.Pagination.Page = 1;
        _context.State.Pagination.PerPage = 10;
        var existingApiKey = new ApiKey
        {
            Id = null,
            UniqueId = Guid.NewGuid(),
            ClientName = "clientName1",
            ExpirationTime = new DateTimeOffset(2024, 12,20,0,0,0,TimeSpan.Zero),
            NotBefore = new DateTimeOffset(2024, 12,1,0,0,0,TimeSpan.Zero),
        };
        ApiKeyService.ApiKeys.Add(existingApiKey);
            
        // Act
        await _apiKeyService.GetAllAsync(_context, _cancellationToken);

        // Assert
        var result = JsonConvert.DeserializeObject<PaginatedCollection>((string)_context.Result!)!;
        Assert.AreEqual(1, result.TotalCount);
        JsonConvert.DeserializeObject<ApiKey>(result.Records[0].ToString()!).Should()
            .BeEquivalentTo(existingApiKey, options => options
            .Using<DateTime>(ctx => ctx.Subject.ToUniversalTime().Should().Be(ctx.Expectation.ToUniversalTime()))
            .WhenTypeIs<DateTime>());
    }

    [TestMethod]
    public async Task GetByIdAsync_ShouldThrowEntityNotFoundException_WhenApiKeyDoesNotExist()
    {
        // Arrange
        _context.State.Id = Guid.NewGuid().ToString();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<EntityNotFoundException>(() => _apiKeyService.GetByIdAsync(_context, _cancellationToken));
    }

    [TestMethod]
    public async Task GetByIdAsync_ShouldReturnApiKey_WhenApiKeyDoesExist()
    {
        // Arrange
        var existingApiKey = new ApiKey
        {
            Id = null,
            UniqueId = Guid.NewGuid(),
            ClientName = "clientName1"
        };
        _context.State.Id = existingApiKey.UniqueId.ToString()!;
        ApiKeyService.ApiKeys.Add(existingApiKey);

        // Act
        await _apiKeyService.GetByIdAsync(_context, _cancellationToken);
            
        // Assert
        JsonConvert.DeserializeObject<ApiKey>(_context.Result!.ToString()!).Should().BeEquivalentTo(existingApiKey);
    }
    
    [TestMethod]
    public async Task CreateAsync_ShouldAddApiKeyToList()
    {
        // Arrange
        var apiKeyJson = $"{{  }}";
        _context.State.Resource = apiKeyJson;
        _configuration["ClientSecretByteLength"].Returns("72"); 
        _configuration["ClientSecretDigestCost"].Returns("4"); 
        
        // Act
        await _apiKeyService.CreateAsync(_context, _cancellationToken);

        // Assert
        var id = Guid.Parse((string)_context.Result!);
        ApiKeyService.ApiKeys.Should().Contain(u => u.UniqueId == id);
        
        _memoryStream.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(_memoryStream, Encoding.UTF8).ReadToEndAsync(CancellationToken.None);
        var apiKey = JsonConvert.DeserializeObject<ApiKeyDto>(responseBody)!;
        apiKey.ClientId.Should().Be(id);
        apiKey.ClientSecret.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    public async Task PatchAsync_ShouldApplyOperationsToApiKey()
    {
        // Arrange
        var expirationTime = DateTime.UtcNow.AddDays(1);
        var existingApiKey = new ApiKey
        {
            Id = 1,
            UniqueId = Guid.NewGuid(),
            ClientName = "clientName1"
        };
        ApiKeyService.ApiKeys.Add(existingApiKey);
        _context.State.Operations = $"[ {{ \"op\": \"add\", \"path\": \"ExpirationTime\", \"value\": \"{expirationTime:yyyy-MM-ddTHH:mm:ss.ffffffZ}\" }} ]";
        _context.State.Id = existingApiKey.UniqueId.ToString()!;

        // Act
        await _apiKeyService.PatchAsync(_context, _cancellationToken);

        // Assert
        var apiKey = ApiKeyService.ApiKeys.First(u => u.UniqueId == existingApiKey.UniqueId);
        apiKey.ExpirationTime.Should().Be(expirationTime);
        ((ApiKey)_context.Roles["ApiKey"]).ChangedPropertyNotification.ChangedProperties.Should().BeEquivalentTo(["ExpirationTime"]);
    }
    
    [TestMethod]
    [DataRow("Digest", "value")]
    [DataRow("ClientId", "039d6c96-ae9e-45be-a99c-5a05533a3ff7")]
    public async Task PatchAsync_TryUpdateReadonlyPropertyes_ShouldThrowInvalidOperationException(string property, string value)
    {
        // Arrange
        var existingApiKey = new ApiKey
        {
            Id = 1,
            UniqueId = Guid.NewGuid(),
            ClientName = "clientName1"
        };
        ApiKeyService.ApiKeys.Add(existingApiKey);
        _context.State.Operations = $"[ {{ \"op\": \"add\", \"path\": \"{property}\", \"value\": \"{value}\" }} ]";
        _context.State.Id = existingApiKey.UniqueId.ToString()!;

        // Act & Assert
        var ex = await Assert
            .ThrowsExceptionAsync<InvalidOperationException>(() => _apiKeyService.PatchAsync(_context, _cancellationToken));
        ex.Message.Should().Be($"Cannot update {property}");
    }
    
    [TestMethod]
    public async Task DeleteAsync_ShouldThrowEntityNotFoundException_WhenApiKeyDoesNotExist()
    {
        // Arrange
        _context.State.Id = Guid.NewGuid().ToString();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<EntityNotFoundException>(() => _apiKeyService.DeleteAsync(_context, _cancellationToken));
    }
    
    [TestMethod]
    public async Task DeleteAsync_ShouldRemoveApiKeyFromList_WhenApiKeyDoesExist()
    {
        // Arrange
        var existingApiKey = new ApiKey
        {
            Id = 1,
            UniqueId = Guid.NewGuid(),
            ClientName = "clientName1"
        };
        _context.State.Id = existingApiKey.UniqueId.ToString()!;
        ApiKeyService.ApiKeys.Add(existingApiKey);

        // Act
        await _apiKeyService.DeleteAsync(_context, _cancellationToken);

        // Assert
        ApiKeyService.ApiKeys.Should().NotContain(u => u.Id == existingApiKey.Id);
    }
    
    [TestMethod]
    public async Task GetByIdAndSecretOrDefaultAsync_ShouldReturnNull_WhenApiKeyDoesNotExist()
    {
        // Arrange
        _context.State.ClientId = Guid.NewGuid().ToString();
        _context.State.ClientSecret = Guid.NewGuid().ToString();

        // Act
        await _apiKeyService.GetByIdAndSecretOrDefaultAsync(_context, _cancellationToken);
            
        // Assert
        _context.Roles.Should().NotContainKey("ApiKey");
        Assert.IsNull(_context.Result);
    }
    
    [TestMethod]
    public async Task GetByIdAndSecretOrDefaultAsync_ResultAndRolesShouldContainApiKey_WhenApiKeyDoesExist()
    {
        // Arrange
        var notBefore = DateTime.UtcNow;
        var expirationTime = DateTime.UtcNow.AddDays(1);
        var apiKeyJson = $"{{ \"notBefore\": \"{notBefore:yyyy-MM-ddTHH:mm:ss.ffffffZ}\", " +
                         $"\"expirationTime\": \"{expirationTime:yyyy-MM-ddTHH:mm:ss.ffffffZ}\"}}";
        _context.State.Resource = apiKeyJson;
        _configuration["ClientSecretByteLength"].Returns("72"); 
        _configuration["ClientSecretDigestCost"].Returns("4"); 
        await _apiKeyService.CreateAsync(_context, _cancellationToken);
        _memoryStream.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(_memoryStream, Encoding.UTF8).ReadToEndAsync(CancellationToken.None);
        var apiKeyDto = JsonConvert.DeserializeObject<ApiKeyDto>(responseBody)!;

        _context.State.ClientId = apiKeyDto.ClientId.ToString()!;
        _context.State.ClientSecret = apiKeyDto.ClientSecret;

        // Act
        await _apiKeyService.GetByIdAndSecretOrDefaultAsync(_context, _cancellationToken);

        // Assert
        Assert.IsNotNull(_context.Result);
        var apiKey = JsonConvert.DeserializeObject<ApiKey>((string)_context.Result!)!;
        apiKey.ClientId.Should().Be(apiKeyDto.ClientId);
        apiKey.NotBefore.ToUniversalTime().Should().Be(notBefore);
        apiKey.ExpirationTime.ToUniversalTime().Should().Be(expirationTime);

        _context.Roles.Should().ContainKey("ApiKey");
        ((ApiKey)_context.Roles["ApiKey"]).Should().BeEquivalentTo(apiKey);
    }
}