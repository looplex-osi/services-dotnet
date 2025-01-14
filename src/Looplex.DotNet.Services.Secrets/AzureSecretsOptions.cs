using Microsoft.Extensions.Options;

namespace Looplex.DotNet.Services.Secrets
{
    /// <summary>
    /// Configuration options for Azure Key Vault secrets service.
    /// +/// </summary>
    /// <remarks>
    /// This class is configured using the "Azure:KeyVault" configuration section.
    /// 
    /// Example usage:
    /// <code>
    /// services.Configure<AzureSecretsOptions>(configuration.GetSection("Azure:KeyVault"));
    /// </code>
    /// 
    /// For more information about Azure Key Vault, see:
    /// https://learn.microsoft.com/azure/key-vault/
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
