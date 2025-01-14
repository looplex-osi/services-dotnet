using System.Net;
using System.Security.Cryptography;
using Looplex.DotNet.Core.Application.ExtensionMethods;
using Looplex.DotNet.Core.Common.Exceptions;
using Looplex.DotNet.Core.Common.Utils;
using Looplex.DotNet.Middlewares.ApiKeys.Application.Abstractions.Services;
using Looplex.DotNet.Middlewares.ApiKeys.Domain.Entities.ClientCredentials;
using Looplex.DotNet.Middlewares.ScimV2.Application.Abstractions.Providers;
using Looplex.DotNet.Middlewares.ScimV2.Domain.Entities;
using Looplex.DotNet.Middlewares.ScimV2.Domain.Entities.Messages;
using Looplex.DotNet.Middlewares.ScimV2.Domain.ExtensionMethods;
using Looplex.DotNet.Services.ApiKeys.InMemory.Dtos;
using Looplex.OpenForExtension.Abstractions.Commands;
using Looplex.OpenForExtension.Abstractions.Contexts;
using Looplex.OpenForExtension.Abstractions.ExtensionMethods;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto.Generators;
using ScimPatch;

namespace Looplex.DotNet.Services.ApiKeys.InMemory.Services
{
    public class ApiKeyService(
        IConfiguration configuration,
        IJsonSchemaProvider jsonSchemaProvider) : IApiKeyService
    {
        private const string JsonSchemaIdForClientCredentialKey = "JsonSchemaIdForClientCredential";
        
        internal static IList<ClientCredential> ClientCredentials = [];
        
        public Task GetAllAsync(IContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var startIndex = context.GetRequiredValue<int>("Pagination.StartIndex");
            var itemsPerPage = context.GetRequiredValue<int>("Pagination.ItemsPerPage");
            context.Plugins.Execute<IHandleInput>(context, cancellationToken);
            
            context.Plugins.Execute<IValidateInput>(context, cancellationToken);

            context.Plugins.Execute<IDefineRoles>(context, cancellationToken);

            context.Plugins.Execute<IBind>(context, cancellationToken);

            context.Plugins.Execute<IBeforeAction>(context, cancellationToken);

            if (!context.SkipDefaultAction)
            {
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
            }

            context.Plugins.Execute<IAfterAction>(context, cancellationToken);

            context.Plugins.Execute<IReleaseUnmanagedResources>(context, cancellationToken);
                        
            return Task.CompletedTask;
        }

        public Task GetByIdAsync(IContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var id = Guid.Parse((string?)context.AsScimV2Context().RouteValues["ClientCredentialId"]!);
            context.Plugins.Execute<IHandleInput>(context, cancellationToken);

            var clientCredential = ClientCredentials.FirstOrDefault(c => c.UniqueId == id);
            if (clientCredential == null)
            {
                throw new EntityNotFoundException(nameof(ClientCredential), id.ToString());
            }
            context.Plugins.Execute<IValidateInput>(context, cancellationToken);

            context.Roles.Add("ClientCredential", clientCredential);
            context.Plugins.Execute<IDefineRoles>(context, cancellationToken);

            context.Plugins.Execute<IBind>(context, cancellationToken);

            context.Plugins.Execute<IBeforeAction>(context, cancellationToken);

            if (!context.SkipDefaultAction)
            {
                context.Result = ((ClientCredential)context.Roles["ClientCredential"]).ToJson();
            }

            context.Plugins.Execute<IAfterAction>(context, cancellationToken);

            context.Plugins.Execute<IReleaseUnmanagedResources>(context, cancellationToken);

            return Task.CompletedTask;
        }

        public async Task CreateAsync(IContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var json = context.GetRequiredValue<string>("Resource");
            var schemaId = configuration[JsonSchemaIdForClientCredentialKey]!;
            var jsonSchema = await jsonSchemaProvider.ResolveJsonSchemaAsync(context, schemaId);
            var clientCredential = Resource.FromJson<ClientCredential>(json, jsonSchema, out var messages);
            await context.Plugins.ExecuteAsync<IHandleInput>(context, cancellationToken);

            if (messages.Count > 0)
            {
                throw new EntityInvalidException(messages.ToList());
            }
            await context.Plugins.ExecuteAsync<IValidateInput>(context, cancellationToken);

            context.Roles.Add("ClientCredential", clientCredential);
            await context.Plugins.ExecuteAsync<IDefineRoles>(context, cancellationToken);

            await context.Plugins.ExecuteAsync<IBind>(context, cancellationToken);

            await context.Plugins.ExecuteAsync<IBeforeAction>(context, cancellationToken);

            if (!context.SkipDefaultAction)
            {
                clientCredential = (ClientCredential)context.Roles["ClientCredential"];
                
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

            await context.Plugins.ExecuteAsync<IAfterAction>(context, cancellationToken);

            await context.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(context, cancellationToken);
        }
        
        public Task UpdateAsync(IContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            throw new NotImplementedException();
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
        
        public async Task PatchAsync(IContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var json = context.GetRequiredValue<string>("Operations");
            await GetByIdAsync(context, cancellationToken);
            var clientCredential = ((ClientCredential)context.Roles["ClientCredential"])
                .WithObservableProxy();
            context.Roles["ClientCredential"] = clientCredential;
            var operations = OperationTracker.FromJson(clientCredential, json);
            await context.Plugins.ExecuteAsync<IHandleInput>(context, cancellationToken);

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

            await context.Plugins.ExecuteAsync<IValidateInput>(context, cancellationToken);

            context.Roles["Operations"] = operations;
            await context.Plugins.ExecuteAsync<IDefineRoles>(context, cancellationToken);

            await context.Plugins.ExecuteAsync<IBind>(context, cancellationToken);

            await context.Plugins.ExecuteAsync<IBeforeAction>(context, cancellationToken);

            if (!context.SkipDefaultAction)
            {
                foreach (var operationNode in context.Roles["Operations"])
                {
                    if (!await operationNode.TryApplyAsync())
                    {
                        throw operationNode.OperationException;
                    }
                }
            }

            await context.Plugins.ExecuteAsync<IAfterAction>(context, cancellationToken);

            await context.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(context, cancellationToken);
        }

        public async Task DeleteAsync(IContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var id = Guid.Parse((string?)context.AsScimV2Context().RouteValues["ClientCredentialId"]!);
            await context.Plugins.ExecuteAsync<IHandleInput>(context, cancellationToken);

            await GetByIdAsync(context, cancellationToken);
            var clientCredential = (ClientCredential)context.Roles["ClientCredential"];
            if (clientCredential == null)
            {
                throw new EntityNotFoundException(nameof(ClientCredential), id.ToString());
            }
            await context.Plugins.ExecuteAsync<IValidateInput>(context, cancellationToken);
            
            await context.Plugins.ExecuteAsync<IDefineRoles>(context, cancellationToken);

            await context.Plugins.ExecuteAsync<IBind>(context, cancellationToken);

            await context.Plugins.ExecuteAsync<IBeforeAction>(context, cancellationToken);

            if (!context.SkipDefaultAction)
            {
                ClientCredentials.Remove(context.Roles["ClientCredential"]);
            }

            await context.Plugins.ExecuteAsync<IAfterAction>(context, cancellationToken);

            await context.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(context, cancellationToken);
        }

        public Task GetByIdAndSecretOrDefaultAsync(IContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            Guid clientId = Guid.Parse(context.State.ClientId);
            string clientSecret = context.State.ClientSecret;
            context.Plugins.Execute<IHandleInput>(context, cancellationToken);

            var digest = DigestCredentials(clientId, Convert.FromBase64String(clientSecret));
            var clientCredential = ClientCredentials.FirstOrDefault(c => c.Digest == digest);
            if (clientCredential != null && digest != null)
            {
                context.Roles.Add("ClientCredential", clientCredential);
            }
            context.Plugins.Execute<IValidateInput>(context, cancellationToken);
            
            context.Plugins.Execute<IDefineRoles>(context, cancellationToken);

            context.Plugins.Execute<IBind>(context, cancellationToken);

            context.Plugins.Execute<IBeforeAction>(context, cancellationToken);

            if (!context.SkipDefaultAction)
            {
                if (context.Roles.TryGetValue("ClientCredential", out var role))
                {
                    context.Result = ((ClientCredential)role).ToJson();
                }
            }

            context.Plugins.Execute<IAfterAction>(context, cancellationToken);

            context.Plugins.Execute<IReleaseUnmanagedResources>(context, cancellationToken);
            
            return Task.CompletedTask;
        }
    }
}
