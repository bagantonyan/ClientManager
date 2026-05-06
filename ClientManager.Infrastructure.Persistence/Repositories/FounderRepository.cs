using ClientManager.Core.Domain.Entities;
using ClientManager.Core.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ClientManager.Infrastructure.Persistence.Repositories
{
    internal sealed class FounderRepository : RepositoryBase<Founder>, IFounderRepository
    {
        public FounderRepository(RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }

        public async Task<IEnumerable<Founder>> GetFoundersAsync(Guid clientId, bool trackChanges, CancellationToken ct = default) =>
            await FindByCondition(f => f.ClientFounders!.Any(cf => cf.ClientId.Equals(clientId)), trackChanges)
                .OrderBy(f => f.FullName)
                .ToListAsync(ct);

        public async Task<Founder> GetFounderAsync(Guid clientId, Guid id, bool trackChanges, CancellationToken ct = default) =>
            await FindByCondition(
                    f => f.Id.Equals(id)
                      && f.ClientFounders!.Any(cf => cf.ClientId.Equals(clientId)),
                    trackChanges)
                .SingleOrDefaultAsync(ct)!;

        public async Task<Founder> GetFounderWithLinksAsync(Guid clientId, Guid id, bool trackChanges, CancellationToken ct = default) =>
            await FindByCondition(
                    f => f.Id.Equals(id)
                      && f.ClientFounders!.Any(cf => cf.ClientId.Equals(clientId)),
                    trackChanges)
                .Include(f => f.ClientFounders)
                .SingleOrDefaultAsync(ct)!;

        public async Task<Founder?> GetByInnIncludingDeletedAsync(string inn, bool trackChanges, CancellationToken ct = default) =>
            await FindByCondition(f => f.INN == inn, trackChanges)
                .IgnoreQueryFilters()
                .Include(f => f.ClientFounders)
                .FirstOrDefaultAsync(ct);

        public void CreateFounderForClient(Client client, Founder founder)
        {
            founder.ClientFounders = new List<ClientFounder>
            {
                new ClientFounder { Client = client }
            };

            Create(founder);
        }

        public void DeleteFounder(Founder founder) => Delete(founder);
    }
}