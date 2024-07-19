using Looplex.DotNet.Middlewares.ScimV2.Application.Abstractions.Services;
using Looplex.DotNet.Services.ScimV2.InMemory.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Looplex.DotNet.Services.ScimV2.InMemory.ExtensionMethods
{
    public static class ServicesExtensionMethods
    {
        public static void AddScimV2InMemoryServices(this IServiceCollection services)
        {
            services.AddSingleton<IGroupService, GroupService>();
            services.AddSingleton<IUserService, UserService>();
        }
    }
}
