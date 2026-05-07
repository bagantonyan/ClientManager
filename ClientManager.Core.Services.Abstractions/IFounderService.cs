using ClientManager.Core.Domain.Entities;
using Shared.DataTransferObjects.Founders;

namespace ClientManager.Core.Services.Abstractions
{
    public interface IFounderService
    {
        Task<IEnumerable<FounderDto>> GetFoundersAsync(Guid clientId, bool trackChanges, CancellationToken ct = default);
        Task<FounderDto> GetFounderAsync(Guid clientId, Guid id, bool trackChanges, CancellationToken ct = default);
        Task<FounderDto> CreateFounderForClientAsync(Guid clientId, FounderForCreationDto founderForCreation, bool trackChanges, CancellationToken ct = default);
        Task DeleteFounderForClientAsync(Guid clientId, Guid id, bool trackChanges, CancellationToken ct = default);
        Task<(FounderForUpdateDto founderToPatch, Founder founderEntity)> GetFounderForPatchAsync(Guid clientId, Guid id, bool clientTrackChanges, bool founderTrackChanges, CancellationToken ct = default);
        Task SaveChangesForPatchAsync(FounderForUpdateDto founderToPatch, Founder founderEntity, byte[]? ifMatch, CancellationToken ct = default);
    }
}