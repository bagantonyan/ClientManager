using AutoMapper;
using ClientManager.Core.Domain.Entities;
using ClientManager.Core.Domain.Exceptions;
using ClientManager.Core.Domain.Repositories;
using ClientManager.Core.Services.Abstractions;
using LoggingService;
using Shared.DataTransferObjects.Founders;
using System.ComponentModel.Design;

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

        public IEnumerable<FounderDto> GetFounders(Guid clientId, bool trackChanges)
        {
            var client = _repository.Client.GetClient(clientId, trackChanges, includeFounders: false);

            if (client is null)
                throw new ClientNotFoundException(clientId);

            var foundersFromDb = _repository.Founder.GetFounders(clientId, trackChanges);

            var foundersDto = _mapper.Map<IEnumerable<FounderDto>>(foundersFromDb);

            return foundersDto;
        }

        public FounderDto GetFounder(Guid clientId, Guid id, bool trackChanges)
        {
            var client = _repository.Client.GetClient(clientId, trackChanges, false);

            if (client is null)
                throw new ClientNotFoundException(clientId);

            var founderDb = _repository.Founder.GetFounder(clientId, id, trackChanges);

            if (founderDb is null)
                throw new FounderNotFoundException(id);

            var founder = _mapper.Map<FounderDto>(founderDb);

            return founder;
        }

        public FounderDto CreateFounderForClient(Guid clientId, FounderForCreationDto founderForCreation, bool trackChanges)
        {
            var client = _repository.Client.GetClient(clientId, trackChanges, false);

            if (client is null)
                throw new ClientNotFoundException(clientId);

            var founderEntity = _mapper.Map<Founder>(founderForCreation);

            _repository.Founder.CreateFounderForClient(clientId, founderEntity);

            _repository.Save();

            var founderToReturn = _mapper.Map<FounderDto>(founderEntity);

            return founderToReturn;
        }
    }
}