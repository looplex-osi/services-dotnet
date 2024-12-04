using System.Dynamic;
using FluentAssertions;
using Looplex.DotNet.Core.Common.Exceptions;
using Looplex.DotNet.Middlewares.ScimV2.Application.Abstractions.Providers;
using Looplex.DotNet.Middlewares.ScimV2.Application.Abstractions.Services;
using Looplex.DotNet.Middlewares.ScimV2.Domain;
using Looplex.DotNet.Middlewares.ScimV2.Domain.Entities.Messages;
using Looplex.DotNet.Middlewares.ScimV2.Domain.Entities.Users;
using Looplex.DotNet.Services.ScimV2.InMemory.Services;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using NSubstitute;

namespace Looplex.DotNet.Services.ScimV2.InMemory.UnitTests.Services;

[TestClass]
public class UserServiceTests
{
    private IUserService _userService = null!;
    private IConfiguration _configuration = null!;
    private IJsonSchemaProvider _jsonSchemaProvider = null!;
    private IScimV2Context _context = null!;
    private CancellationToken _cancellationToken;

    [TestInitialize]
    public void Setup()
    {
        _configuration = Substitute.For<IConfiguration>();
        _jsonSchemaProvider = Substitute.For<IJsonSchemaProvider>();
        _userService = new UserService(_configuration, _jsonSchemaProvider);
        UserService.Users = [];
        _context = Substitute.For<IScimV2Context>();
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
        _context.State.Pagination.StartIndex = 1;
        _context.State.Pagination.ItemsPerPage = 10;
        var existingUser = new User
        {
            Id = null,
            UniqueId = Guid.NewGuid(),
            UserName = "userName1"
        };
        UserService.Users.Add(existingUser);
            
        // Act
        await _userService.GetAllAsync(_context, _cancellationToken);

        // Assert
        var result = JsonConvert.DeserializeObject<ListResponse>((string)_context.Result!)!;
        Assert.AreEqual(1, result.TotalResults);
        JsonConvert.DeserializeObject<User>(result.Resources[0].ToString()!).Should().BeEquivalentTo(existingUser);
    }

    [TestMethod]
    public async Task GetByIdAsync_ShouldThrowEntityNotFoundException_WhenUserDoesNotExist()
    {
        // Arrange
        _context.RouteValues = new Dictionary<string, object?>
        {
            { "UserId", Guid.NewGuid().ToString() }
        };
        
        // Act & Assert
        await Assert.ThrowsExceptionAsync<EntityNotFoundException>(() => _userService.GetByIdAsync(_context, _cancellationToken));
    }

    [TestMethod]
    public async Task GetByIdAsync_ShouldReturnUser_WhenUserDoesExist()
    {
        // Arrange
        var existingUser = new User
        {
            Id = null,
            UniqueId = Guid.NewGuid(),
            UserName = "userName1"
        };
        _context.RouteValues = new Dictionary<string, object?>
        {
            { "UserId", existingUser.UniqueId.ToString() }
        };
        UserService.Users.Add(existingUser);

        // Act
        await _userService.GetByIdAsync(_context, _cancellationToken);
            
        // Assert
        JsonConvert.DeserializeObject<User>(_context.Result!.ToString()!).Should().BeEquivalentTo(existingUser);
    }
    
    [TestMethod]
    public async Task CreateAsync_ShouldAddUserToList()
    {
        // Arrange
        var userJson = $"{{ \"userName\": \"Test User\" }}";
        _context.State.Resource = userJson;
        _configuration["JsonSchemaIdForUser"].Returns("userSchemaId"); 

        _jsonSchemaProvider
            .ResolveJsonSchemaAsync(Arg.Any<IScimV2Context>(), "userSchemaId")
            .Returns("{}");
        
        // Act
        await _userService.CreateAsync(_context, _cancellationToken);

        // Assert
        Assert.IsNotNull(_context.Result);
        var id = Guid.Parse((string)_context.Result);
        UserService.Users.Should().Contain(u => u.UniqueId == id);
    }
    
    [TestMethod]
    public async Task PatchAsync_ShouldThrowException_OperationFailed()
    {
        // Arrange
        var existingUser = new User
        {
            Id = 1,
            UniqueId = Guid.NewGuid(),
            UserName = "userName1"
        };
        UserService.Users.Add(existingUser);
        _context.State.Operations = "[ { \"op\": \"add\", \"path\": \"InvalidPath\", \"value\": \"Updated User\" } ]";
        _context.RouteValues = new Dictionary<string, object?>
        {
            { "UserId", existingUser.UniqueId.ToString() }
        };
        
        // Act
        var action = () => _userService.PatchAsync(_context, _cancellationToken);

        // Assert
        var ex = await Assert.ThrowsExceptionAsync<ArgumentException>(() => action());
        Assert.AreEqual("InvalidPath", ex.ParamName);
    }
    
    [TestMethod]
    public async Task PatchAsync_ShouldApplyOperationsToUser()
    {
        // Arrange
        var existingUser = new User
        {
            Id = 1,
            UniqueId = Guid.NewGuid(),
            UserName = "userName1"
        };
        UserService.Users.Add(existingUser);
        _context.State.Operations = "[ { \"op\": \"add\", \"path\": \"UserName\", \"value\": \"Updated User\" } ]";
        _context.RouteValues = new Dictionary<string, object?>
        {
            { "UserId", existingUser.UniqueId.ToString() }
        };
        
        // Act
        await _userService.PatchAsync(_context, _cancellationToken);

        // Assert
        var user = UserService.Users.First(u => u.Id == existingUser.Id);
        user.UserName.Should().Be("Updated User");
        ((User)_context.Roles["User"]).ChangedPropertyNotification.ChangedProperties.Should().BeEquivalentTo(["UserName"]);
    }
    
    [TestMethod]
    public async Task DeleteAsync_ShouldThrowEntityNotFoundException_WhenUserDoesNotExist()
    {
        // Arrange
        _context.RouteValues = new Dictionary<string, object?>
        {
            { "UserId", Guid.NewGuid().ToString() }
        };
        
        // Act & Assert
        await Assert.ThrowsExceptionAsync<EntityNotFoundException>(() => _userService.DeleteAsync(_context, _cancellationToken));
    }
    
    [TestMethod]
    public async Task DeleteAsync_ShouldRemoveUserFromList_WhenUserDoesExist()
    {
        // Arrange
        var existingUser = new User
        {
            Id = 1,
            UniqueId = Guid.NewGuid(),
            UserName = "userName1"
        };
        _context.RouteValues = new Dictionary<string, object?>
        {
            { "UserId", existingUser.UniqueId.ToString() }
        };
        UserService.Users.Add(existingUser);

        // Act
        await _userService.DeleteAsync(_context, _cancellationToken);

        // Assert
        UserService.Users.Should().NotContain(u => u.Id == existingUser.Id);
    }
}