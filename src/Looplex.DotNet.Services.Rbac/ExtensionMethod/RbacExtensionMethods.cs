using Casbin;
using Looplex.DotNet.Core.Application.Abstractions.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Looplex.DotNet.Services.Rbac.ExtensionMethod;

public static class RbacExtensionMethods
{
    public static void AddRbacServices(this IServiceCollection services, IEnforcer enforcer)
    {
        services.AddSingleton<IRbacService, RbacService>();
        services.AddSingleton(enforcer);
    }
}