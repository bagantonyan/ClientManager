using ClientManager.Core.Domain.Repositories;
using ClientManager.Infrastructure.Persistence.Repositories;

namespace ClientManager.Infrastructure.Persistence
{
    public sealed class RepositoryManager : IRepositoryManager
    {
        private readonly RepositoryContext _repositoryContext;
        private readonly Lazy<IClientRepository> _clientRepository;
        private readonly Lazy<IFounderRepository> _founderRepository;
        public RepositoryManager(RepositoryContext repositoryContext)
        {
            _repositoryContext = repositoryContext;
            _clientRepository = new Lazy<IClientRepository>(() => new ClientRepository(repositoryContext));
            _founderRepository = new Lazy<IFounderRepository>(() => new FounderRepository(repositoryContext));
        }
        public IClientRepository Client => _clientRepository.Value;
        public IFounderRepository Founder => _founderRepository.Value;
        public void Save() => _repositoryContext.SaveChanges();
    }
}