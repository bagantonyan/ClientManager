using AutoMapper;
using ClientManager.Core.Domain.Entities;
using ClientManager.Core.Domain.Exceptions;
using ClientManager.Core.Domain.Repositories;
using ClientManager.Core.Services.Abstractions;
using LoggingService;
using Shared.DataTransferObjects.Founders;
using Shared.Enums;

namespace ClientManager.Core.Services
{
    internal sealed class FounderService : IFounderService
    {
        private readonly IRepositoryManager _repository;
        private readonly ILoggerManager _logger;
        private readonly IMapper _mapper;

        public FounderService(
            IRepositoryManager repository, 
            ILoggerManager logger,
            IMapper mapper)
        {
            _repository = repository;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<IEnumerable<FounderDto>> GetFoundersAsync(Guid clientId, bool trackChanges, CancellationToken ct = default)
        {
            var client = await GetAndCheckIfClientExistsAsync(clientId, trackChanges, includeFounders: false, ct);

            var foundersFromDb = await _repository.Founder.GetFoundersAsync(clientId, trackChanges, ct);

            var foundersDto = _mapper.Map<IEnumerable<FounderDto>>(foundersFromDb);

            return foundersDto;
        }

        public async Task<FounderDto> GetFounderAsync(Guid clientId, Guid id, bool trackChanges, CancellationToken ct = default)
        {
            await GetAndCheckIfClientExistsAsync(clientId, trackChanges, includeFounders: false, ct);

            var founderDb = await GetFounderForClientAndCheckIfItExistsAsync(clientId, id, trackChanges, ct);

            var founder = _mapper.Map<FounderDto>(founderDb);

            return founder;
        }

        public async Task<FounderDto> CreateFounderForClientAsync(Guid clientId, FounderForCreationDto founderForCreation, bool trackChanges, CancellationToken ct = default)
        {
            var client = await GetAndCheckIfClientExistsAsync(clientId, trackChanges: true, includeFounders: false, ct);

            if (client.ClientType != ClientType.Legal_Entity)
            {
                _logger.LogWarning($"Cannot add founder to client {clientId}: client type is {client.ClientType}.");
                throw new FounderNotAllowedForClientException(clientId);
            }

            var existing = await _repository.Founder.GetByInnIncludingDeletedAsync(founderForCreation.INN!, trackChanges: true, ct);

            Founder founderEntity;
            if (existing is not null)
            {
                if (existing.DeletedDate is not null)
                {
                    _logger.LogInformation($"Restoring soft-deleted founder. Id: {existing.Id}.");
                    existing.DeletedDate = null;
                    existing.FullName = founderForCreation.FullName!;
                }

                if (existing.ClientFounders.Any(cf => cf.ClientId == clientId))
                {
                    _logger.LogWarning($"Founder {existing.Id} is already linked to client {clientId}.");
                    throw new FounderAlreadyLinkedToClientException(clientId, existing.Id);
                }

                _logger.LogDebug($"Linking existing founder {existing.Id} to client {clientId}.");
                existing.ClientFounders.Add(new ClientFounder { Client = client });
                founderEntity = existing;
            }
            else
            {
                _logger.LogDebug($"Creating new founder for client {clientId}.");
                founderEntity = _mapper.Map<Founder>(founderForCreation);
                _repository.Founder.CreateFounderForClient(client, founderEntity);
            }

            await _repository.SaveAsync(ct);

            _logger.LogInformation($"Founder {founderEntity.Id} saved for client {clientId}.");

            var founderToReturn = _mapper.Map<FounderDto>(founderEntity);

            return founderToReturn;
        }

        public async Task DeleteFounderForClientAsync(Guid clientId, Guid id, bool trackChanges, CancellationToken ct = default)
        {
            var client = await GetAndCheckIfClientExistsAsync(clientId, trackChanges, includeFounders: true, ct);

            var founder = await _repository.Founder.GetFounderWithLinksAsync(clientId, id, trackChanges: true, ct);

            if (founder is null)
            {
                _logger.LogWarning($"Founder {id} for client {clientId} not found.");
                throw new FounderNotFoundException(id);
            }

            if (client.ClientType == ClientType.Legal_Entity
                && client.ClientFounders!.Count <= 1)
            {
                _logger.LogWarning($"Cannot remove the last founder of legal entity {clientId}.");
                throw new LegalEntityWithoutFoundersException();
            }

            var linkToRemove = founder.ClientFounders!.Single(cf => cf.ClientId.Equals(clientId));
            founder.ClientFounders!.Remove(linkToRemove);

            var founderAlsoDeleted = false;
            if (founder.ClientFounders!.Count == 0)
            {
                _repository.Founder.DeleteFounder(founder);
                founderAlsoDeleted = true;
            }

            await _repository.SaveAsync(ct);

            _logger.LogInformation(
                $"Founder {id} unlinked from client {clientId}." +
                (founderAlsoDeleted ? " Founder soft-deleted (no other links remain)." : ""));
        }

        public async Task<(FounderForUpdateDto founderToPatch, Founder founderEntity)> GetFounderForPatchAsync(Guid clientId, Guid id, bool clientTrackChanges, bool founderTrackChanges, CancellationToken ct = default)
        {
            await GetAndCheckIfClientExistsAsync(clientId, clientTrackChanges, includeFounders: false, ct);

            var founderEntity = await GetFounderForClientAndCheckIfItExistsAsync(clientId, id, founderTrackChanges, ct);

            var founderToPatch = _mapper.Map<FounderForUpdateDto>(founderEntity);

            return (founderToPatch, founderEntity);
        }

        public async Task SaveChangesForPatchAsync(FounderForUpdateDto founderToPatch, Founder founderEntity, byte[]? ifMatch, CancellationToken ct = default)
        {
            _mapper.Map(founderToPatch, founderEntity);

            if (ifMatch is not null)
                _repository.SetOriginalRowVersion(founderEntity, ifMatch);

            await _repository.SaveAsync(ct);

            _logger.LogInformation($"Founder {founderEntity.Id} updated via patch.");
        }

        private async Task<Client> GetAndCheckIfClientExistsAsync(Guid clientId, bool trackChanges, bool includeFounders, CancellationToken ct)
        {
            var client = await _repository.Client.GetClientAsync(clientId, trackChanges, includeFounders, ct);

            if (client is null)
            {
                _logger.LogWarning($"Client {clientId} not found in the database.");
                throw new ClientNotFoundException(clientId);
            }

            return client;
        }

        private async Task<Founder> GetFounderForClientAndCheckIfItExistsAsync(Guid clientId, Guid id, bool trackChanges, CancellationToken ct)
        {
            var founder = await _repository.Founder.GetFounderAsync(clientId, id, trackChanges, ct);

            if (founder is null)
            {
                _logger.LogWarning($"Founder {id} for client {clientId} not found in the database.");
                throw new FounderNotFoundException(id);
            }

            return founder;
        }
    }
}