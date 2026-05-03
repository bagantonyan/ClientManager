using ClientManager.Core.Domain.Repositories;
using ClientManager.Core.Services.Abstractions;
using LoggingService;

namespace ClientManager.Core.Services
{
    internal sealed class FounderService : IFounderService
    {
        private readonly IRepositoryManager _repository;
        private readonly ILoggerManager _logger;
        public FounderService(IRepositoryManager repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }
    }
}