using Looplex.DotNet.Middlewares.ApiKeys.Application.Abstractions.Services;
using Looplex.DotNet.Services.ApiKeys.InMemory.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Looplex.DotNet.Services.ApiKeys.InMemory.ExtensionMethods;

public static class ServicesExtensionMethods
{
    public static void AddApiKeyInMemoryServices(this IServiceCollection services)
    {
        services.AddSingleton<IApiKeyService, ApiKeyService>();
    }
}