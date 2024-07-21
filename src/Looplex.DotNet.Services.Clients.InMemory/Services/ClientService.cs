using Looplex.DotNet.Core.Application.ExtensionMethods;
using Looplex.DotNet.Core.Common.Exceptions;
using Looplex.DotNet.Core.Common.Utils;
using Looplex.DotNet.Core.Domain;
using Looplex.DotNet.Middlewares.Clients.Application.Abstractions.Services;
using Looplex.DotNet.Middlewares.Clients.Domain.Entities.Clients;
using Looplex.DotNet.Middlewares.ScimV2.Domain.Entities;
using Looplex.OpenForExtension.Abstractions.Commands;
using Looplex.OpenForExtension.Abstractions.Contexts;
using Looplex.OpenForExtension.Abstractions.ExtensionMethods;

namespace Looplex.DotNet.Services.Clients.InMemory.Services
{
    public class ClientService() : IClientService
    {
        private static readonly IList<Client> _clients = [];
        
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
                var records = _clients
                    .Skip(PaginationUtils.GetOffset(perPage, page))
                    .Take(perPage)
                    .ToList();

                var result = new PaginatedCollection
                {
                    Records = records.Select(r => (object)r).ToList(),
                    Page = page,
                    PerPage = perPage,
                    TotalCount = _clients.Count
                };
                context.State.Pagination.TotalCount = _clients.Count;
                
                context.Result = result.ToJson(Client.Converter.Settings);
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

            var client = _clients.FirstOrDefault(c => c.Id == id.ToString());
            if (client == null)
            {
                throw new EntityNotFoundException(nameof(Client), id.ToString());
            }
            context.Plugins.Execute<IValidateInput>(context, cancellationToken);

            context.Roles.Add("Client", client);
            context.Plugins.Execute<IDefineRoles>(context, cancellationToken);

            context.Plugins.Execute<IBind>(context, cancellationToken);

            context.Plugins.Execute<IBeforeAction>(context, cancellationToken);

            if (!context.SkipDefaultAction)
            {
                context.Result = ((Client)context.Roles["Client"]).ToJson();
            }

            context.Plugins.Execute<IAfterAction>(context, cancellationToken);

            context.Plugins.Execute<IReleaseUnmanagedResources>(context, cancellationToken);

            return Task.CompletedTask;
        }

        public Task CreateAsync(IContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var json = context.GetRequiredValue<string>("Resource");
            var client = Resource.FromJson<Client>(json, out var messages);
            context.Plugins.Execute<IHandleInput>(context, cancellationToken);

            if (messages.Count > 0)
            {
                throw new EntityInvalidException(messages.ToList());
            }
            context.Plugins.Execute<IValidateInput>(context, cancellationToken);

            context.Roles.Add("Client", client);
            context.Plugins.Execute<IDefineRoles>(context, cancellationToken);

            context.Plugins.Execute<IBind>(context, cancellationToken);

            context.Plugins.Execute<IBeforeAction>(context, cancellationToken);

            if (!context.SkipDefaultAction)
            {
                _clients.Add(context.Roles["Client"]);

                context.Result = context.Roles["Client"].Id;
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

            var client = _clients.FirstOrDefault(c => c.Id == id.ToString());
            if (client == null)
            {
                throw new EntityNotFoundException(nameof(Client), id.ToString());
            }
            context.Plugins.Execute<IValidateInput>(context, cancellationToken);

            context.Roles.Add("Client", client);
            context.Plugins.Execute<IDefineRoles>(context, cancellationToken);

            context.Plugins.Execute<IBind>(context, cancellationToken);

            context.Plugins.Execute<IBeforeAction>(context, cancellationToken);

            if (!context.SkipDefaultAction)
            {
                _clients.Remove(context.Roles["Client"]);
            }

            context.Plugins.Execute<IAfterAction>(context, cancellationToken);

            context.Plugins.Execute<IReleaseUnmanagedResources>(context, cancellationToken);

            return Task.CompletedTask;
        }

        public Task GetByIdAndSecretOrDefaultAsync(IContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            Guid id = context.State.ClientId;
            string secret = context.State.ClientSecret;
            context.Plugins.Execute<IHandleInput>(context, cancellationToken);

            var client = _clients.FirstOrDefault(c => Guid.Parse(c.Id) == id && c.Secret == secret);
            context.Plugins.Execute<IValidateInput>(context, cancellationToken);

            if (client != null)
            {
                context.Roles.Add("Client", client);
            }
            context.Plugins.Execute<IDefineRoles>(context, cancellationToken);

            context.Plugins.Execute<IBind>(context, cancellationToken);

            context.Plugins.Execute<IBeforeAction>(context, cancellationToken);

            if (!context.SkipDefaultAction)
            {
                if (context.Roles.TryGetValue("Client", out var role))
                {
                    context.Result = role;
                }
            }

            context.Plugins.Execute<IAfterAction>(context, cancellationToken);

            context.Plugins.Execute<IReleaseUnmanagedResources>(context, cancellationToken);
            
            return Task.CompletedTask;
        }
    }
}
