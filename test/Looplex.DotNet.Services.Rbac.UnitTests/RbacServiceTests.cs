using System.Dynamic;
using System.Reflection;
using Casbin;
using FluentAssertions;
using Looplex.OpenForExtension.Abstractions.Contexts;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Looplex.DotNet.Services.Rbac.UnitTests;

[TestClass]
public class RbacServiceTests
{
    private RbacService _rbacService = null!;
    
    private IContext _context = null!;
    private ILogger<RbacService> _logger = null!;
    
    [TestInitialize]
    public void SetUp()
    {
        // Set up substitutes
        var testDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                            ?? throw new InvalidOperationException("Could not determine test directory");
        var modelPath = Path.Combine(testDirectory, "model.conf");
        var policyPath = Path.Combine(testDirectory, "policy.csv");

        if (!File.Exists(modelPath) || !File.Exists(policyPath))
        {
            throw new FileNotFoundException("Required Casbin configuration files are missing");
        }

        var enforcer = new Enforcer(modelPath, policyPath);
        _logger = Substitute.For<ILogger<RbacService>>();
        _rbacService = new RbacService(enforcer, _logger);


        // Set up the middleware context
        _context = Substitute.For<IContext>();

        dynamic state = new ExpandoObject();
        _context.State.Returns(state);
        _context.State.CancellationToken = CancellationToken.None;
    }
        
    [TestMethod]
    public void ThrowIfUnauthorized_UserIsNotInContext_ExceptionIsThrown()
    {
        // Arrange
        
        // Act
        var action = () => _rbacService.ThrowIfUnauthorized(_context, "resource", "read");
        
        // Assert
        action.Should().Throw<ArgumentNullException>().WithMessage("The path 'User.Email' does not exist or has a null value. (Parameter 'path')");
    }
        
    [TestMethod]
    public void ThrowIfUnauthorized_UserEmailIsEmptyInContext_ExceptionIsThrown()
    {
        // Arrange
        _context.State.Tenant = "tenant";
        _context.State.User = new ExpandoObject();
        _context.State.User.Email = "";
        
        // Act
        var action = () => _rbacService.ThrowIfUnauthorized(_context, "resource", "read");
        
        // Assert
        action.Should().Throw<ArgumentNullException>().WithMessage("USER_EMAIL_REQUIRED_FOR_AUTHORIZATION (Parameter 'email')");
    }
        
    [TestMethod]
    public void ThrowIfUnauthorized_TenantIsNotInContext_ExceptionIsThrown()
    {
        // Arrange
        
        // Act
        var action = () => _rbacService.ThrowIfUnauthorized(_context, "resource", "read");
        
        // Assert
        action.Should().Throw<ArgumentNullException>().WithMessage("The path 'User.Email' does not exist or has a null value. (Parameter 'path')");
    }
        
    [TestMethod]
    public void ThrowIfUnauthorized_TenantIsEmptyInContext_ExceptionIsThrown()
    {
        // Arrange
        _context.State.Tenant = "";
        _context.State.User = new ExpandoObject();
        _context.State.User.Email = "email@email.com";
        
        // Act
        var action = () => _rbacService.ThrowIfUnauthorized(_context, "resource", "read");
        
        // Assert
        action.Should().Throw<ArgumentNullException>().WithMessage("TENANT_REQUIRED_FOR_AUTHORIZATION (Parameter 'tenant')");
    }
        
    [TestMethod]
    [DataRow("read")]
    [DataRow("write")]
    [DataRow("delete")]
    public void ThrowIfUnauthorized_UserHasPermission_ExceptionIsNotThrown(string action)
    {
        // Arrange
        _context.State.Tenant = "looplex";
        _context.State.User = new ExpandoObject();
        _context.State.User.Email = "bob.rivest@email.com";
        
        // Act & Assert
        _rbacService.ThrowIfUnauthorized(_context, "resource", action);
    }
    
    [TestMethod]
    [DataRow("execute")]
    public void ThrowIfUnauthorized_UserDoesNotHasPermission_ExceptionIsThrown(string action)
    {
        // Arrange
        _context.State.Tenant = "looplex";
        _context.State.User = new ExpandoObject();
        _context.State.User.Email = "bob.rivest@email.com";
        
        // Act
        var act = () => _rbacService.ThrowIfUnauthorized(_context, "resource", action);
        
        // Assert
        act.Should().Throw<UnauthorizedAccessException>().WithMessage("UNAUTHORIZED_ACCESS");
    }
}