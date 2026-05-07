using ClientManager.Core.Domain.Entities;
using ClientManager.Core.Domain.Repositories;
using ClientManager.Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;
using Shared.RequestFeatures;

namespace ClientManager.Infrastructure.Persistence.Repositories
{
    internal sealed class ClientRepository : RepositoryBase<Client>, IClientRepository
    {
        public ClientRepository(RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }

        public async Task<PagedList<Client>> GetAllClientsAsync(ClientParameters clientParameters, bool trackChanges, bool includeFounders, CancellationToken ct = default)
        {
            var filtered = FindAll(trackChanges).Search(clientParameters.SearchTerm!);

            var count = await filtered.CountAsync(ct);

            var pageQuery = filtered;
            if (includeFounders)
                pageQuery = pageQuery
                    .Include(c => c.ClientFounders!)
                        .ThenInclude(cf => cf.Founder);

            var clients = await pageQuery
                .Sort(clientParameters.OrderBy!)
                .Skip((clientParameters.PageNumber - 1) * clientParameters.PageSize)
                .Take(clientParameters.PageSize)
                .ToListAsync(ct);

            return PagedList<Client>
                .ToPagedList(clients, count, clientParameters.PageNumber, clientParameters.PageSize);
        }

        public async Task<Client> GetClientAsync(Guid clientId, bool trackChanges, bool includeFounders, CancellationToken ct = default)
        {
            var query = FindByCondition(c => c.Id.Equals(clientId), trackChanges);

            if (includeFounders)
                query = query
                    .Include(c => c.ClientFounders!)
                        .ThenInclude(cf => cf.Founder);

            return await query.SingleOrDefaultAsync(ct)!;
        }

        public void CreateClient(Client client) => Create(client);

        public async Task<IEnumerable<Client>> GetByIdsAsync(IEnumerable<Guid> ids, bool trackChanges, bool includeFounders, CancellationToken ct = default)
        {
            var query = FindByCondition(x => ids.Contains(x.Id), trackChanges);

            if (includeFounders)
                query = query
                    .Include(c => c.ClientFounders!)
                        .ThenInclude(cf => cf.Founder);

            return await query.ToListAsync(ct);
        }

        public async Task<Client> GetClientForDeletionAsync(Guid clientId, CancellationToken ct = default) =>
            await FindByCondition(c => c.Id.Equals(clientId), trackChanges: true)
                .Include(c => c.ClientFounders!)
                    .ThenInclude(cf => cf.Founder)
                        .ThenInclude(f => f!.ClientFounders)
                .SingleOrDefaultAsync(ct)!;

        public async Task<Client?> GetByInnIncludingDeletedAsync(string inn, bool trackChanges, CancellationToken ct = default) =>
            await FindByCondition(c => c.INN == inn, trackChanges)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(ct);

        public void DeleteClient(Client client) => Delete(client);
    }
}