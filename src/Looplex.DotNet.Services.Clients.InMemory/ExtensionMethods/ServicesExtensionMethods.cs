using Looplex.DotNet.Middlewares.OAuth2.Services;
using Looplex.DotNet.Middlewares.OAuth2.Storages.InMemory;
using Microsoft.Extensions.DependencyInjection;

namespace Looplex.DotNet.Middlewares.OAuth2.Storages.Default.ExtensionMethods
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
