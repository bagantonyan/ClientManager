using AutoMapper;
using ClientManager.Core.Domain.Entities;
using ClientManager.Core.Domain.Exceptions;
using ClientManager.Core.Domain.Repositories;
using ClientManager.Core.Services.Abstractions;
using LoggingService;
using Shared.DataTransferObjects.Clients;
using Shared.DataTransferObjects.Founders;
using Shared.Enums;
using Shared.RequestFeatures;

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

        public async Task<(IEnumerable<ClientDto> clients, MetaData metaData)> GetAllClientsAsync(ClientParameters clientParameters, bool trackChanges, bool includeFounders, CancellationToken ct = default)
        {
            var clientsWithMetaData = await _repository.Client.GetAllClientsAsync(clientParameters, trackChanges, includeFounders, ct);

            var clientsDto = _mapper.Map<IEnumerable<ClientDto>>(clientsWithMetaData);

            return (clients: clientsDto, metaData: clientsWithMetaData.MetaData);
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

            var founderCache = new Dictionary<string, Founder>();
            var clientEntity = await ResolveOrCreateClientAsync(client, founderCache, ct);

            await _repository.SaveAsync(ct);

            var clientToReturn = _mapper.Map<ClientDto>(clientEntity);

            return clientToReturn;
        }

        private async Task<Client> ResolveOrCreateClientAsync(
            ClientForCreationDto dto,
            Dictionary<string, Founder> founderCache,
            CancellationToken ct)
        {
            var existing = await _repository.Client.GetByInnIncludingDeletedAsync(dto.INN!, trackChanges: true, ct);

            Client clientEntity;
            if (existing is not null)
            {
                if (existing.DeletedDate is null)
                    throw new ClientWithSameInnExistsException(dto.INN!);

                existing.DeletedDate = null;
                existing.Name = dto.Name;
                existing.ClientType = dto.ClientType;
                clientEntity = existing;
            }
            else
            {
                clientEntity = new Client
                {
                    INN = dto.INN,
                    Name = dto.Name,
                    ClientType = dto.ClientType
                };
                _repository.Client.CreateClient(clientEntity);
            }

            if (dto.Founders is not null && dto.Founders.Any())
            {
                var resolved = new List<ClientFounder>();
                foreach (var founderDto in dto.Founders)
                {
                    var founder = await ResolveFounderAsync(founderDto, founderCache, ct);
                    resolved.Add(new ClientFounder { Founder = founder });
                }
                clientEntity.ClientFounders = resolved;
            }

            return clientEntity;
        }

        private async Task<Founder> ResolveFounderAsync(
            FounderForCreationDto dto,
            Dictionary<string, Founder> cache,
            CancellationToken ct)
        {
            if (cache.TryGetValue(dto.INN!, out var cached))
                return cached;

            var existing = await _repository.Founder.GetByInnIncludingDeletedAsync(dto.INN!, trackChanges: true, ct);

            Founder result;
            if (existing is not null)
            {
                if (existing.DeletedDate is not null)
                {
                    existing.DeletedDate = null;
                    existing.FullName = dto.FullName;
                }
                result = existing;
            }
            else
            {
                result = _mapper.Map<Founder>(dto);
            }

            cache[dto.INN!] = result;
            return result;
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

            var dtos = clientCollection.ToList();

            foreach (var dto in dtos)
                ValidateFoundersByClientType(dto);

            var founderCache = new Dictionary<string, Founder>();
            var clientEntities = new List<Client>();

            foreach (var dto in dtos)
            {
                var clientEntity = await ResolveOrCreateClientAsync(dto, founderCache, ct);
                clientEntities.Add(clientEntity);
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