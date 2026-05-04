using ClientManager.Core.Domain.Entities;
using ClientManager.Core.Domain.Repositories;
using ClientManager.Core.Services.Abstractions;
using LoggingService;

namespace ClientManager.Core.Services
{
    internal sealed class ClientService : IClientService
    {
        private readonly IRepositoryManager _repository;
        private readonly ILoggerManager _logger;
        public ClientService(IRepositoryManager repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public IEnumerable<Client> GetAllClients(bool trackChanges)
        {
            try
            {
                var clients = _repository.Client.GetAllClients(trackChanges);
                return clients;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong in the {nameof(GetAllClients)} service method {ex}");
                throw;
            }
        }
    }
}