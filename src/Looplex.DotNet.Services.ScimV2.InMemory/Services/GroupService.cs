using Looplex.DotNet.Core.Application.ExtensionMethods;
using Looplex.DotNet.Core.Common.Exceptions;
using Looplex.DotNet.Core.Common.Utils;
using Looplex.DotNet.Core.Domain;
using Looplex.DotNet.Middlewares.ScimV2.Application.Abstractions.Services;
using Looplex.DotNet.Middlewares.ScimV2.Domain.Entities;
using Looplex.DotNet.Middlewares.ScimV2.Domain.Entities.Groups;
using Looplex.OpenForExtension.Abstractions.Commands;
using Looplex.OpenForExtension.Abstractions.Contexts;
using Looplex.OpenForExtension.Abstractions.ExtensionMethods;

namespace Looplex.DotNet.Services.ScimV2.InMemory.Services
{
    public class GroupService() : IGroupService
    {
        private static readonly IList<Group> _groups = [];
        
        public Task GetAllAsync(IContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            context.Plugins.Execute<IHandleInput>(context, cancellationToken);
            var page = context.GetRequiredValue<int>("Pagination.Page");
            var perPage = context.GetRequiredValue<int>("Pagination.PerPage");

            context.Plugins.Execute<IValidateInput>(context, cancellationToken);

            context.Plugins.Execute<IDefineRoles>(context, cancellationToken);

            context.Plugins.Execute<IBind>(context, cancellationToken);

            context.Plugins.Execute<IBeforeAction>(context, cancellationToken);

            if (!context.SkipDefaultAction)
            {
                var records = _groups
                    .Skip(PaginationUtils.GetOffset(perPage, page))
                    .Take(perPage)
                    .ToList();

                var result = new PaginatedCollection
                {
                    Records = records.Select(r => (object)r).ToList(),
                    Page = page,
                    PerPage = perPage,
                    TotalCount = _groups.Count
                };
                context.State.Pagination.TotalCount = _groups.Count;
                
                context.Result = result.ToJson(Group.Converter.Settings);
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

            var group = _groups.FirstOrDefault(g => g.Id == id.ToString());
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
        
        public Task CreateAsync(IContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var json = context.GetRequiredValue<string>("Resource");
            var group = Resource.FromJson<Group>(json, out var messages);
            context.Plugins.Execute<IHandleInput>(context, cancellationToken);

            if (messages.Count > 0)
            {
                throw new EntityInvalidException(messages.ToList());
            }
            context.Plugins.Execute<IValidateInput>(context, cancellationToken);

            context.Roles.Add("Group", group);
            context.Plugins.Execute<IDefineRoles>(context, cancellationToken);

            context.Plugins.Execute<IBind>(context, cancellationToken);

            context.Plugins.Execute<IBeforeAction>(context, cancellationToken);

            if (!context.SkipDefaultAction)
            {
                var groupId = Guid.NewGuid();

                context.Roles["Group"].Id = groupId.ToString();
                _groups.Add(context.Roles["Group"]);

                context.Result = groupId;
            }

            context.Plugins.Execute<IAfterAction>(context, cancellationToken);

            context.Plugins.Execute<IReleaseUnmanagedResources>(context, cancellationToken);

            return Task.CompletedTask;
        }

        public Task DeleteAsync(IContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var id = Guid.Parse(context.GetRequiredValue<string>("Id"));
            context.Plugins.Execute<IHandleInput>(context, cancellationToken);

            var group = _groups.FirstOrDefault(g => g.Id == id.ToString());
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
                _groups.Remove(context.Roles["Group"]);
            }

            context.Plugins.Execute<IAfterAction>(context, cancellationToken);

            context.Plugins.Execute<IReleaseUnmanagedResources>(context, cancellationToken);

            return Task.CompletedTask;
        }
    }
}
