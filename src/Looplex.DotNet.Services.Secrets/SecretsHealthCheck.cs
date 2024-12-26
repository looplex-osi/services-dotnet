using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Looplex.DotNet.Services.Secrets;

public class SecretsHealthCheck(IConfiguration configuration) : IHealthCheck
{    
    private IConfiguration _configuration = configuration;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            string? azureKeyVaultUrl = _configuration[Constants.AzureKeyVaultUrlKey];

            if (string.IsNullOrEmpty(azureKeyVaultUrl))
                throw new Exception($"The key {Constants.AzureKeyVaultUrlKey} was not found.");
                        
            var credential = new DefaultAzureCredential();
            var token = await credential.GetTokenAsync(
                new TokenRequestContext(new[] { $"{azureKeyVaultUrl}/.default" })
            );


            return HealthCheckResult.Healthy($"Key vault service is healthy.");
        }
        catch(Exception ex)
        {
            return HealthCheckResult.Unhealthy("Key vault service is unhealthy.", ex);
        }
    }
}
