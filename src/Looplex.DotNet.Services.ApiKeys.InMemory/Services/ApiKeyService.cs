using System.Net;
using System.Security.Cryptography;
using Looplex.DotNet.Core.Application.Abstractions.Services;
using Looplex.DotNet.Core.Application.ExtensionMethods;
using Looplex.DotNet.Core.Common.Exceptions;
using Looplex.DotNet.Core.Common.Utils;
using Looplex.DotNet.Middlewares.ApiKeys.Application.Abstractions.Services;
using Looplex.DotNet.Middlewares.ApiKeys.Domain.Entities.ClientCredentials;
using Looplex.DotNet.Middlewares.ScimV2.Application.Abstractions.OpenForExtensions;
using Looplex.DotNet.Middlewares.ScimV2.Application.Abstractions.Providers;
using Looplex.DotNet.Middlewares.ScimV2.Domain.Entities;
using Looplex.DotNet.Middlewares.ScimV2.Domain.Entities.Messages;
using Looplex.DotNet.Middlewares.ScimV2.Domain.ExtensionMethods;
using Looplex.DotNet.Middlewares.ScimV2.Services;
using Looplex.DotNet.Services.ApiKeys.InMemory.Dtos;
using Looplex.OpenForExtension.Abstractions.Contexts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto.Generators;
using ScimPatch;

namespace Looplex.DotNet.Services.ApiKeys.InMemory.Services;

