using Looplex.DotNet.Core.Application.Abstractions.Services;
using Looplex.DotNet.Core.Application.ExtensionMethods;
using Looplex.DotNet.Core.Common.Exceptions;
using Looplex.DotNet.Middlewares.ScimV2.Application.Abstractions.OpenForExtensions;
using Looplex.DotNet.Middlewares.ScimV2.Application.Abstractions.Providers;
using Looplex.DotNet.Middlewares.ScimV2.Application.Abstractions.Services;
using Looplex.DotNet.Middlewares.ScimV2.Domain.Entities;
using Looplex.DotNet.Middlewares.ScimV2.Domain.Entities.Messages;
using Looplex.DotNet.Middlewares.ScimV2.Domain.Entities.Groups;
using Looplex.DotNet.Middlewares.ScimV2.Domain.ExtensionMethods;
using Looplex.DotNet.Middlewares.ScimV2.Services;
using Looplex.OpenForExtension.Abstractions.Contexts;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using ScimPatch;

namespace Looplex.DotNet.Services.ScimV2.InMemory.Services;

public class GroupService(
    IRbacService rbacService,
    IExtensionPointOrchestrator extensionPointOrchestrator,
    IConfiguration configuration,
    IJsonSchemaProvider jsonSchemaProvider)
    : BaseCrudService(rbacService, extensionPointOrchestrator), IGroupService
{
    private const string JsonSchemaIdForGroupKey = "JsonSchemaIdForGroup";

    internal static IList<Group> Groups = [];
        
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
        var id = Guid.Parse(context.GetRequiredRouteValue<string>("groupId"));
        context.State.GroupId = id;
        return Task.CompletedTask;
    }

    protected override Task GetByIdValidateInputAsync(IContext context)
    {
        var id = context.GetRequiredValue<Guid>("GroupId");
        var group = Groups.FirstOrDefault(u => u.UniqueId == id);
        if (group == null)
        {
            throw new EntityNotFoundException(nameof(Group), id.ToString());
        }
        context.State.Group = group;
        return Task.CompletedTask;
    }

    protected override Task GetByIdDefineRolesAsync(IContext context)
    {
        var group = context.GetRequiredValue<Group>("Group");
        context.Roles["Group"] = group;
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
        context.Result = ((Group)context.Roles["Group"]).ToJson();
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
        var schemaId = configuration[JsonSchemaIdForGroupKey]!;
        var jsonSchema = await jsonSchemaProvider.ResolveJsonSchemaAsync(context, schemaId);
        var json = context.GetRequiredValue<string>("Resource");
        var group = Resource.FromJson<Group>(json, jsonSchema, out var messages);
        context.State.Group = group;
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
        var group = context.GetRequiredValue<Group>("Group");
        context.Roles["Group"] = group;
        return Task.CompletedTask;
    }

    protected override Task CreateBindAsync(IContext context)
    {
        return Task.CompletedTask;
    }

    protected override Task CreateBeforeActionAsync(IContext context)
    {
        return Task.CompletedTask;
    }

    protected override Task CreateDefaultActionAsync(IContext context)
    {
        var group = context.Roles["Group"];
        group.Id = (Groups.Max(a => a.Id) ?? 0) + 1; // This should be generated by the DB
        group.UniqueId = Guid.NewGuid(); // This should be generated by the DB
        Groups.Add(group); // Persist in storage

        context.Result = context.Roles["Group"].UniqueId.ToString();
        return Task.CompletedTask;
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
        var group = ((Group)context.Roles["Group"])
            .WithObservableProxy();
        context.Roles["Group"] = group;
        var operations = OperationTracker.FromJson(group, json);
        context.State.Operations = operations;
    }

    protected override Task PatchValidateInputAsync(IContext context)
    {
        var operations = context.GetRequiredValue<IList<OperationNode>>("Operations");
        if (operations.Count == 0)
        {
            throw new InvalidOperationException("List of operations can't be empty.");
        }
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
        var id = Guid.Parse(context.GetRequiredRouteValue<string>("groupId"));
        context.State.GroupId = id;
        return Task.CompletedTask;
    }

    protected override async Task DeleteValidateInputAsync(IContext context)
    {
        var id = context.GetRequiredValue<Guid>("GroupId");
        await GetByIdAsync(context);
        var group = (Group)context.Roles["Group"];
        if (group == null)
        {
            throw new EntityNotFoundException(nameof(Group), id.ToString());
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
        Groups.Remove(context.Roles["Group"]);
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
}