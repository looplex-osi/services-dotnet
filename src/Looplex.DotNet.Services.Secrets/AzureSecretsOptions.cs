using Microsoft.Extensions.Options;

namespace Looplex.DotNet.Services.Secrets
{
    /// <summary>
    /// Configuration options for Azure Key Vault secrets service.
    /// </summary>
    public sealed class AzureSecretsOptions
    {
        /// <summary>
        /// Gets or sets the Azure Key Vault URL.
        /// </summary>
        public string KeyVaultUrl { get; set; } = string.Empty;
        
        public void Validate()
        {
            if (string.IsNullOrEmpty(KeyVaultUrl))
                throw new OptionsValidationException(
                    nameof(AzureSecretsOptions),
                    typeof(AzureSecretsOptions),
                    new[] { $"The {nameof(KeyVaultUrl)} is required." });

            if (!Uri.TryCreate(KeyVaultUrl, UriKind.Absolute, out _))
                throw new OptionsValidationException(
                nameof(AzureSecretsOptions),
                typeof(AzureSecretsOptions),
                new[] { $"The {nameof(KeyVaultUrl)} must be a valid URL." });
        }
    }
}
