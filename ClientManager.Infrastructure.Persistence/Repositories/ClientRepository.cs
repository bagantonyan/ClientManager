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

        public IEnumerable<Client> GetAllClients(bool trackChanges) =>
            FindAll(trackChanges)
                .Include(c => c.ClientFounders!)
                    .ThenInclude(cf => cf.Founder)
                .OrderBy(c => c.Name)
                .ToList();

        public Client GetClient(Guid clientId, bool trackChanges) =>
            FindByCondition(c => c.Id.Equals(clientId), trackChanges)
                .Include(c => c.ClientFounders!)
                    .ThenInclude(cf => cf.Founder)
                .SingleOrDefault()!;
    }
}