using Looplex.DotNet.Services.Secrets.Exceptions;

namespace Looplex.DotNet.Services.Secrets
{
    public class AzureSecretsOptions
    {
        public string KeyVaultUrl { get; set; } = string.Empty;
        
        public void Validate()
        {
            if (string.IsNullOrEmpty(KeyVaultUrl))
                throw new OptionsValidationException(
                    nameof(AzureSecretsOptions),
                    typeof(AzureSecretsOptions),
                    new[] { $"The {nameof(KeyVaultUrl)} is required." });
        }
    }
}
