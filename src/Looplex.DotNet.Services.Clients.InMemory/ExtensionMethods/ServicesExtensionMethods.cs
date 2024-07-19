using Looplex.DotNet.Middlewares.Clients.Application.Abstractions.Services;
using Looplex.DotNet.Services.Clients.InMemory.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Looplex.DotNet.Services.Clients.InMemory.ExtensionMethods
{
    public static class ServicesExtensionMethods
    {
        public static void AddClientsInMemoryServices(this IServiceCollection services)
        {
            services.AddSingleton<IClientService, ClientService>();
        }
    }
}
