using Azure;
using Azure.Security.KeyVault.Secrets;
using Looplex.DotNet.Core.Application.Abstractions.Services;
using Looplex.DotNet.Services.Secrets.Exceptions;
using Microsoft.Extensions.Logging;
using Polly;

namespace Looplex.DotNet.Services.Secrets;

public class AzureSecretsService : ISecretsService
{
    private readonly ILogger<AzureSecretsService> _logger;
    private readonly IAsyncPolicy<string> _retryPolicy;
    private readonly SecretClient _secretClient;

    public AzureSecretsService(SecretClient secretClient, ILogger<AzureSecretsService> logger)
    {
        _secretClient = secretClient;
        _logger = logger;
        _retryPolicy = Policy<string>
            .Handle<RequestFailedException>()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    public async Task<string?> GetSecretAsync(string secretName)
    {          
        if (string.IsNullOrEmpty(secretName))
            throw new SecretValidationException("The secret name is required.");

        _logger.LogInformation($"Retrieving secret: {secretName}", secretName);

        return await _retryPolicy.ExecuteAsync(async () =>
        {
            try
            {
                var keyVaultSecret = await _secretClient.GetSecretAsync(secretName);

                if (keyVaultSecret == null)
                    throw new KeyVaultException("Response<KeyVaultSecret> can not be null");
                if (keyVaultSecret.Value == null)
                    throw new KeyVaultException("KeyVaultSecret can not be null");

                return keyVaultSecret.Value.Value;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, $"Failed to retrieve secret: {secretName}", secretName);
                throw; 
            }
        });
    }
}
