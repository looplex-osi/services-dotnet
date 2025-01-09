using Azure;
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
            Response<KeyVaultSecret> keyVaultSecret = await secretClient.GetSecretAsync(secretName);                       
            
            if(keyVaultSecret == null) 
                throw new Exception("Response<KeyVaultSecret> can not be null");
            if (keyVaultSecret.Value == null)
                throw new Exception("KeyVaultSecret can not be null");            
                
            return keyVaultSecret.Value.Value;
        }
        catch(Exception ex) 
        {
            throw new Exception(ex.Message);
        }
    }
}
