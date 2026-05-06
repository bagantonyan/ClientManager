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
            var clientEntity = await GetAndCheckIfClientExistsAsync(id, trackChanges, includeFounders, ct);

            var clientDto = _mapper.Map<ClientDto>(clientEntity);

            return clientDto;
        }

        public async Task<ClientDto> CreateClientAsync(ClientForCreationDto client, CancellationToken ct = default)
        {
            ValidateFoundersByClientType(client);

            var existing = await _repository.Client.GetByInnIncludingDeletedAsync(client.INN!, trackChanges: true, ct);

            Client clientEntity;
            if (existing is not null)
            {
                if (existing.DeletedDate is null)
                    throw new ClientWithSameInnExistsException(client.INN!);

                // Restore the soft-deleted client
                existing.DeletedDate = null;
                existing.Name = client.Name;
                existing.ClientType = client.ClientType;
                clientEntity = existing;
            }
            else
            {
                clientEntity = new Client
                {
                    INN = client.INN,
                    Name = client.Name,
                    ClientType = client.ClientType
                };
                _repository.Client.CreateClient(clientEntity);
            }

            // Resolve each founder by INN: existing (active or restored) → reuse; otherwise → create.
            // This handles the unique-INN constraint correctly when a person is a founder of multiple clients.
            if (client.Founders is not null && client.Founders.Any())
            {
                var resolved = new List<ClientFounder>();
                foreach (var founderDto in client.Founders)
                {
                    var founder = await ResolveFounderAsync(founderDto, ct);
                    resolved.Add(new ClientFounder { Founder = founder });
                }
                clientEntity.ClientFounders = resolved;
            }

            await _repository.SaveAsync(ct);

            var clientToReturn = _mapper.Map<ClientDto>(clientEntity);

            return clientToReturn;
        }

        private async Task<Founder> ResolveFounderAsync(Shared.DataTransferObjects.Founders.FounderForCreationDto dto, CancellationToken ct)
        {
            var existing = await _repository.Founder.GetByInnIncludingDeletedAsync(dto.INN!, trackChanges: true, ct);
            if (existing is not null)
            {
                if (existing.DeletedDate is not null)
                {
                    existing.DeletedDate = null;
                    existing.FullName = dto.FullName;
                }
                return existing;
            }
            return _mapper.Map<Founder>(dto);
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

        public async Task<(ClientForUpdateDto clientToPatch, Client clientEntity)> GetClientForPatchAsync(Guid clientId, bool trackChanges, CancellationToken ct = default)
        {
            var clientEntity = await GetAndCheckIfClientExistsAsync(clientId, trackChanges, includeFounders: false, ct);

            var clientToPatch = _mapper.Map<ClientForUpdateDto>(clientEntity);

            return (clientToPatch, clientEntity);
        }

        public async Task SaveChangesForPatchAsync(ClientForUpdateDto clientToPatch, Client clientEntity, CancellationToken ct = default)
        {
            _mapper.Map(clientToPatch, clientEntity);

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

        private async Task<Client> GetAndCheckIfClientExistsAsync(Guid clientId, bool trackChanges, bool includeFounders, CancellationToken ct)
        {
            var clientEntity = await _repository.Client.GetClientAsync(clientId, trackChanges, includeFounders, ct);

            if (clientEntity is null)
                throw new ClientNotFoundException(clientId);

            return clientEntity;
        }
    }
}