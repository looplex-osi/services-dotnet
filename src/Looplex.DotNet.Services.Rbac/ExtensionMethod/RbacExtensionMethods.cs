using Casbin;
using Microsoft.Extensions.DependencyInjection;

namespace Looplex.DotNet.Services.Rbac.ExtensionMethod;

public static class RbacExtensionMethods
{
    public static void AddRbacServices(this IServiceCollection services, IEnforcer enforcer)
    {
        services.AddSingleton(enforcer);
    }
}