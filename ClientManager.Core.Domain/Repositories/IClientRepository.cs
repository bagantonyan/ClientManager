using ClientManager.Core.Domain.Entities;
using Shared.RequestFeatures;

namespace ClientManager.Core.Domain.Repositories
{
    public interface IClientRepository
    {
        Task<PagedList<Client>> GetAllClientsAsync(ClientParameters clientParameters, bool trackChanges, bool includeFounders, CancellationToken ct = default);
        Task<Client> GetClientAsync(Guid clientId, bool trackChanges, bool includeFounders, CancellationToken ct = default);
        void CreateClient(Client client);
        Task<IEnumerable<Client>> GetByIdsAsync(IEnumerable<Guid> ids, bool trackChanges, bool includeFounders, CancellationToken ct = default);
        Task<Client> GetClientForDeletionAsync(Guid clientId, CancellationToken ct = default);
        Task<Client?> GetByInnIncludingDeletedAsync(string inn, bool trackChanges, CancellationToken ct = default);
        void DeleteClient(Client client);
    }
}