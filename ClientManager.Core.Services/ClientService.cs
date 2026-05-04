using AutoMapper;
using ClientManager.Core.Domain.Repositories;
using ClientManager.Core.Services.Abstractions;
using LoggingService;
using Shared.DataTransferObjects.Clients;

namespace ClientManager.Core.Services
{
    internal sealed class ClientService : IClientService
    {
        private readonly IRepositoryManager _repository;
        private readonly ILoggerManager _logger;
        private readonly IMapper _mapper;
        public ClientService(
            IRepositoryManager repository, 
            ILoggerManager logger,
            IMapper mapper)
        {
            _repository = repository;
            _logger = logger;
            _mapper = mapper;
        }

        public IEnumerable<ClientDto> GetAllClients(bool trackChanges)
        {
            try
            {
                var clients = _repository.Client.GetAllClients(trackChanges);

                var clientsDto = _mapper.Map<IEnumerable<ClientDto>>(clients);

                return clientsDto;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong in the {nameof(GetAllClients)} service method {ex}");
                throw;
            }
        }
    }
}