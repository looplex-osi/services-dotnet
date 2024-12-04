using Looplex.DotNet.Core.Application.ExtensionMethods;
using Looplex.DotNet.Core.Common.Exceptions;
using Looplex.DotNet.Middlewares.ScimV2.Application.Abstractions.Providers;
using Looplex.DotNet.Middlewares.ScimV2.Application.Abstractions.Services;
using Looplex.DotNet.Middlewares.ScimV2.Domain;
using Looplex.DotNet.Middlewares.ScimV2.Domain.Entities;
using Looplex.DotNet.Middlewares.ScimV2.Domain.Entities.Messages;
using Looplex.DotNet.Middlewares.ScimV2.Domain.Entities.Users;
using Looplex.OpenForExtension.Abstractions.Commands;
using Looplex.OpenForExtension.Abstractions.Contexts;
using Looplex.OpenForExtension.Abstractions.ExtensionMethods;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using ScimPatch;

namespace Looplex.DotNet.Services.ScimV2.InMemory.Services
{
    public class UserService(
        IConfiguration configuration,
        IJsonSchemaProvider jsonSchemaProvider) : IUserService
    {
        private const string JsonSchemaIdForUserKey = "JsonSchemaIdForUser";

        internal static IList<User> Users = [];

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
            }

            context.Plugins.Execute<IAfterAction>(context, cancellationToken);

            context.Plugins.Execute<IReleaseUnmanagedResources>(context, cancellationToken);

            return Task.CompletedTask;
        }

        public Task GetByIdAsync(IContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var id = Guid.Parse((string?)((IScimV2Context)context).RouteValues["UserId"]!);
            context.Plugins.Execute<IHandleInput>(context, cancellationToken);

            var user = Users.FirstOrDefault(u => u.UniqueId == id);
            if (user == null)
            {
                throw new EntityNotFoundException(nameof(User), id.ToString());
            }
            context.Plugins.Execute<IValidateInput>(context, cancellationToken);

            context.Roles["User"] = user;
            context.Plugins.Execute<IDefineRoles>(context, cancellationToken);

            context.Plugins.Execute<IBind>(context, cancellationToken);

            context.Plugins.Execute<IBeforeAction>(context, cancellationToken);

            if (!context.SkipDefaultAction)
            {
                context.Result = ((User)context.Roles["User"]).ToJson();
            }

            context.Plugins.Execute<IAfterAction>(context, cancellationToken);

            context.Plugins.Execute<IReleaseUnmanagedResources>(context, cancellationToken);

            return Task.CompletedTask;
        }
        
        public async Task CreateAsync(IContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var schemaId = configuration[JsonSchemaIdForUserKey]!;
            var jsonSchema = await jsonSchemaProvider.ResolveJsonSchemaAsync(context, schemaId);
            var json = context.GetRequiredValue<string>("Resource");
            var user = Resource.FromJson<User>(json, jsonSchema, out var messages);
            await context.Plugins.ExecuteAsync<IHandleInput>(context, cancellationToken);

            if (messages.Count > 0)
            {
                throw new EntityInvalidException(messages.ToList());
            }
            await context.Plugins.ExecuteAsync<IValidateInput>(context, cancellationToken);

            context.Roles["User"] = user;
            await context.Plugins.ExecuteAsync<IDefineRoles>(context, cancellationToken);

            await context.Plugins.ExecuteAsync<IBind>(context, cancellationToken);

            await context.Plugins.ExecuteAsync<IBeforeAction>(context, cancellationToken);

            if (!context.SkipDefaultAction)
            {
                user = context.Roles["User"];
                user.Id = (Users.Max(a => a.Id) ?? 0) + 1; // This should be generated by the DB
                user.UniqueId = Guid.NewGuid(); // This should be generated by the DB
                Users.Add(user); // Persist in storage

                context.Result = context.Roles["User"].UniqueId.ToString();
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
            var user = ((User)context.Roles["User"])
                .WithObservableProxy();
            context.Roles["User"] = user;
            var operations = OperationTracker.FromJson(user, json);
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

            var id = Guid.Parse((string?)((IScimV2Context)context).RouteValues["UserId"]!);
            await context.Plugins.ExecuteAsync<IHandleInput>(context, cancellationToken);

            await GetByIdAsync(context, cancellationToken);
            var user = (User)context.Roles["User"];
            if (user == null)
            {
                throw new EntityNotFoundException(nameof(User), id.ToString());
            }
            await context.Plugins.ExecuteAsync<IValidateInput>(context, cancellationToken);

            await context.Plugins.ExecuteAsync<IDefineRoles>(context, cancellationToken);

            await context.Plugins.ExecuteAsync<IBind>(context, cancellationToken);

            await context.Plugins.ExecuteAsync<IBeforeAction>(context, cancellationToken);

            if (!context.SkipDefaultAction)
            {
                Users.Remove(context.Roles["User"]);
            }

            await context.Plugins.ExecuteAsync<IAfterAction>(context, cancellationToken);

            await context.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(context, cancellationToken);
        }
    }
}
