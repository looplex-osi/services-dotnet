using AutoMapper;
using Looplex.DotNet.Core.Application.Abstractions.Dtos;
using Looplex.DotNet.Core.Application.ExtensionMethods;
using Looplex.DotNet.Core.Common.Exceptions;
using Looplex.DotNet.Core.Common.Utils;
using Looplex.DotNet.Core.Domain;
using Looplex.DotNet.Middlewares.ScimV2.Dtos.Groups;
using Looplex.DotNet.Middlewares.ScimV2.Entities;
using Looplex.DotNet.Middlewares.ScimV2.Entities.Groups;
using Looplex.DotNet.Middlewares.ScimV2.Services;
using Looplex.OpenForExtension.Commands;
using Looplex.OpenForExtension.Context;
using Looplex.OpenForExtension.ExtensionMethods;

namespace Looplex.DotNet.Services.ScimV2.InMemory.Services
{
    public class GroupService(IMapper mapper) : IGroupService
    {
        private static readonly IList<Group> _groups = []; 
        
        private readonly IMapper _mapper = mapper;
        
        public Task GetAllAsync(IDefaultContext context)
        {
            context.Plugins.Execute<IHandleInput>(context);
            var page = context.GetRequiredValue<int>("Pagination.Page");
            var perPage = context.GetRequiredValue<int>("Pagination.PerPage");

            context.Plugins.Execute<IValidateInput>(context);

            context.Plugins.Execute<IDefineActors>(context);

            context.Plugins.Execute<IBind>(context);

            context.Plugins.Execute<IBeforeAction>(context);

            if (!context.SkipDefaultAction)
            {
                var records = _groups
                    .Skip(PaginationUtils.GetOffset(perPage, page))
                    .Take(perPage)
                    .ToList();

                var result = new PaginatedCollection<Group>
                {
                    Records = records,
                    Page = page,
                    PerPage = perPage,
                    TotalCount = _groups.Count
                };

                context.Result = _mapper.Map<PaginatedCollection<Group>, PaginatedCollectionDto<GroupReadDto>>(result);
            }

            context.Plugins.Execute<IAfterAction>(context);

            context.Plugins.Execute<IReleaseUnmanagedResources>(context);

            return Task.CompletedTask;
        }

        public Task GetByIdAsync(IDefaultContext context)
        {
            var id = Guid.Parse(context.GetRequiredValue<string>("Id"));
            context.Plugins.Execute<IHandleInput>(context);

            var group = _groups.FirstOrDefault(g => g.Id == id.ToString());
            if (group == null)
            {
                throw new EntityNotFoundException(nameof(Group), id.ToString());
            }
            context.Plugins.Execute<IValidateInput>(context);
            
            context.Actors.Add("Group", group);
            context.Plugins.Execute<IDefineActors>(context);

            context.Plugins.Execute<IBind>(context);

            context.Plugins.Execute<IBeforeAction>(context);

            if (!context.SkipDefaultAction)
            {
                context.Result = _mapper.Map<Group, GroupReadDto>(context.Actors["Group"]);
            }

            context.Plugins.Execute<IAfterAction>(context);

            context.Plugins.Execute<IReleaseUnmanagedResources>(context);

            return Task.CompletedTask;
        }
        
        public Task CreateAsync(IDefaultContext context)
        {
            var json = context.GetRequiredValue<string>("Resource");
            var group = Resource.FromJson<Group>(json, out var messages);
            context.Plugins.Execute<IHandleInput>(context);

            if (messages.Count > 0)
            {
                throw new EntityInvalidExcepion(messages.ToList());
            }
            context.Plugins.Execute<IValidateInput>(context);

            context.Actors.Add("Group", group);
            context.Plugins.Execute<IDefineActors>(context);

            context.Plugins.Execute<IBind>(context);

            context.Plugins.Execute<IBeforeAction>(context);

            if (!context.SkipDefaultAction)
            {
                var groupId = Guid.NewGuid();

                context.Actors["Group"].Id = groupId.ToString();
                _groups.Add(context.Actors["Group"]);

                context.Result = groupId;
            }

            context.Plugins.Execute<IAfterAction>(context);

            context.Plugins.Execute<IReleaseUnmanagedResources>(context);

            return Task.CompletedTask;
        }

        public Task DeleteAsync(IDefaultContext context)
        {
            var id = Guid.Parse(context.GetRequiredValue<string>("Id"));
            context.Plugins.Execute<IHandleInput>(context);

            var group = _groups.FirstOrDefault(g => g.Id == id.ToString());
            if (group == null)
            {
                throw new EntityNotFoundException(nameof(Group), id.ToString());
            }
            context.Plugins.Execute<IValidateInput>(context);

            context.Actors.Add("Group", group);
            context.Plugins.Execute<IDefineActors>(context);

            context.Plugins.Execute<IBind>(context);

            context.Plugins.Execute<IBeforeAction>(context);

            if (!context.SkipDefaultAction)
            {
                _groups.Remove(context.Actors["Group"]);
            }

            context.Plugins.Execute<IAfterAction>(context);

            context.Plugins.Execute<IReleaseUnmanagedResources>(context);

            return Task.CompletedTask;
        }
    }
}
