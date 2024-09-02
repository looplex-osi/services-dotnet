using System.Dynamic;
using FluentAssertions;
using Looplex.DotNet.Core.Common.Exceptions;
using Looplex.DotNet.Core.Domain;
using Looplex.DotNet.Middlewares.ScimV2.Application.Abstractions.Services;
using Looplex.DotNet.Middlewares.ScimV2.Domain.Entities.Schemas;
using Looplex.DotNet.Middlewares.ScimV2.Domain.Entities.Users;
using Looplex.DotNet.Services.ScimV2.InMemory.Services;
using Looplex.OpenForExtension.Abstractions.Contexts;
using Newtonsoft.Json;
using NSubstitute;

namespace Looplex.DotNet.Services.ScimV2.InMemory.UnitTests.Services;

[TestClass]
public class UserServiceTests
{
    private IUserService _userService = null!;
    private IContext _context = null!;
    private CancellationToken _cancellationToken;

    [TestInitialize]
    public void Setup()
    {
        _userService = new UserService();
        UserService.Users = [];
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
        var result = JsonConvert.DeserializeObject<PaginatedCollection>((string)_context.Result!)!;
        Assert.AreEqual(1, result.TotalCount);
        JsonConvert.DeserializeObject<User>(result.Records[0].ToString()!).Should().BeEquivalentTo(existingUser);
    }

    [TestMethod]
    public async Task GetByIdAsync_ShouldThrowEntityNotFoundException_WhenUserDoesNotExist()
    {
        // Arrange
        _context.State.Id = Guid.NewGuid().ToString();

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
        _context.State.Id = existingUser.UniqueId.ToString()!;
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
        var id = Guid.NewGuid();
        var userJson = $"{{ \"id\": \"{id}\", \"userName\": \"Test User\" }}";
        _context.State.Resource = userJson;
        Schema.Add<User>("{}");
        
        // Act
        await _userService.CreateAsync(_context, _cancellationToken);

        // Assert
        Assert.AreEqual(id, _context.Result);
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
        _context.State.Id = existingUser.UniqueId.ToString()!;

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
        _context.State.Id = existingUser.UniqueId.ToString()!;

        // Act
        await _userService.PatchAsync(_context, _cancellationToken);

        // Assert
        var user = UserService.Users.First(u => u.Id == existingUser.Id);
        user.UserName.Should().Be("Updated User");
    }
    
    [TestMethod]
    public async Task DeleteAsync_ShouldThrowEntityNotFoundException_WhenUserDoesNotExist()
    {
        // Arrange
        _context.State.Id = Guid.NewGuid().ToString();

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
        _context.State.Id = existingUser.UniqueId.ToString()!;
        UserService.Users.Add(existingUser);

        // Act
        await _userService.DeleteAsync(_context, _cancellationToken);

        // Assert
        UserService.Users.Should().NotContain(u => u.Id == existingUser.Id);
    }
}