using ClientManager.Core.Domain.Entities;
using ClientManager.Core.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

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
    }
}