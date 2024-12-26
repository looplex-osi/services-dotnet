using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Looplex.DotNet.Core.Application.Abstractions.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Looplex.DotNet.Services.Secrets.ExtensionMethods;

public static class SecretsExtensionMethods
{   
    public static void AddSecretsServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ISecretsService, AzureSecretsService>();
        services.AddSingleton(_ =>
        {
            string? azureKeyVaultUrl = configuration[Constants.AzureKeyVaultUrlKey];

            if (string.IsNullOrEmpty(azureKeyVaultUrl))
                throw new Exception($"The key {Constants.AzureKeyVaultUrlKey} was not found.");
                        
            return new SecretClient(new Uri(azureKeyVaultUrl), new DefaultAzureCredential());
        });
    }

    public static void AddSecretsHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<SecretsHealthCheck>("Default");
    }
}
