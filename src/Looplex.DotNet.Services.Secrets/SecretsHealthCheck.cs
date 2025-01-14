using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Looplex.DotNet.Services.Secrets;

public class SecretsHealthCheck : IHealthCheck
{       
    private readonly IOptions<AzureSecretsOptions> _options;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SecretsHealthCheck> _logger;
    private const string TokenCacheKey = "AzureKeyVaultToken";

    public SecretsHealthCheck(
        IOptions<AzureSecretsOptions> options,
        IMemoryCache cache,
        ILogger<SecretsHealthCheck> logger)
    {
        _options = options;
        _cache = cache; 
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var options = _options.Value;
            options.Validate();

            if (!_cache.TryGetValue(TokenCacheKey, out AccessToken _))
            {
                var credential = new DefaultAzureCredential();
                var token = await credential.GetTokenAsync(
                    new TokenRequestContext(
                        new[] { $"{options.KeyVaultUrl}/.default" }),
                    cancellationToken);

                TimeSpan cacheTimeExpiration = token.ExpiresOn.UtcDateTime - DateTime.UtcNow;
                
                // Add 5-minute buffer to refresh token before it expires
                var bufferTime = TimeSpan.FromMinutes(5);
                var effectiveExpiration = cacheTimeExpiration > bufferTime
                                    ? cacheTimeExpiration - bufferTime
                                    : cacheTimeExpiration;

                _cache.Set(TokenCacheKey, token, effectiveExpiration);                
            }

            return HealthCheckResult.Healthy($"Key vault service is healthy.");
        }
        catch (OptionsValidationException ex)
        {
            _logger.LogError(ex, "Invalid Azure Key Vault configuration");
            return HealthCheckResult.Unhealthy("Invalid Azure Key Vault configuration", ex);
        }
        catch (AuthenticationFailedException ex)
        {
            _logger.LogError(ex, "Failed to authenticate with Azure Key Vault");
            return HealthCheckResult.Unhealthy("Failed to authenticate with Azure Key Vault", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error checking Key Vault health");
            return HealthCheckResult.Unhealthy("Key Vault Service is unhealthy", ex);
        }
    }
}
