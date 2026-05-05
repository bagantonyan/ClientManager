using ClientManager.Core.Domain.Entities;
using ClientManager.Core.Domain.Repositories;

namespace ClientManager.Infrastructure.Persistence.Repositories
{
    internal sealed class FounderRepository : RepositoryBase<Founder>, IFounderRepository
    {
        public FounderRepository(RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }

        public IEnumerable<Founder> GetFounders(Guid clientId, bool trackChanges) =>
            FindByCondition(f => f.ClientFounders!.Any(cf => cf.ClientId.Equals(clientId)), trackChanges)
                .OrderBy(f => f.FullName)
                .ToList();

        public Founder GetFounder(Guid clientId, Guid id, bool trackChanges) =>
            FindByCondition(
                    f => f.Id.Equals(id)
                      && f.ClientFounders!.Any(cf => cf.ClientId.Equals(clientId)),
                    trackChanges)
                .SingleOrDefault()!;

        public void CreateFounderForClient(Client client, Founder founder)
        {
            founder.ClientFounders = new List<ClientFounder>
            {
                new ClientFounder { Client = client }
            };

            Create(founder);
        }
    }
}