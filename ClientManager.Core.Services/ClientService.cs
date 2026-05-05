using AutoMapper;
using ClientManager.Core.Domain.Exceptions;
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

        public IEnumerable<ClientDto> GetAllClients(bool trackChanges, bool includeFounders)
        {
            var clients = _repository.Client.GetAllClients(trackChanges, includeFounders);

            var clientsDto = _mapper.Map<IEnumerable<ClientDto>>(clients);

            return clientsDto;
        }

        public ClientDto GetClient(Guid id, bool trackChanges, bool includeFounders)
        {
            var client = _repository.Client.GetClient(id, trackChanges, includeFounders);

            if (client is null)
                throw new ClientNotFoundException(id);

            var clientDto = _mapper.Map<ClientDto>(client);

            return clientDto;
        }
    }
}