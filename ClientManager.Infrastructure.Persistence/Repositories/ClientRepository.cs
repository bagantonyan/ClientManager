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

        public async Task<IEnumerable<Client>> GetAllClientsAsync(bool trackChanges, bool includeFounders)
        {
            var query = FindAll(trackChanges);

            if (includeFounders)
                query = query
                    .Include(c => c.ClientFounders!)
                        .ThenInclude(cf => cf.Founder);

            return await query
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Client> GetClientAsync(Guid clientId, bool trackChanges, bool includeFounders)
        {
            var query = FindByCondition(c => c.Id.Equals(clientId), trackChanges);

            if (includeFounders)
                query = query
                    .Include(c => c.ClientFounders!)
                        .ThenInclude(cf => cf.Founder);

            return await query.SingleOrDefaultAsync()!;
        }

        public void CreateClient(Client client) => Create(client);

        public async Task<IEnumerable<Client>> GetByIdsAsync(IEnumerable<Guid> ids, bool trackChanges, bool includeFounders)
        {
            var query = FindByCondition(x => ids.Contains(x.Id), trackChanges);

            if (includeFounders)
                query = query
                    .Include(c => c.ClientFounders!)
                        .ThenInclude(cf => cf.Founder);

            return await query.ToListAsync();
        }

        public async Task<Client> GetClientForDeletionAsync(Guid clientId) =>
            await FindByCondition(c => c.Id.Equals(clientId), trackChanges: true)
                .Include(c => c.ClientFounders!)
                    .ThenInclude(cf => cf.Founder)
                        .ThenInclude(f => f!.ClientFounders)
                .SingleOrDefaultAsync()!;

        public void DeleteClient(Client client) => Delete(client);
    }
}