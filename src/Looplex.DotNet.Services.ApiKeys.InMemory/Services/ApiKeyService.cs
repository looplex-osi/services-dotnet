using System.Net;
using System.Security.Cryptography;
using Looplex.DotNet.Core.Application.ExtensionMethods;
using Looplex.DotNet.Core.Common.Exceptions;
using Looplex.DotNet.Core.Common.Utils;
using Looplex.DotNet.Core.Domain;
using Looplex.DotNet.Middlewares.ApiKeys.Application.Abstractions.Services;
using Looplex.DotNet.Middlewares.ApiKeys.Domain.Entities.ApiKeys;
using Looplex.DotNet.Middlewares.ScimV2.Domain.Entities;
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
    public class ApiKeyService(IConfiguration configuration) : IApiKeyService
    {
        internal static IList<ApiKey> ApiKeys = [];

        private readonly IConfiguration _configuration = configuration;
        
        public Task GetAllAsync(IContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var page = context.GetRequiredValue<int>("Pagination.Page");
            var perPage = context.GetRequiredValue<int>("Pagination.PerPage");
            context.Plugins.Execute<IHandleInput>(context, cancellationToken);
            
            context.Plugins.Execute<IValidateInput>(context, cancellationToken);

            context.Plugins.Execute<IDefineRoles>(context, cancellationToken);

            context.Plugins.Execute<IBind>(context, cancellationToken);

            context.Plugins.Execute<IBeforeAction>(context, cancellationToken);

            if (!context.SkipDefaultAction)
            {
                var records = ApiKeys
                    .Skip(PaginationUtils.GetOffset(perPage, page))
                    .Take(perPage)
                    .ToList();

                var result = new PaginatedCollection
                {
                    Records = records.Select(r => (object)r).ToList(),
                    Page = page,
                    PerPage = perPage,
                    TotalCount = ApiKeys.Count
                };
                context.State.Pagination.TotalCount = ApiKeys.Count;
                
                context.Result = result.ToJson(ApiKey.Converter.Settings);
            }

            context.Plugins.Execute<IAfterAction>(context, cancellationToken);

            context.Plugins.Execute<IReleaseUnmanagedResources>(context, cancellationToken);
                        
            return Task.CompletedTask;
        }

        public Task GetByIdAsync(IContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var id = Guid.Parse(context.GetRequiredValue<string>("Id"));
            context.Plugins.Execute<IHandleInput>(context, cancellationToken);

            var apiKey = ApiKeys.FirstOrDefault(c => c.UniqueId == id);
            if (apiKey == null)
            {
                throw new EntityNotFoundException(nameof(ApiKey), id.ToString());
            }
            context.Plugins.Execute<IValidateInput>(context, cancellationToken);

            context.Roles.Add("ApiKey", apiKey);
            context.Plugins.Execute<IDefineRoles>(context, cancellationToken);

            context.Plugins.Execute<IBind>(context, cancellationToken);

            context.Plugins.Execute<IBeforeAction>(context, cancellationToken);

            if (!context.SkipDefaultAction)
            {
                context.Result = ((ApiKey)context.Roles["ApiKey"]).ToJson();
            }

            context.Plugins.Execute<IAfterAction>(context, cancellationToken);

            context.Plugins.Execute<IReleaseUnmanagedResources>(context, cancellationToken);

            return Task.CompletedTask;
        }

        public async Task CreateAsync(IContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var json = context.GetRequiredValue<string>("Resource");
            var apiKey = Resource.FromJson<ApiKey>(json, out var messages);
            await context.Plugins.ExecuteAsync<IHandleInput>(context, cancellationToken);

            if (messages.Count > 0)
            {
                throw new EntityInvalidException(messages.ToList());
            }
            await context.Plugins.ExecuteAsync<IValidateInput>(context, cancellationToken);

            context.Roles.Add("ApiKey", apiKey);
            await context.Plugins.ExecuteAsync<IDefineRoles>(context, cancellationToken);

            await context.Plugins.ExecuteAsync<IBind>(context, cancellationToken);

            await context.Plugins.ExecuteAsync<IBeforeAction>(context, cancellationToken);

            if (!context.SkipDefaultAction)
            {
                apiKey = (ApiKey)context.Roles["ApiKey"];
                
                var clientId = Guid.NewGuid();
                
                var clientSecretByteLength = int.Parse(_configuration["ClientSecretByteLength"]!);

                var clientSecretBytes = new byte[clientSecretByteLength];
                RandomNumberGenerator.Fill(clientSecretBytes);

                apiKey.ClientId = clientId;
                apiKey.Digest = DigestCredentials(clientId, clientSecretBytes)!;

                apiKey.Id = (ApiKeys.Max(a => a.Id) ?? 0) + 1; // This should be generated by the DB
                apiKey.UniqueId = Guid.NewGuid(); // This should be generated by the DB
                ApiKeys.Add(apiKey); // Persist in storage

                context.Result = context.Roles["ApiKey"].UniqueId.ToString();

                var httpContext = (HttpContext)context.State.HttpContext;
                await httpContext.Response.WriteAsJsonAsync(JsonConvert.SerializeObject(new ApiKeyDto
                {
                    ClientId = clientId,
                    ClientSecret = Convert.ToBase64String(clientSecretBytes)
                }), HttpStatusCode.Created);
            }

            await context.Plugins.ExecuteAsync<IAfterAction>(context, cancellationToken);

            await context.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(context, cancellationToken);
        }

        private string? DigestCredentials(Guid clientId, byte[] clientSecretBytes)
        {
            string? digest;

            try
            {
                var clientSecretDigestCost = int.Parse(_configuration["ClientSecretDigestCost"]!);

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
            var apiKey = ((ApiKey)context.Roles["ApiKey"])
                .WithObservableProxy();
            context.Roles["ApiKey"] = apiKey;
            var operations = OperationTracker.FromJson(apiKey, json);
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
            
            var id = Guid.Parse(context.GetRequiredValue<string>("Id"));
            await context.Plugins.ExecuteAsync<IHandleInput>(context, cancellationToken);

            await GetByIdAsync(context, cancellationToken);
            var apiKey = (ApiKey)context.Roles["ApiKey"];
            if (apiKey == null)
            {
                throw new EntityNotFoundException(nameof(ApiKey), id.ToString());
            }
            await context.Plugins.ExecuteAsync<IValidateInput>(context, cancellationToken);
            
            await context.Plugins.ExecuteAsync<IDefineRoles>(context, cancellationToken);

            await context.Plugins.ExecuteAsync<IBind>(context, cancellationToken);

            await context.Plugins.ExecuteAsync<IBeforeAction>(context, cancellationToken);

            if (!context.SkipDefaultAction)
            {
                ApiKeys.Remove(context.Roles["ApiKey"]);
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
            var apiKey = ApiKeys.FirstOrDefault(c => c.Digest == digest);
            if (apiKey != null && digest != null)
            {
                context.Roles.Add("ApiKey", apiKey);
            }
            context.Plugins.Execute<IValidateInput>(context, cancellationToken);
            
            context.Plugins.Execute<IDefineRoles>(context, cancellationToken);

            context.Plugins.Execute<IBind>(context, cancellationToken);

            context.Plugins.Execute<IBeforeAction>(context, cancellationToken);

            if (!context.SkipDefaultAction)
            {
                if (context.Roles.TryGetValue("ApiKey", out var role))
                {
                    context.Result = ((ApiKey)role).ToJson();;
                }
            }

            context.Plugins.Execute<IAfterAction>(context, cancellationToken);

            context.Plugins.Execute<IReleaseUnmanagedResources>(context, cancellationToken);
            
            return Task.CompletedTask;
        }
    }
}
