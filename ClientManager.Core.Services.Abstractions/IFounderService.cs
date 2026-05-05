using Shared.DataTransferObjects.Founders;

namespace ClientManager.Core.Services.Abstractions
{
    public interface IFounderService
    {
        IEnumerable<FounderDto> GetFounders(Guid clientId, bool trackChanges);
        FounderDto GetFounder(Guid clientId, Guid id, bool trackChanges);
        FounderDto CreateFounderForClient(Guid clientId, FounderForCreationDto founderForCreation, bool trackChanges);
        void DeleteFounderForClient(Guid clientId, Guid id, bool trackChanges);
    }
}