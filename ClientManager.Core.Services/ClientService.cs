using AutoMapper;
using ClientManager.Core.Domain.Entities;
using ClientManager.Core.Domain.Exceptions;
using ClientManager.Core.Domain.Repositories;
using ClientManager.Core.Services.Abstractions;
using LoggingService;
using Shared.DataTransferObjects.Clients;
using Shared.Enums;

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

        public ClientDto CreateClient(ClientForCreationDto client)
        {
            ValidateFoundersByClientType(client);

            var clientEntity = _mapper.Map<Client>(client);

            _repository.Client.CreateClient(clientEntity);

            _repository.Save();

            var clientToReturn = _mapper.Map<ClientDto>(clientEntity);

            return clientToReturn;
        }

        public IEnumerable<ClientDto> GetByIds(IEnumerable<Guid> ids, bool trackChanges, bool includeFounders)
        {
            if (ids is null)
                throw new IdParametersBadRequestException();

            var clientEntities = _repository.Client.GetByIds(ids, trackChanges, includeFounders);

            if (ids.Count() != clientEntities.Count())
                throw new CollectionByIdsBadRequestException();

            var clientsToReturn = _mapper.Map<IEnumerable<ClientDto>>(clientEntities);

            return clientsToReturn;
        }

        public (IEnumerable<ClientDto> clients, string ids) CreateClientCollection(IEnumerable<ClientForCreationDto> clientCollection)
        {
            if (clientCollection is null)
                throw new ClientCollectionBadRequest();

            foreach (var client in clientCollection)
                ValidateFoundersByClientType(client);

            var clientEntities = _mapper.Map<IEnumerable<Client>>(clientCollection);

            foreach (var client in clientEntities)
            {
                _repository.Client.CreateClient(client);
            }

            _repository.Save();

            var clientCollectionToReturn = _mapper.Map<IEnumerable<ClientDto>>(clientEntities);

            var ids = string.Join(",", clientCollectionToReturn.Select(c => c.Id));

            return (clients: clientCollectionToReturn, ids: ids);
        }

        private static void ValidateFoundersByClientType(ClientForCreationDto client)
        {
            var hasFounders = client.Founders is not null && client.Founders.Any();

            if (client.ClientType == ClientType.Legal_Entity && !hasFounders)
                throw new LegalEntityWithoutFoundersException();

            if (client.ClientType != ClientType.Legal_Entity && hasFounders)
                throw new FounderNotAllowedForClientException();
        }

        public void DeleteClient(Guid clientId)
        {
            var client = _repository.Client.GetClientForDeletion(clientId);

            if (client is null)
                throw new ClientNotFoundException(clientId);

            if (client.ClientFounders is not null)
            {
                foreach (var link in client.ClientFounders.ToList())
                {
                    var founder = link.Founder;

                    client.ClientFounders.Remove(link);

                    if (founder is not null
                        && !founder.ClientFounders!.Any(cf => cf.ClientId != clientId))
                    {
                        _repository.Founder.DeleteFounder(founder);
                    }
                }
            }

            _repository.Client.DeleteClient(client);

            _repository.Save();
        }
    }
}