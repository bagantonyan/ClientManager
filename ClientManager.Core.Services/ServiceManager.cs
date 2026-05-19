using AutoMapper;
using ClientManager.Core.Domain.Repositories;
using ClientManager.Core.Services.Abstractions;
using LoggingService;

namespace ClientManager.Core.Services
{
    public sealed class ServiceManager : IServiceManager
    {
        private readonly Lazy<IClientService> _clientService;
        private readonly Lazy<IFounderService> _founderService;

        public ServiceManager(
            IRepositoryManager repositoryManager,
            ILoggerManager logger,
            IMapper mapper)
        {
            _clientService = new Lazy<IClientService>(() => new ClientService(repositoryManager, logger, mapper));
            _founderService = new Lazy<IFounderService>(() => new FounderService(repositoryManager, logger, mapper));
        }

        public IClientService ClientService => _clientService.Value;
        public IFounderService FounderService => _founderService.Value;
    }
}
