using ClientManager.Core.Domain.Entities;
using ClientManager.Core.Domain.Repositories;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

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
                .OrderBy(c => c.Name)
                .ToList();

        public Client GetClient(Guid clientId, bool trackChanges) =>
            FindByCondition(c => c.Id.Equals(clientId), trackChanges)
                .SingleOrDefault()!;
    }
}