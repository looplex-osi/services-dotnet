using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Looplex.DotNet.Core.Application.Abstractions.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Looplex.DotNet.Services.Secrets.ExtensionMethods;

public static class SecretsExtensionMethods
{   
    public static void AddSecretsServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AzureSecretsOptions>()            
            .Bind(configuration.GetSection("Azure:KeyVault"))
            .ValidateOnStart();

        services.AddSingleton<ISecretsService, AzureSecretsService>();
        services.AddSingleton(_ =>
        {
            var options = _.GetRequiredService<IOptions<AzureSecretsOptions>>().Value;
            options.Validate();
                                    
            return new SecretClient(new Uri(options.KeyVaultUrl), new DefaultAzureCredential());
        });
    }

    public static void AddSecretsHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<SecretsHealthCheck>("Default");
    }
}
