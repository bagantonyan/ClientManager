using ClientManager.Core.Domain.Entities;

namespace ClientManager.Core.Domain.Repositories
{
    public interface IFounderRepository
    {
        Task<IEnumerable<Founder>> GetFoundersAsync(Guid clientId, bool trackChanges, CancellationToken ct = default);
        Task<Founder> GetFounderAsync(Guid clientId, Guid id, bool trackChanges, CancellationToken ct = default);
        Task<Founder> GetFounderWithLinksAsync(Guid clientId, Guid id, bool trackChanges, CancellationToken ct = default);
        void CreateFounderForClient(Client client, Founder founder);
        void DeleteFounder(Founder founder);
    }
}