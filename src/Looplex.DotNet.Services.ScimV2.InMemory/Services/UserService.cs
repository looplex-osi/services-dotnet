﻿using Looplex.DotNet.Core.Application.Abstractions.Services;
using Looplex.DotNet.Core.Application.ExtensionMethods;
using Looplex.DotNet.Core.Common.Exceptions;
using Looplex.DotNet.Middlewares.ScimV2.Application.Abstractions.OpenForExtensions;
using Looplex.DotNet.Middlewares.ScimV2.Application.Abstractions.Providers;
using Looplex.DotNet.Middlewares.ScimV2.Application.Abstractions.Services;
using Looplex.DotNet.Middlewares.ScimV2.Domain.Entities;
using Looplex.DotNet.Middlewares.ScimV2.Domain.Entities.Messages;
using Looplex.DotNet.Middlewares.ScimV2.Domain.Entities.Users;
using Looplex.DotNet.Middlewares.ScimV2.Domain.ExtensionMethods;
using Looplex.DotNet.Middlewares.ScimV2.Services;
using Looplex.OpenForExtension.Abstractions.Contexts;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using ScimPatch;

namespace Looplex.DotNet.Services.ScimV2.InMemory.Services;

public class UserService(
    IRbacService rbacService,
    IExtensionPointOrchestrator extensionPointOrchestrator,
    IConfiguration configuration,
    IJsonSchemaProvider jsonSchemaProvider)
    : BaseCrudService(rbacService, extensionPointOrchestrator), IUserService
{
    private const string JsonSchemaIdForUserKey = "JsonSchemaIdForUser";

    internal static IList<User> Users = [];
        
    protected override Task GetAllHandleInputAsync(IContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected override Task GetAllValidateInputAsync(IContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected override Task GetAllDefineRolesAsync(IContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected override Task GetAllBindAsync(IContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected override Task GetAllBeforeActionAsync(IContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected override Task GetAllDefaultActionAsync(IContext context, CancellationToken cancellationToken)
    {
        var startIndex = context.GetRequiredValue<int>("Pagination.StartIndex");
        var itemsPerPage = context.GetRequiredValue<int>("Pagination.ItemsPerPage");
            
        var records = Users
            .Skip(Math.Min(0, startIndex - 1))
            .Take(itemsPerPage)
            .ToList();

        var result = new ListResponse
        {
            Resources = records.Select(r => (object)r).ToList(),
            StartIndex = startIndex,
            ItemsPerPage = itemsPerPage,
            TotalResults = Users.Count
                    
        };
        context.State.Pagination.TotalCount = Users.Count;
                
        context.Result = JsonConvert.SerializeObject(result, User.Converter.Settings);

        return Task.CompletedTask;
    }

    protected override Task GetAllAfterActionAsync(IContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected override Task GetAllReleaseUnmanagedResourcesAsync(IContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected override Task GetByIdHandleInputAsync(IContext context, CancellationToken cancellationToken)
    {
        var id = Guid.Parse((string?)context.AsScimV2Context().RouteValues["UserId"]!);
        context.State.UserId = id;
        return Task.CompletedTask;
    }

    protected override Task GetByIdValidateInputAsync(IContext context, CancellationToken cancellationToken)
    {
        var id = context.GetRequiredValue<Guid>("UserId");
        var user = Users.FirstOrDefault(u => u.UniqueId == id);
        if (user == null)
        {
            throw new EntityNotFoundException(nameof(User), id.ToString());
        }
        context.State.User = user;
        return Task.CompletedTask;
    }

    protected override Task GetByIdDefineRolesAsync(IContext context, CancellationToken cancellationToken)
    {
        var user = context.GetRequiredValue<User>("User");
        context.Roles["User"] = user;
        return Task.CompletedTask;
    }

    protected override Task GetByIdBindAsync(IContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected override Task GetByIdBeforeActionAsync(IContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected override Task GetByIdDefaultActionAsync(IContext context, CancellationToken cancellationToken)
    {
        context.Result = ((User)context.Roles["User"]).ToJson();
        return Task.CompletedTask;
    }

    protected override Task GetByIdAfterActionAsync(IContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected override Task GetByIdReleaseUnmanagedResourcesAsync(IContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected override async Task CreateHandleInputAsync(IContext context, CancellationToken cancellationToken)
    {
        var schemaId = configuration[JsonSchemaIdForUserKey]!;
        var jsonSchema = await jsonSchemaProvider.ResolveJsonSchemaAsync(context, schemaId);
        var json = context.GetRequiredValue<string>("Resource");
        var user = Resource.FromJson<User>(json, jsonSchema, out var messages);
        context.State.User = user;
        context.State.Messages = messages;
    }

    protected override Task CreateValidateInputAsync(IContext context, CancellationToken cancellationToken)
    {
        var messages = context.GetRequiredValue<IList<string>>("Messages");
        if (messages.Count > 0)
        {
            throw new EntityInvalidException(messages.ToList());
        }
        return Task.CompletedTask;
    }

    protected override Task CreateDefineRolesAsync(IContext context, CancellationToken cancellationToken)
    {
        var user = context.GetRequiredValue<User>("User");
        context.Roles["User"] = user;
        return Task.CompletedTask;
    }

    protected override Task CreateBindAsync(IContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected override Task CreateBeforeActionAsync(IContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected override Task CreateDefaultActionAsync(IContext context, CancellationToken cancellationToken)
    {
        var user = context.Roles["User"];
        user.Id = (Users.Max(a => a.Id) ?? 0) + 1; // This should be generated by the DB
        user.UniqueId = Guid.NewGuid(); // This should be generated by the DB
        Users.Add(user); // Persist in storage

        context.Result = context.Roles["User"].UniqueId.ToString();
        return Task.CompletedTask;
    }

    protected override Task CreateAfterActionAsync(IContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected override Task CreateReleaseUnmanagedResourcesAsync(IContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected override Task UpdateHandleInputAsync(IContext context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    protected override Task UpdateValidateInputAsync(IContext context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    protected override Task UpdateDefineRolesAsync(IContext context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    protected override Task UpdateBindAsync(IContext context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    protected override Task UpdateBeforeActionAsync(IContext context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    protected override Task UpdateDefaultActionAsync(IContext context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    protected override Task UpdateAfterActionAsync(IContext context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    protected override Task UpdateReleaseUnmanagedResourcesAsync(IContext context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    protected override async Task PatchHandleInputAsync(IContext context, CancellationToken cancellationToken)
    {
        var json = context.GetRequiredValue<string>("Operations");
        await GetByIdAsync(context, cancellationToken);
        var user = ((User)context.Roles["User"])
            .WithObservableProxy();
        context.Roles["User"] = user;
        var operations = OperationTracker.FromJson(user, json);
        context.State.Operations = operations;
    }

    protected override Task PatchValidateInputAsync(IContext context, CancellationToken cancellationToken)
    {
        var operations = context.GetRequiredValue<IList<OperationNode>>("Operations");
        if (operations.Count == 0)
        {
            throw new InvalidOperationException("List of operations can't be empty.");
        }
        return Task.CompletedTask;
    }

    protected override Task PatchDefineRolesAsync(IContext context, CancellationToken cancellationToken)
    {
        var operations = context.GetRequiredValue<IList<OperationNode>>("Operations");
        context.Roles["Operations"] = operations;
        return Task.CompletedTask;
    }

    protected override Task PatchBindAsync(IContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected override Task PatchBeforeActionAsync(IContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected override async Task PatchDefaultActionAsync(IContext context, CancellationToken cancellationToken)
    {
        foreach (var operationNode in context.Roles["Operations"])
        {
            if (!await operationNode.TryApplyAsync())
            {
                throw operationNode.OperationException;
            }
        }
    }

    protected override Task PatchAfterActionAsync(IContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected override Task PatchReleaseUnmanagedResourcesAsync(IContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected override Task DeleteHandleInputAsync(IContext context, CancellationToken cancellationToken)
    {
        var id = Guid.Parse((string?)context.AsScimV2Context().RouteValues["UserId"]!);
        context.State.UserId = id;
        return Task.CompletedTask;
    }

    protected override async Task DeleteValidateInputAsync(IContext context, CancellationToken cancellationToken)
    {
        var id = context.GetRequiredValue<Guid>("UserId");
        await GetByIdAsync(context, cancellationToken);
        var user = (User)context.Roles["User"];
        if (user == null)
        {
            throw new EntityNotFoundException(nameof(User), id.ToString());
        }
    }

    protected override Task DeleteDefineRolesAsync(IContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected override Task DeleteBindAsync(IContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected override Task DeleteBeforeActionAsync(IContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected override Task DeleteDefaultActionAsync(IContext context, CancellationToken cancellationToken)
    {
        Users.Remove(context.Roles["User"]);
        return Task.CompletedTask;
    }

    protected override Task DeleteAfterActionAsync(IContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected override Task DeleteReleaseUnmanagedResourcesAsync(IContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}