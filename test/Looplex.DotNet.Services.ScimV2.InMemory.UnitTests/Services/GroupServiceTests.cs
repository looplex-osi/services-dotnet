using System.Dynamic;
using FluentAssertions;
using Looplex.DotNet.Core.Common.Exceptions;
using Looplex.DotNet.Middlewares.ScimV2.Application.Abstractions.Services;
using Looplex.DotNet.Middlewares.ScimV2.Domain.Entities;
using Looplex.DotNet.Middlewares.ScimV2.Domain.Entities.Groups;
using Looplex.DotNet.Middlewares.ScimV2.Domain.Entities.Messages;
using Looplex.DotNet.Services.ScimV2.InMemory.Services;
using Looplex.OpenForExtension.Abstractions.Contexts;
using Newtonsoft.Json;
using NSubstitute;

namespace Looplex.DotNet.Services.ScimV2.InMemory.UnitTests.Services;

[TestClass]
public class GroupServiceTests
{
    private IGroupService _groupService = null!;
    private IContext _context = null!;
    private CancellationToken _cancellationToken;

    [TestInitialize]
    public void Setup()
    {
        GroupService.Groups = [];
        _groupService = new GroupService();
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
        _context.State.Pagination.StartIndex = 1;
        _context.State.Pagination.ItemsPerPage = 10;
        var existingGroup = new Group
        {
            Id = null,
            UniqueId = Guid.NewGuid(),
            DisplayName = "displayName1"
        };
        GroupService.Groups.Add(existingGroup);
            
        // Act
        await _groupService.GetAllAsync(_context, _cancellationToken);

        // Assert
        var result = JsonConvert.DeserializeObject<ListResponse>((string)_context.Result!)!;
        Assert.AreEqual(1, result.TotalResults);
        JsonConvert.DeserializeObject<Group>(result.Resources[0].ToString()!).Should().BeEquivalentTo(existingGroup);
    }

    [TestMethod]
    public async Task GetByIdAsync_ShouldThrowEntityNotFoundException_WhenGroupDoesNotExist()
    {
        // Arrange
        _context.State.Id = Guid.NewGuid().ToString();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<EntityNotFoundException>(() => _groupService.GetByIdAsync(_context, _cancellationToken));
    }

    [TestMethod]
    public async Task GetByIdAsync_ShouldReturnGroup_WhenGroupDoesExist()
    {
        // Arrange
        var existingGroup = new Group
        {
            Id = null,
            UniqueId = Guid.NewGuid(),
            DisplayName = "displayName1"
        };
        _context.State.Id = existingGroup.UniqueId.ToString()!;
        GroupService.Groups.Add(existingGroup);

        // Act
        await _groupService.GetByIdAsync(_context, _cancellationToken);
            
        // Assert
        JsonConvert.DeserializeObject<Group>(_context.Result!.ToString()!).Should().BeEquivalentTo(existingGroup);
    }
    
    [TestMethod]
    public async Task CreateAsync_ShouldAddGroupToList()
    {
        // Arrange
        var groupJson = $"{{ \"displayName\": \"Test Group\" }}";
        _context.State.Resource = groupJson;
        Schemas.Add(typeof(Group), "{}");
        
        // Act
        await _groupService.CreateAsync(_context, _cancellationToken);

        // Assert
        Assert.IsNotNull(_context.Result);
        var id = Guid.Parse((string)_context.Result);
        GroupService.Groups.Should().Contain(u => u.UniqueId == id);
    }

    [TestMethod]
    public async Task PatchAsync_ShouldApplyOperationsToGroup()
    {
        // Arrange
        var existingGroup = new Group
        {
            Id = 1,
            UniqueId = Guid.NewGuid(),
            DisplayName = "displayName1"
        };
        _context.State.Id = existingGroup.Id;
        GroupService.Groups.Add(existingGroup);
        _context.State.Operations = "[ { \"op\": \"add\", \"path\": \"DisplayName\", \"value\": \"Updated Group\" } ]";
        _context.State.Id = existingGroup.UniqueId.ToString()!;

        // Act
        await _groupService.PatchAsync(_context, _cancellationToken);

        // Assert
        var group = GroupService.Groups.First(u => u.Id == existingGroup.Id);
        group.DisplayName.Should().Be("Updated Group");
        ((Group)_context.Roles["Group"]).ChangedPropertyNotification.ChangedProperties.Should().BeEquivalentTo(["DisplayName"]);
    }
    
    [TestMethod]
    public async Task DeleteAsync_ShouldThrowEntityNotFoundException_WhenGroupDoesNotExist()
    {
        // Arrange
        _context.State.Id = Guid.NewGuid().ToString();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<EntityNotFoundException>(() => _groupService.DeleteAsync(_context, _cancellationToken));
    }
    
    [TestMethod]
    public async Task DeleteAsync_ShouldRemoveGroupFromList_WhenGroupDoesExist()
    {
        // Arrange
        var existingGroup = new Group
        {
            Id = 1,
            UniqueId = Guid.NewGuid(),
            DisplayName = "displayName1"
        };
        _context.State.Id = existingGroup.UniqueId.ToString()!;
        GroupService.Groups.Add(existingGroup);

        // Act
        await _groupService.DeleteAsync(_context, _cancellationToken);

        // Assert
        GroupService.Groups.Should().NotContain(u => u.UniqueId == existingGroup.UniqueId);
    }
}