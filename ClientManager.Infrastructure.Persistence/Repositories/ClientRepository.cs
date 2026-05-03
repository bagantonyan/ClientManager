using ClientManager.Core.Domain.Entities;
using ClientManager.Core.Domain.Repositories;

namespace ClientManager.Infrastructure.Persistence.Repositories
{
    internal sealed class ClientRepository : RepositoryBase<Client>, IClientRepository
    {
        public ClientRepository(RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
}