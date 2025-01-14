using Looplex.DotNet.Core.Application.ExtensionMethods;
using Looplex.DotNet.Core.Common.Exceptions;
using Looplex.DotNet.Middlewares.ScimV2.Application.Abstractions.Providers;
using Looplex.DotNet.Middlewares.ScimV2.Application.Abstractions.Services;
using Looplex.DotNet.Middlewares.ScimV2.Domain.Entities;
using Looplex.DotNet.Middlewares.ScimV2.Domain.Entities.Groups;
using Looplex.DotNet.Middlewares.ScimV2.Domain.Entities.Messages;
using Looplex.DotNet.Middlewares.ScimV2.Domain.ExtensionMethods;
using Looplex.OpenForExtension.Abstractions.Commands;
using Looplex.OpenForExtension.Abstractions.Contexts;
using Looplex.OpenForExtension.Abstractions.ExtensionMethods;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using ScimPatch;

namespace Looplex.DotNet.Services.ScimV2.InMemory.Services
{
    public class GroupService(
        IConfiguration configuration,
        IJsonSchemaProvider jsonSchemaProvider) : IGroupService
    {
        // TODO inherit BaseCrudService 

        private const string JsonSchemaIdForGroupKey = "JsonSchemaIdForGroup";
        
        internal static IList<Group> Groups = [];
        
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
                var records = Groups
                    .Skip(Math.Min(0, startIndex - 1))
                    .Take(itemsPerPage)
                    .ToList();

                var result = new ListResponse
                {
                    Resources = records.Select(r => (object)r).ToList(),
                    StartIndex = startIndex,
                    ItemsPerPage = itemsPerPage,
                    TotalResults = Groups.Count
                };
                context.State.Pagination.TotalCount = Groups.Count;
                
                context.Result = JsonConvert.SerializeObject(result, Group.Converter.Settings);
            }

            context.Plugins.Execute<IAfterAction>(context, cancellationToken);

            context.Plugins.Execute<IReleaseUnmanagedResources>(context, cancellationToken);

            return Task.CompletedTask;
        }

        public Task GetByIdAsync(IContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var id = Guid.Parse((string?)context.AsScimV2Context().RouteValues["GroupId"]!);
            context.Plugins.Execute<IHandleInput>(context, cancellationToken);

            var group = Groups.FirstOrDefault(g => g.UniqueId == id);
            if (group == null)
            {
                throw new EntityNotFoundException(nameof(Group), id.ToString());
            }
            context.Plugins.Execute<IValidateInput>(context, cancellationToken);
            
            context.Roles.Add("Group", group);
            context.Plugins.Execute<IDefineRoles>(context, cancellationToken);

            context.Plugins.Execute<IBind>(context, cancellationToken);

            context.Plugins.Execute<IBeforeAction>(context, cancellationToken);

            if (!context.SkipDefaultAction)
            {
                context.Result = ((Group)context.Roles["Group"]).ToJson();
            }

            context.Plugins.Execute<IAfterAction>(context, cancellationToken);

            context.Plugins.Execute<IReleaseUnmanagedResources>(context, cancellationToken);

            return Task.CompletedTask;
        }
        
        public async Task CreateAsync(IContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var schemaId = configuration[JsonSchemaIdForGroupKey]!;
            var jsonSchema = await jsonSchemaProvider.ResolveJsonSchemaAsync(context, schemaId);
            var json = context.GetRequiredValue<string>("Resource");
            var group = Resource
                .FromJson<Group>(json, jsonSchema, out var messages);
            await context.Plugins.ExecuteAsync<IHandleInput>(context, cancellationToken);

            if (messages.Count > 0)
            {
                throw new EntityInvalidException(messages.ToList());
            }
            await context.Plugins.ExecuteAsync<IValidateInput>(context, cancellationToken);

            context.Roles.Add("Group", group);
            await context.Plugins.ExecuteAsync<IDefineRoles>(context, cancellationToken);

            await context.Plugins.ExecuteAsync<IBind>(context, cancellationToken);

            await context.Plugins.ExecuteAsync<IBeforeAction>(context, cancellationToken);

            if (!context.SkipDefaultAction)
            {
                group = context.Roles["Group"];
                group.Id = (Groups.Max(a => a.Id) ?? 0) + 1; // This should be generated by the DB
                group.UniqueId = Guid.NewGuid(); // This should be generated by the DB
                Groups.Add(group); // Persist in storage

                context.Result = context.Roles["Group"].UniqueId.ToString();
            }

            await context.Plugins.ExecuteAsync<IAfterAction>(context, cancellationToken);

            await context.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(context, cancellationToken);
        }
        
        public Task UpdateAsync(IContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            throw new NotImplementedException();
        }
        
        public async Task PatchAsync(IContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var json = context.GetRequiredValue<string>("Operations");
            await GetByIdAsync(context, cancellationToken);
            var group = ((Group)context.Roles["Group"])
                .WithObservableProxy();
            context.Roles["Group"] = group;
            var operations = OperationTracker.FromJson(group, json);
            await context.Plugins.ExecuteAsync<IHandleInput>(context, cancellationToken);

            if (operations.Count == 0)
            {
                throw new InvalidOperationException("List of operations can't be empty.");
            }
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

            var id = Guid.Parse((string?)context.AsScimV2Context().RouteValues["GroupId"]!);
            await context.Plugins.ExecuteAsync<IHandleInput>(context, cancellationToken);

            await GetByIdAsync(context, cancellationToken);
            var group = (Group)context.Roles["Group"];
            if (group == null)
            {
                throw new EntityNotFoundException(nameof(Group), id.ToString());
            }
            await context.Plugins.ExecuteAsync<IValidateInput>(context, cancellationToken);
            
            await context.Plugins.ExecuteAsync<IDefineRoles>(context, cancellationToken);

            await context.Plugins.ExecuteAsync<IBind>(context, cancellationToken);

            await context.Plugins.ExecuteAsync<IBeforeAction>(context, cancellationToken);

            if (!context.SkipDefaultAction)
            {
                Groups.Remove(context.Roles["Group"]);
            }

            await context.Plugins.ExecuteAsync<IAfterAction>(context, cancellationToken);

            await context.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(context, cancellationToken);
        }
    }
}
