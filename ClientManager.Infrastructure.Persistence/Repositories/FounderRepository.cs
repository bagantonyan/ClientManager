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
    }
}