using Newtonsoft.Json;

namespace Looplex.DotNet.Services.ApiKeys.InMemory.Dtos;

public class ClientCredentialDto
{
    [JsonProperty("client_id")]
    public required Guid ClientId { get; init; }
    
    [JsonProperty("client_secret")]
    public required string ClientSecret { get; init; }
}