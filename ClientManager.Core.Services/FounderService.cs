using AutoMapper;
using ClientManager.Core.Domain.Exceptions;
using ClientManager.Core.Domain.Repositories;
using ClientManager.Core.Services.Abstractions;
using LoggingService;
using Shared.DataTransferObjects.Founders;

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
            var client = _repository.Client.GetClient(clientId, trackChanges);

            if (client is null)
                throw new ClientNotFoundException(clientId);

            var foundersFromDb = _repository.Founder.GetFounders(clientId, trackChanges);

            var foundersDto = _mapper.Map<IEnumerable<FounderDto>>(foundersFromDb);

            return foundersDto;
        }
    }
}