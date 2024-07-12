using Looplex.DotNet.Core.Application.ExtensionMethods;
using Looplex.DotNet.Core.Common.Exceptions;
using Looplex.DotNet.Core.Common.Utils;
using Looplex.DotNet.Core.Domain;
using Looplex.DotNet.Middlewares.Clients.Entities;
using Looplex.OpenForExtension.Commands;
using Looplex.OpenForExtension.Context;
using Looplex.OpenForExtension.ExtensionMethods;

namespace Looplex.DotNet.Middlewares.OAuth2.Services
{
    public class ClientService : IClientService
    {
        private static readonly IList<Client> _clients = [];

        public Task GetAllAsync(IDefaultContext context)
        {
            var page = context.GetRequiredValue<int>("Pagination.Page");
            var perPage = context.GetRequiredValue<int>("Pagination.PerPage");
            context.Plugins.Execute<IHandleInput>(context);

            if (page < 1)
            {
                page = 1;
            }
            context.Plugins.Execute<IValidateInput>(context);

            context.Plugins.Execute<IDefineActors>(context);

            context.Plugins.Execute<IBind>(context);

            context.Plugins.Execute<IBeforeAction>(context);

            if (!context.SkipDefaultAction)
            {
                var records = _clients
                    .Skip(PaginationUtils.GetOffset(perPage, page))
                    .Take(perPage)
                    .ToList();

                var result = new PaginatedCollection<Client>
                {
                    Records = records,
                    Page = page,
                    PerPage = perPage,
                    TotalCount = _clients.Count
                };

                context.Result = result;
            }

            context.Plugins.Execute<IAfterAction>(context);

            context.Plugins.Execute<IReleaseUnmanagedResources>(context);
                        
            return Task.CompletedTask;
        }

        public Task GetByIdAsync(IDefaultContext context)
        {
            var id = Guid.Parse(context.GetRequiredValue<string>("Id"));
            context.Plugins.Execute<IHandleInput>(context);

            var client = _clients.FirstOrDefault(c => c.Id == id.ToString());
            if (client == null)
            {
                throw new EntityNotFoundException(nameof(Client), id.ToString());
            }
            context.Plugins.Execute<IValidateInput>(context);

            context.Actors.Add("Client", client);
            context.Plugins.Execute<IDefineActors>(context);

            context.Plugins.Execute<IBind>(context);

            context.Plugins.Execute<IBeforeAction>(context);

            if (!context.SkipDefaultAction)
            {
                context.Result = context.Actors["Client"];
            }

            context.Plugins.Execute<IAfterAction>(context);

            context.Plugins.Execute<IReleaseUnmanagedResources>(context);

            return Task.CompletedTask;
        }

        public Task CreateAsync(IDefaultContext context)
        {
            var client = context.GetRequiredValue<Client>("Resource");
            context.Plugins.Execute<IHandleInput>(context);

            context.Plugins.Execute<IValidateInput>(context);

            context.Actors.Add("Client", client);
            context.Plugins.Execute<IDefineActors>(context);

            context.Plugins.Execute<IBind>(context);

            context.Plugins.Execute<IBeforeAction>(context);

            if (!context.SkipDefaultAction)
            {
                var clientId = Guid.NewGuid();

                context.Actors["Client"].Id = clientId.ToString();
                _clients.Add(context.Actors["Client"]);

                context.Result = clientId;
            }

            context.Plugins.Execute<IAfterAction>(context);

            context.Plugins.Execute<IReleaseUnmanagedResources>(context);

            return Task.CompletedTask;
        }

        public Task DeleteAsync(IDefaultContext context)
        {
            var id = Guid.Parse(context.GetRequiredValue<string>("Id"));
            context.Plugins.Execute<IHandleInput>(context);

            var client = _clients.FirstOrDefault(c => c.Id == id.ToString());
            if (client == null)
            {
                throw new EntityNotFoundException(nameof(Client), id.ToString());
            }

            context.Plugins.Execute<IValidateInput>(context);

            context.Actors.Add("Client", client);
            context.Plugins.Execute<IDefineActors>(context);

            context.Plugins.Execute<IBind>(context);

            context.Plugins.Execute<IBeforeAction>(context);

            if (!context.SkipDefaultAction)
            {
                _clients.Remove(context.Actors["Client"]);
            }

            context.Plugins.Execute<IAfterAction>(context);

            context.Plugins.Execute<IReleaseUnmanagedResources>(context);

            return Task.CompletedTask;
        }

        public Task GetByIdAndSecretOrDefaultAsync(IDefaultContext context)
        {
            Guid id = context.State.ClientId;
            string secret = context.State.ClientSecret;
            context.Plugins.Execute<IHandleInput>(context);

            var client = _clients.FirstOrDefault(c => Guid.Parse(c.Id) == id && c.Secret == secret);
            context.Plugins.Execute<IValidateInput>(context);

            context.Actors.Add("Client", client);
            context.Plugins.Execute<IDefineActors>(context);

            context.Plugins.Execute<IBind>(context);

            context.Plugins.Execute<IBeforeAction>(context);

            if (!context.SkipDefaultAction)
            {
                context.Result = context.Actors["Client"];
            }

            context.Plugins.Execute<IAfterAction>(context);

            context.Plugins.Execute<IReleaseUnmanagedResources>(context);
            
            return Task.CompletedTask;
        }
    }
}
