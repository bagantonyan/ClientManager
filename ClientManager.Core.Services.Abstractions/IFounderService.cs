using Shared.DataTransferObjects.Founders;

namespace ClientManager.Core.Services.Abstractions
{
    public interface IFounderService
    {
        IEnumerable<FounderDto> GetFounders(Guid clientId, bool trackChanges);
    }
}