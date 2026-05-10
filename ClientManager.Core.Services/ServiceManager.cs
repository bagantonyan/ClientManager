using AutoMapper;
using ClientManager.Core.Domain.Entities;
using ClientManager.Core.Domain.Repositories;
using ClientManager.Core.Services.Abstractions;
using LoggingService;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace ClientManager.Core.Services
{
    public sealed class ServiceManager : IServiceManager
    {
        private readonly Lazy<IClientService> _clientService;
        private readonly Lazy<IFounderService> _founderService;
        private readonly Lazy<IAuthenticationService> _authenticationService;
        public ServiceManager(
            IRepositoryManager repositoryManager, 
            ILoggerManager logger, 
            IMapper mapper,
            UserManager<User> userManager, IConfiguration configuration)
        {
            _clientService = new Lazy<IClientService>(() => new ClientService(repositoryManager, logger, mapper));
            _founderService = new Lazy<IFounderService>(() => new FounderService(repositoryManager, logger, mapper));
            _authenticationService = new Lazy<IAuthenticationService>(() => new AuthenticationService(logger, mapper, userManager, configuration));
        }
        public IClientService ClientService => _clientService.Value;
        public IFounderService FounderService => _founderService.Value;
        public IAuthenticationService AuthenticationService => _authenticationService.Value;
    }
}