using Looplex.DotNet.Core.Application.ExtensionMethods;
using Looplex.DotNet.Core.Common.Exceptions;
using Looplex.DotNet.Core.Common.Utils;
using Looplex.DotNet.Core.Domain;
using Looplex.DotNet.Middlewares.ScimV2.Entities.Users;
using Looplex.DotNet.Middlewares.ScimV2.Services;
using Looplex.OpenForExtension.Commands;
using Looplex.OpenForExtension.Context;
using Looplex.OpenForExtension.ExtensionMethods;
using MassTransit;

namespace Looplex.DotNet.Services.ScimV2.InMemory.Services
{
    public class UserService(IBus bus) : IUserService
    {
        private static readonly IList<User> _users = [];

        public Task GetAllAsync(IDefaultContext context)
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

                var result = new PaginatedCollection<User>
                {
                    Records = records,
                    Page = page,
                    PerPage = perPage,
                    TotalCount = _users.Count
                };

                context.Result = result;
            }

            context.Plugins.Execute<IAfterAction>(context);

            context.Plugins.Execute<IReleaseUnmanagedResources>(context);

            return Task.CompletedTask;
        }

        public Task GetByIdAsync(IDefaultContext context)
        {
            var id = context.GetRequiredValue<Guid>("Id");
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
                context.Result = context.Actors["User"];
            }

            context.Plugins.Execute<IAfterAction>(context);

            context.Plugins.Execute<IReleaseUnmanagedResources>(context);

            return Task.CompletedTask;
        }
        
        public Task CreateAsync(IDefaultContext context)
        {
            var user = context.GetRequiredValue<User>("Resource");
            context.Plugins.Execute<IHandleInput>(context);

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

        public Task DeleteAsync(IDefaultContext context)
        {
            var id = context.GetRequiredValue<Guid>("Id");
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
