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
            var client = await _repository.Client.GetClientAsync(clientId, trackChanges, includeFounders: false, ct);

            if (client is null)
                throw new ClientNotFoundException(clientId);

            var foundersFromDb = await _repository.Founder.GetFoundersAsync(clientId, trackChanges, ct);

            var foundersDto = _mapper.Map<IEnumerable<FounderDto>>(foundersFromDb);

            return foundersDto;
        }

        public async Task<FounderDto> GetFounderAsync(Guid clientId, Guid id, bool trackChanges, CancellationToken ct = default)
        {
            var client = await _repository.Client.GetClientAsync(clientId, trackChanges, includeFounders: false, ct);

            if (client is null)
                throw new ClientNotFoundException(clientId);

            var founderDb = await _repository.Founder.GetFounderAsync(clientId, id, trackChanges, ct);

            if (founderDb is null)
                throw new FounderNotFoundException(id);

            var founder = _mapper.Map<FounderDto>(founderDb);

            return founder;
        }

        public async Task<FounderDto> CreateFounderForClientAsync(Guid clientId, FounderForCreationDto founderForCreation, bool trackChanges, CancellationToken ct = default)
        {
            var client = await _repository.Client.GetClientAsync(clientId, trackChanges, includeFounders: false, ct);

            if (client is null)
                throw new ClientNotFoundException(clientId);

            if (client.ClientType != ClientType.Legal_Entity)
                throw new FounderNotAllowedForClientException(clientId);

            var founderEntity = _mapper.Map<Founder>(founderForCreation);

            _repository.Founder.CreateFounderForClient(client, founderEntity);

            await _repository.SaveAsync(ct);

            var founderToReturn = _mapper.Map<FounderDto>(founderEntity);

            return founderToReturn;
        }

        public async Task DeleteFounderForClientAsync(Guid clientId, Guid id, bool trackChanges, CancellationToken ct = default)
        {
            var client = await _repository.Client.GetClientAsync(clientId, trackChanges: true, includeFounders: true, ct);

            if (client is null)
                throw new ClientNotFoundException(clientId);

            var founder = await _repository.Founder.GetFounderWithLinksAsync(clientId, id, trackChanges: true, ct);

            if (founder is null)
                throw new FounderNotFoundException(id);

            if (client.ClientType == ClientType.Legal_Entity
                && client.ClientFounders!.Count <= 1)
                throw new LegalEntityWithoutFoundersException();

            var linkToRemove = founder.ClientFounders!.Single(cf => cf.ClientId.Equals(clientId));
            founder.ClientFounders!.Remove(linkToRemove);

            if (founder.ClientFounders!.Count == 0)
                _repository.Founder.DeleteFounder(founder);

            await _repository.SaveAsync(ct);
        }
    }
}