public class ApiKeyService(
    IRbacService rbacService,
    IExtensionPointOrchestrator extensionPointOrchestrator,
    IConfiguration configuration,
    IJsonSchemaProvider jsonSchemaProvider)
    : BaseCrudService(rbacService, extensionPointOrchestrator), IApiKeyService
{
    private const string JsonSchemaIdForClientCredentialKey = "JsonSchemaIdForClientCredential";

    internal static IList<ClientCredential> ClientCredentials = [];
    private readonly IExtensionPointOrchestrator _extensionPointOrchestrator = extensionPointOrchestrator;
    private readonly IRbacService _rbacService = rbacService;

    protected override Task GetAllHandleInputAsync(IContext context)
    {
        return Task.CompletedTask;
    }

    protected override Task GetAllValidateInputAsync(IContext context)
    {
        return Task.CompletedTask;
    }

    protected override Task GetAllDefineRolesAsync(IContext context)
    {
        return Task.CompletedTask;
    }

    protected override Task GetAllBindAsync(IContext context)
    {
        return Task.CompletedTask;
    }

    protected override Task GetAllBeforeActionAsync(IContext context)
    {
        return Task.CompletedTask;
    }

    protected override Task GetAllDefaultActionAsync(IContext context)
    {
        var startIndex = context.GetRequiredValue<int>("Pagination.StartIndex");
        var itemsPerPage = context.GetRequiredValue<int>("Pagination.ItemsPerPage");

        var records = ClientCredentials
            .Skip(Math.Min(0, startIndex - 1))
            .Take(itemsPerPage)
            .ToList();

        var result = new ListResponse
        {
            Resources = records.Select(r => (object)r).ToList(),
            StartIndex = startIndex,
            ItemsPerPage = itemsPerPage,
            TotalResults = ClientCredentials.Count
        };
        context.State.Pagination.TotalCount = ClientCredentials.Count;

        context.Result = JsonConvert.SerializeObject(result, ClientCredential.Converter.Settings);

        return Task.CompletedTask;
    }

    protected override Task GetAllAfterActionAsync(IContext context)
    {
        return Task.CompletedTask;
    }

    protected override Task GetAllReleaseUnmanagedResourcesAsync(IContext context)
    {
        return Task.CompletedTask;
    }

    protected override Task GetByIdHandleInputAsync(IContext context)
    {
        var id = Guid.Parse(context.GetRequiredRouteValue<string>("clientCredentialId"));
        context.State.ClientCredentialId = id;
        return Task.CompletedTask;
    }

    protected override Task GetByIdValidateInputAsync(IContext context)
    {
        var id = context.GetRequiredValue<Guid>("ClientCredentialId");
        var clientCredential = ClientCredentials.FirstOrDefault(c => c.UniqueId == id);
        if (clientCredential == null)
        {
            throw new EntityNotFoundException(nameof(ClientCredential), id.ToString());
        }
        context.State.ClientCredential = clientCredential;
        return Task.CompletedTask;
    }

    protected override Task GetByIdDefineRolesAsync(IContext context)
    {
        var clientCredential = context.GetRequiredValue<ClientCredential>("ClientCredential");
        context.Roles.Add("ClientCredential", clientCredential);
        return Task.CompletedTask;
    }

    protected override Task GetByIdBindAsync(IContext context)
    {
        return Task.CompletedTask;
    }

    protected override Task GetByIdBeforeActionAsync(IContext context)
    {
        return Task.CompletedTask;
    }

    protected override Task GetByIdDefaultActionAsync(IContext context)
    {
        context.Result = ((ClientCredential)context.Roles["ClientCredential"]).ToJson();
        return Task.CompletedTask;
    }

    protected override Task GetByIdAfterActionAsync(IContext context)
    {
        return Task.CompletedTask;
    }

    protected override Task GetByIdReleaseUnmanagedResourcesAsync(IContext context)
    {
        return Task.CompletedTask;
    }

    protected override async Task CreateHandleInputAsync(IContext context)
    {
        var json = context.GetRequiredValue<string>("Resource");
        var schemaId = configuration[JsonSchemaIdForClientCredentialKey]!;
        var jsonSchema = await jsonSchemaProvider.ResolveJsonSchemaAsync(context, schemaId);
        var clientCredential = Resource.FromJson<ClientCredential>(json, jsonSchema, out var messages);
        context.State.ClientCredential = clientCredential;
        context.State.Messages = messages;
    }

    protected override Task CreateValidateInputAsync(IContext context)
    {
        var messages = context.GetRequiredValue<IList<string>>("Messages");
        if (messages.Count > 0)
        {
            throw new EntityInvalidException(messages.ToList());
        }

        return Task.CompletedTask;
    }

    protected override Task CreateDefineRolesAsync(IContext context)
    {
        return Task.CompletedTask;
    }

    protected override Task CreateBindAsync(IContext context)
    {
        var clientCredential = context.GetRequiredValue<ClientCredential>("ClientCredential");
        context.Roles.Add("ClientCredential", clientCredential);
        return Task.CompletedTask;
    }

    protected override Task CreateBeforeActionAsync(IContext context)
    {
        return Task.CompletedTask;
    }

    protected override async Task CreateDefaultActionAsync(IContext context)
    {
        var clientCredential = (ClientCredential)context.Roles["ClientCredential"];

        var clientId = Guid.NewGuid();

        var clientSecretByteLength = int.Parse(configuration["ClientSecretByteLength"]!);

        var clientSecretBytes = new byte[clientSecretByteLength];
        RandomNumberGenerator.Fill(clientSecretBytes);

        clientCredential.ClientId = clientId;
        clientCredential.Digest = DigestCredentials(clientId, clientSecretBytes)!;

        clientCredential.Id = (ClientCredentials.Max(a => a.Id) ?? 0) + 1; // This should be generated by the DB
        clientCredential.UniqueId = Guid.NewGuid(); // This should be generated by the DB
        ClientCredentials.Add(clientCredential); // Persist in storage

        context.Result = context.Roles["ClientCredential"].UniqueId.ToString();

        var httpContext = (HttpContext)context.State.HttpContext;
        await httpContext.Response.WriteAsJsonAsync(JsonConvert.SerializeObject(new ClientCredentialDto
        {
            ClientId = clientId,
            ClientSecret = Convert.ToBase64String(clientSecretBytes)
        }), HttpStatusCode.Created);
    }

    private string? DigestCredentials(Guid clientId, byte[] clientSecretBytes)
    {
        string? digest;

        try
        {
            var clientSecretDigestCost = int.Parse(configuration["ClientSecretDigestCost"]!);

            digest = Convert.ToBase64String(BCrypt.Generate(
                clientSecretBytes,
                clientId.ToByteArray(),
                clientSecretDigestCost));
        }
        catch (Exception)
        {
            digest = null;
        }

        return digest;
    }

    protected override Task CreateAfterActionAsync(IContext context)
    {
        return Task.CompletedTask;
    }

    protected override Task CreateReleaseUnmanagedResourcesAsync(IContext context)
    {
        return Task.CompletedTask;
    }

    protected override Task UpdateHandleInputAsync(IContext context)
    {
        throw new NotImplementedException();
    }

    protected override Task UpdateValidateInputAsync(IContext context)
    {
        throw new NotImplementedException();
    }

    protected override Task UpdateDefineRolesAsync(IContext context)
    {
        throw new NotImplementedException();
    }

    protected override Task UpdateBindAsync(IContext context)
    {
        throw new NotImplementedException();
    }

    protected override Task UpdateBeforeActionAsync(IContext context)
    {
        throw new NotImplementedException();
    }

    protected override Task UpdateDefaultActionAsync(IContext context)
    {
        throw new NotImplementedException();
    }

    protected override Task UpdateAfterActionAsync(IContext context)
    {
        throw new NotImplementedException();
    }

    protected override Task UpdateReleaseUnmanagedResourcesAsync(IContext context)
    {
        throw new NotImplementedException();
    }

    protected override async Task PatchHandleInputAsync(IContext context)
    {
        var json = context.GetRequiredValue<string>("Operations");
        await GetByIdAsync(context);
        var clientCredential = ((ClientCredential)context.Roles["ClientCredential"])
            .WithObservableProxy();
        context.Roles["ClientCredential"] = clientCredential;
        var operations = OperationTracker.FromJson(clientCredential, json);
        context.State.Operations = operations;
    }

    protected override Task PatchValidateInputAsync(IContext context)
    {
        var operations = context.GetRequiredValue<IList<OperationNode>>("Operations");
        if (operations.Count == 0)
        {
            throw new InvalidOperationException("List of operations can't be empty.");
        }
            
        List<string> readonlyProperties = ["Digest", "ClientId"];
        var readonlyProperty = operations
            .FirstOrDefault(o => readonlyProperties.Contains(o.TargetProperty.Name))
            ?.TargetProperty.Name;

        if (readonlyProperty != null)
            throw new InvalidOperationException($"Cannot update {readonlyProperty}");

        return Task.CompletedTask;
    }

    protected override Task PatchDefineRolesAsync(IContext context)
    {
        var operations = context.GetRequiredValue<IList<OperationNode>>("Operations");
        context.Roles["Operations"] = operations;
        return Task.CompletedTask;
    }

    protected override Task PatchBindAsync(IContext context)
    {
        return Task.CompletedTask;
    }

    protected override Task PatchBeforeActionAsync(IContext context)
    {
        return Task.CompletedTask;
    }

    protected override async Task PatchDefaultActionAsync(IContext context)
    {
        foreach (var operationNode in context.Roles["Operations"])
        {
            if (!await operationNode.TryApplyAsync())
            {
                throw operationNode.OperationException;
            }
        }
    }

    protected override Task PatchAfterActionAsync(IContext context)
    {
        return Task.CompletedTask;
    }

    protected override Task PatchReleaseUnmanagedResourcesAsync(IContext context)
    {
        return Task.CompletedTask;
    }

    protected override Task DeleteHandleInputAsync(IContext context)
    {
        var id = Guid.Parse(context.GetRequiredRouteValue<string>("clientCredentialId"));
        context.State.ClientCredentialId = id;
        return Task.CompletedTask;
    }

    protected override async Task DeleteValidateInputAsync(IContext context)
    {
        var id = context.GetRequiredValue<Guid>("ClientCredentialId");
        await GetByIdAsync(context);
        var clientCredential = (ClientCredential)context.Roles["ClientCredential"];
        if (clientCredential == null)
        {
            throw new EntityNotFoundException(nameof(ClientCredential), id.ToString());
        }
    }

    protected override Task DeleteDefineRolesAsync(IContext context)
    {
        return Task.CompletedTask;
    }

    protected override Task DeleteBindAsync(IContext context)
    {
        return Task.CompletedTask;
    }

    protected override Task DeleteBeforeActionAsync(IContext context)
    {
        return Task.CompletedTask;
    }

    protected override Task DeleteDefaultActionAsync(IContext context)
    {
        ClientCredentials.Remove(context.Roles["ClientCredential"]);
        return Task.CompletedTask;
    }

    protected override Task DeleteAfterActionAsync(IContext context)
    {
        return Task.CompletedTask;
    }

    protected override Task DeleteReleaseUnmanagedResourcesAsync(IContext context)
    {
        return Task.CompletedTask;
    }

    public Task GetByIdAndSecretOrDefaultAsync(IContext context)
    {
        _rbacService.ThrowIfUnauthorized(context, GetType().Name, this.GetCallerName());
        
        return _extensionPointOrchestrator.OrchestrateAsync(
            context,
            GetByIdAndSecretOrDefaultHandleInputAsync,
            GetByIdAndSecretOrDefaultValidateInputAsync,
            GetByIdAndSecretOrDefaultDefineRolesAsync,
            GetByIdAndSecretOrDefaultBindAsync,
            GetByIdAndSecretOrDefaultBeforeActionAsync,
            GetByIdAndSecretOrDefaultDefaultActionAsync,
            GetByIdAndSecretOrDefaultAfterActionAsync,
            GetByIdAndSecretOrDefaultReleaseUnmanagedResourcesAsync);
    }

    private Task GetByIdAndSecretOrDefaultHandleInputAsync(IContext context)
    {
        Guid clientId = Guid.Parse(context.State.ClientId);
        string clientSecret = context.State.ClientSecret; 
        var digest = DigestCredentials(clientId, Convert.FromBase64String(clientSecret));
        var clientCredential = ClientCredentials.FirstOrDefault(c => c.Digest == digest);
#pragma warning disable CS8601 // Possible null reference assignment.
        context.State.Digest = digest;
        context.State.ClientCredential = clientCredential;
#pragma warning restore CS8601 // Possible null reference assignment.
        return Task.CompletedTask;
    }

    private Task GetByIdAndSecretOrDefaultValidateInputAsync(IContext context)
    {
        return Task.CompletedTask;
    }

    private Task GetByIdAndSecretOrDefaultDefineRolesAsync(IContext context)
    {
        var digest = context.GetValue<string>("Digest");
        var clientCredential = context.GetValue<ClientCredential>("ClientCredential");
        if (clientCredential != null && digest != null)
        {
            context.Roles.Add("ClientCredential", clientCredential);
        }

        return Task.CompletedTask;
    }

    private Task GetByIdAndSecretOrDefaultBindAsync(IContext context)
    {
        return Task.CompletedTask;
    }

    private Task GetByIdAndSecretOrDefaultBeforeActionAsync(IContext context)
    {
        return Task.CompletedTask;
    }

    private Task GetByIdAndSecretOrDefaultDefaultActionAsync(IContext context)
    {
        if (context.Roles.TryGetValue("ClientCredential", out var role))
        {
            context.Result = ((ClientCredential)role).ToJson();
        }

        return Task.CompletedTask;
    }

    private Task GetByIdAndSecretOrDefaultAfterActionAsync(IContext context)
    {
        return Task.CompletedTask;
    }

    private Task GetByIdAndSecretOrDefaultReleaseUnmanagedResourcesAsync(IContext context)
    {
        return Task.CompletedTask;
    }
}