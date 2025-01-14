using Azure;
using Azure.Security.KeyVault.Secrets;
using Looplex.DotNet.Core.Application.Abstractions.Services;
using Looplex.DotNet.Services.Secrets.Exceptions;
using Microsoft.Extensions.Logging;
using Polly;
using System.Security.Cryptography;
using System.Text;

namespace Looplex.DotNet.Services.Secrets;

/// <summary>
/// Provides functionality to securely retrieve secrets from Azure Key Vault.
/// </summary>
public class AzureSecretsService : ISecretsService
{
    private readonly ILogger<AzureSecretsService> _logger;
    private readonly IAsyncPolicy<string> _retryPolicy;
    private readonly SecretClient _secretClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureSecretsService"/> class.
    /// </summary>
    /// <param name="secretClient">The Azure Key Vault secret client.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when secretClient or logger is null.</exception>
    public AzureSecretsService(SecretClient secretClient, ILogger<AzureSecretsService> logger)
    {
        _secretClient = secretClient ?? throw new ArgumentNullException(nameof(secretClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _retryPolicy = Policy<string>
            .Handle<RequestFailedException>()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    public async Task<string?> GetSecretAsync(string secretName)
    {
        if (string.IsNullOrEmpty(secretName))
            throw new SecretValidationException("The secret name is required.");
        
        string secretHash = ComputeHash(secretName) ;

        _logger.LogInformation($"Retrieving secret: {secretHash}", secretHash);

        return await _retryPolicy.ExecuteAsync(async () =>
        {
            try
            {
                var keyVaultSecret = await _secretClient.GetSecretAsync(secretName);

                if (keyVaultSecret == null)
                    throw new KeyVaultException("The response from Azure Key Vault can not be null");
                if (keyVaultSecret.Value == null)
                    throw new KeyVaultException("The secret value can not be null");

                return keyVaultSecret.Value.Value;
            }
            catch (RequestFailedException ex)
            {                
                _logger.LogError(ex, $"Failed to retrieve secret: {secretHash}", secretHash);
                throw; 
            }
        });
    }

    public string ComputeHash(string input)
    {
        if (string.IsNullOrEmpty(input))
            throw new ArgumentNullException(nameof(input));

        string hash = string.Empty;

        using (SHA256 sha256Hash = SHA256.Create())
        {
            hash = GetHash(sha256Hash, input);                       
        }
        return hash;
    }

    private static string GetHash(HashAlgorithm hashAlgorithm, string input)
    {

        // Convert the input string to a byte array and compute the hash.
        byte[] data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));

        // Create a new Stringbuilder to collect the bytes
        // and create a string.
        var sBuilder = new StringBuilder();

        // Loop through each byte of the hashed data
        // and format each one as a hexadecimal string.
        for (int i = 0; i < data.Length; i++)
        {
            sBuilder.Append(data[i].ToString("x2"));
        }

        // Return the hexadecimal string.
        return sBuilder.ToString();
    }

    // Verify a hash against a string.
    private static bool VerifyHash(HashAlgorithm hashAlgorithm, string input, string hash)
    {
        // Hash the input.
        var hashOfInput = GetHash(hashAlgorithm, input);

        // Create a StringComparer an compare the hashes.
        StringComparer comparer = StringComparer.OrdinalIgnoreCase;

        return comparer.Compare(hashOfInput, hash) == 0;
    }
}
