using Looplex.DotNet.Core.Application.ExtensionMethods;
using Looplex.DotNet.Core.Common.Exceptions;
using Looplex.DotNet.Core.Common.Utils;
using Looplex.DotNet.Core.Domain;
using Looplex.DotNet.Middlewares.ScimV2.Application.Abstractions.Services;
using Looplex.DotNet.Middlewares.ScimV2.Domain.Entities;
using Looplex.DotNet.Middlewares.ScimV2.Domain.Entities.Users;
using Looplex.OpenForExtension.Commands;
using Looplex.OpenForExtension.Context;
using Looplex.OpenForExtension.ExtensionMethods;

namespace Looplex.DotNet.Services.ScimV2.InMemory.Services
{
    public class UserService() : IUserService
    {
        private static readonly IList<User> _users = [];

        public Task GetAllAsync(IDefaultContext context, CancellationToken cancellationToken)
        {
            var page = context.GetRequiredValue<int>("Pagination.Page");
            var perPage = context.GetRequiredValue<int>("Pagination.PerPage");
            context.Plugins.Execute<IHandleInput>(context);

            context.Plugins.Execute<IValidateInput>(context);

            context.Plugins.Execute<IDefineActors>(context);

            context.Plugins.Execute<IBind>(context);

            context.Plugins.Execute<IBeforeAction>(context);

            if (!context.SkipDefaultAction)
            {
                var records = _users
                    .Skip(PaginationUtils.GetOffset(perPage, page))
                    .Take(perPage)
                    .ToList();

                var result = new PaginatedCollection
                {
                    Records = records.Select(r => (object)r).ToList(),
                    Page = page,
                    PerPage = perPage,
                    TotalCount = _users.Count
                };
                context.State.Pagination.TotalCount = _users.Count;
                
                context.Result = result.ToJson(User.Converter.Settings);
            }

            context.Plugins.Execute<IAfterAction>(context);

            context.Plugins.Execute<IReleaseUnmanagedResources>(context);

            return Task.CompletedTask;
        }

        public Task GetByIdAsync(IDefaultContext context, CancellationToken cancellationToken)
        {
            var id = Guid.Parse(context.GetRequiredValue<string>("Id"));
            context.Plugins.Execute<IHandleInput>(context);

            var user = _users.FirstOrDefault(u => u.Id == id.ToString());
            if (user == null)
            {
                throw new EntityNotFoundException(nameof(User), id.ToString());
            }
            context.Plugins.Execute<IValidateInput>(context);

            context.Actors["User"] = user;
            context.Plugins.Execute<IDefineActors>(context);

            context.Plugins.Execute<IBind>(context);

            context.Plugins.Execute<IBeforeAction>(context);

            if (!context.SkipDefaultAction)
            {
                context.Result = ((User)context.Actors["User"]).ToJson();
            }

            context.Plugins.Execute<IAfterAction>(context);

            context.Plugins.Execute<IReleaseUnmanagedResources>(context);

            return Task.CompletedTask;
        }
        
        public Task CreateAsync(IDefaultContext context, CancellationToken cancellationToken)
        {
            var json = context.GetRequiredValue<string>("Resource");
            var user = Resource.FromJson<User>(json, out var messages);
            context.Plugins.Execute<IHandleInput>(context);

            if (messages.Count > 0)
            {
                throw new EntityInvalidException(messages.ToList());
            }
            context.Plugins.Execute<IValidateInput>(context);

            context.Actors["User"] = user;
            context.Plugins.Execute<IDefineActors>(context);

            context.Plugins.Execute<IBind>(context);

            context.Plugins.Execute<IBeforeAction>(context);

            if (!context.SkipDefaultAction)
            {
                var userId = Guid.NewGuid();

                context.Actors["User"].Id = userId.ToString();
                _users.Add(context.Actors["User"]);

                context.Result = userId;
            }

            context.Plugins.Execute<IAfterAction>(context);

            context.Plugins.Execute<IReleaseUnmanagedResources>(context);

            return Task.CompletedTask;
        }

        public Task DeleteAsync(IDefaultContext context, CancellationToken cancellationToken)
        {
            var id = Guid.Parse(context.GetRequiredValue<string>("Id"));
            context.Plugins.Execute<IHandleInput>(context);

            var user = _users.FirstOrDefault(u => u.Id == id.ToString());
            if (user == null)
            {
                throw new EntityNotFoundException(nameof(User), id.ToString());
            }
            context.Plugins.Execute<IValidateInput>(context);

            context.Actors["User"] = user;
            context.Plugins.Execute<IDefineActors>(context);

            context.Plugins.Execute<IBind>(context);

            context.Plugins.Execute<IBeforeAction>(context);

            if (!context.SkipDefaultAction)
            {
                _users.Remove(context.Actors["User"]);
            }

            context.Plugins.Execute<IAfterAction>(context);

            context.Plugins.Execute<IReleaseUnmanagedResources>(context);

            return Task.CompletedTask;
        }
    }
}
