using ClientManager.Core.Domain.Entities;
using ClientManager.Core.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace ClientManager.Infrastructure.Persistence.Repositories
{
    internal sealed class ClientRepository : RepositoryBase<Client>, IClientRepository
    {
        public ClientRepository(RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }

        public IEnumerable<Client> GetAllClients(bool trackChanges, bool includeFounders)
        {
            var query = FindAll(trackChanges);

            if (includeFounders)
                query = query
                    .Include(c => c.ClientFounders!)
                        .ThenInclude(cf => cf.Founder);

            return query
                .OrderBy(c => c.Name)
                .ToList();
        }

        public Client GetClient(Guid clientId, bool trackChanges, bool includeFounders)
        {
            var query = FindByCondition(c => c.Id.Equals(clientId), trackChanges);

            if (includeFounders)
                query = query
                    .Include(c => c.ClientFounders!)
                        .ThenInclude(cf => cf.Founder);

            return query.SingleOrDefault()!;
        }

        public void CreateClient(Client client) => Create(client);

        public IEnumerable<Client> GetByIds(IEnumerable<Guid> ids, bool trackChanges, bool includeFounders)
        {
            var query = FindByCondition(x => ids.Contains(x.Id), trackChanges);

            if (includeFounders)
                query = query
                    .Include(c => c.ClientFounders!)
                        .ThenInclude(cf => cf.Founder);

            return query.ToList();
        }

        public Client GetClientForDeletion(Guid clientId) =>
            FindByCondition(c => c.Id.Equals(clientId), trackChanges: true)
                .Include(c => c.ClientFounders!)
                    .ThenInclude(cf => cf.Founder)
                        .ThenInclude(f => f!.ClientFounders)
                .SingleOrDefault()!;

        public void DeleteClient(Client client) => Delete(client);
    }
}