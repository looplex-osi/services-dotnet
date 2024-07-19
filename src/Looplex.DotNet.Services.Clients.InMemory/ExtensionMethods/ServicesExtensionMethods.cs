using Looplex.DotNet.Middlewares.OAuth2.Services;
using Looplex.DotNet.Services.Clients.InMemory.Profiles;
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

        public static void AddClientsInMemoryAutoMapper(this IServiceCollection services)
        {
            services.AddAutoMapper(typeof(ClientsInMemoryProfile));
        }
    }
}
