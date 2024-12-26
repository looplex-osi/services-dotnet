using Azure.Security.KeyVault.Secrets;
using Looplex.DotNet.Core.Application.Abstractions.Services;

namespace Looplex.DotNet.Services.Secrets;

public class AzureSecretsService (SecretClient secretClient) : ISecretsService
{    
    public async Task<string?> GetSecretAsync(string secretName)
    {          
        if (string.IsNullOrEmpty(secretName))
            throw new Exception("The secret name is required.");

        try
        {
            var keyVaultSecret = await secretClient.GetSecretAsync(secretName);
            return keyVaultSecret.Value.Value;
        }
        catch(Exception ex) 
        {
            throw new Exception(ex.Message);
        }
    }
}
