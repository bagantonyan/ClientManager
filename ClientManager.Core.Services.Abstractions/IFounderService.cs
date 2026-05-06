using Shared.DataTransferObjects.Founders;

namespace ClientManager.Core.Services.Abstractions
{
    public interface IFounderService
    {
        Task<IEnumerable<FounderDto>> GetFoundersAsync(Guid clientId, bool trackChanges, CancellationToken ct = default);
        Task<FounderDto> GetFounderAsync(Guid clientId, Guid id, bool trackChanges, CancellationToken ct = default);
        Task<FounderDto> CreateFounderForClientAsync(Guid clientId, FounderForCreationDto founderForCreation, bool trackChanges, CancellationToken ct = default);
        Task DeleteFounderForClientAsync(Guid clientId, Guid id, bool trackChanges, CancellationToken ct = default);
        Task UpdateFounderForClientAsync(Guid clientId, Guid id, FounderForUpdateDto founderForUpdate, bool clientTrackChanges, bool founderTrackChanges, CancellationToken ct = default);
    }
}