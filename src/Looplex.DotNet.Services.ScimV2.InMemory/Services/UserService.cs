using Looplex.DotNet.Core.Application.ExtensionMethods;
using Looplex.DotNet.Core.Common.Exceptions;
using Looplex.DotNet.Core.Common.Utils;
using Looplex.DotNet.Core.Domain;
using Looplex.DotNet.Middlewares.ScimV2.Application.Abstractions.Services;
using Looplex.DotNet.Middlewares.ScimV2.Domain.Entities;
using Looplex.DotNet.Middlewares.ScimV2.Domain.Entities.Users;
using Looplex.OpenForExtension.Abstractions.Commands;
using Looplex.OpenForExtension.Abstractions.Contexts;
using Looplex.OpenForExtension.Abstractions.ExtensionMethods;

namespace Looplex.DotNet.Services.ScimV2.InMemory.Services
{
    public class UserService() : IUserService
    {
        private static readonly IList<User> _users = [];

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

            context.Plugins.Execute<IAfterAction>(context, cancellationToken);

            context.Plugins.Execute<IReleaseUnmanagedResources>(context, cancellationToken);

            return Task.CompletedTask;
        }

        public Task GetByIdAsync(IContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var id = Guid.Parse(context.GetRequiredValue<string>("Id"));
            context.Plugins.Execute<IHandleInput>(context, cancellationToken);

            var user = _users.FirstOrDefault(u => u.Id == id.ToString());
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
        
        public Task CreateAsync(IContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var json = context.GetRequiredValue<string>("Resource");
            var user = Resource.FromJson<User>(json, out var messages);
            context.Plugins.Execute<IHandleInput>(context, cancellationToken);

            if (messages.Count > 0)
            {
                throw new EntityInvalidException(messages.ToList());
            }
            context.Plugins.Execute<IValidateInput>(context, cancellationToken);

            context.Roles["User"] = user;
            context.Plugins.Execute<IDefineRoles>(context, cancellationToken);

            context.Plugins.Execute<IBind>(context, cancellationToken);

            context.Plugins.Execute<IBeforeAction>(context, cancellationToken);

            if (!context.SkipDefaultAction)
            {
                var userId = Guid.NewGuid();

                context.Roles["User"].Id = userId.ToString();
                _users.Add(context.Roles["User"]);

                context.Result = userId;
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

            var user = _users.FirstOrDefault(u => u.Id == id.ToString());
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
                _users.Remove(context.Roles["User"]);
            }

            context.Plugins.Execute<IAfterAction>(context, cancellationToken);

            context.Plugins.Execute<IReleaseUnmanagedResources>(context, cancellationToken);

            return Task.CompletedTask;
        }
    }
}
