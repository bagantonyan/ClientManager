using Shared.DataTransferObjects.Founders;

namespace ClientManager.Core.Services.Abstractions
{
    public interface IFounderService
    {
        Task<IEnumerable<FounderDto>> GetFoundersAsync(Guid clientId, bool trackChanges);
        Task<FounderDto> GetFounderAsync(Guid clientId, Guid id, bool trackChanges);
        Task<FounderDto> CreateFounderForClientAsync(Guid clientId, FounderForCreationDto founderForCreation, bool trackChanges);
        Task DeleteFounderForClientAsync(Guid clientId, Guid id, bool trackChanges);
    }
}