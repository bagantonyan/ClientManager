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

        public async Task<IEnumerable<ClientDto>> GetAllClientsAsync(bool trackChanges, bool includeFounders, CancellationToken ct = default)
        {
            var clients = await _repository.Client.GetAllClientsAsync(trackChanges, includeFounders, ct);

            var clientsDto = _mapper.Map<IEnumerable<ClientDto>>(clients);

            return clientsDto;
        }

        public async Task<ClientDto> GetClientAsync(Guid id, bool trackChanges, bool includeFounders, CancellationToken ct = default)
        {
            var client = await _repository.Client.GetClientAsync(id, trackChanges, includeFounders, ct);

            if (client is null)
                throw new ClientNotFoundException(id);

            var clientDto = _mapper.Map<ClientDto>(client);

            return clientDto;
        }

        public async Task<ClientDto> CreateClientAsync(ClientForCreationDto client, CancellationToken ct = default)
        {
            ValidateFoundersByClientType(client);

            var clientEntity = _mapper.Map<Client>(client);

            _repository.Client.CreateClient(clientEntity);

            await _repository.SaveAsync(ct);

            var clientToReturn = _mapper.Map<ClientDto>(clientEntity);

            return clientToReturn;
        }

        public async Task<IEnumerable<ClientDto>> GetByIdsAsync(IEnumerable<Guid> ids, bool trackChanges, bool includeFounders, CancellationToken ct = default)
        {
            if (ids is null)
                throw new IdParametersBadRequestException();

            var clientEntities = await _repository.Client.GetByIdsAsync(ids, trackChanges, includeFounders, ct);

            if (ids.Count() != clientEntities.Count())
                throw new CollectionByIdsBadRequestException();

            var clientsToReturn = _mapper.Map<IEnumerable<ClientDto>>(clientEntities);

            return clientsToReturn;
        }

        public async Task<(IEnumerable<ClientDto> clients, string ids)> CreateClientCollectionAsync(IEnumerable<ClientForCreationDto> clientCollection, CancellationToken ct = default)
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

            await _repository.SaveAsync(ct);

            var clientCollectionToReturn = _mapper.Map<IEnumerable<ClientDto>>(clientEntities);

            var ids = string.Join(",", clientCollectionToReturn.Select(c => c.Id));

            return (clients: clientCollectionToReturn, ids: ids);
        }

        public async Task DeleteClientAsync(Guid clientId, CancellationToken ct = default)
        {
            var client = await _repository.Client.GetClientForDeletionAsync(clientId, ct);

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

            await _repository.SaveAsync(ct);
        }

        public async Task UpdateClientAsync(Guid clientId, ClientForUpdateDto clientForUpdate, bool trackChanges, CancellationToken ct = default)
        {
            var clientEntity = await _repository.Client.GetClientAsync(clientId, trackChanges, includeFounders: true, ct);

            if (clientEntity is null)
                throw new ClientNotFoundException(clientId);

            _mapper.Map(clientForUpdate, clientEntity);

            await _repository.SaveAsync(ct);
        }

        private void ValidateFoundersByClientType(ClientForCreationDto client)
        {
            var hasFounders = client.Founders is not null && client.Founders.Any();

            if (client.ClientType == ClientType.Legal_Entity && !hasFounders)
                throw new LegalEntityWithoutFoundersException();

            if (client.ClientType != ClientType.Legal_Entity && hasFounders)
                throw new FounderNotAllowedForClientException();
        }
    }
}