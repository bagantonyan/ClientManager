using ClientManager.Core.Domain.Entities;

namespace ClientManager.Core.Domain.Repositories
{
    public interface IClientRepository
    {
        Task<IEnumerable<Client>> GetAllClientsAsync(bool trackChanges, bool includeFounders);
        Task<Client> GetClientAsync(Guid clientId, bool trackChanges, bool includeFounders);
        void CreateClient(Client client);
        Task<IEnumerable<Client>> GetByIdsAsync(IEnumerable<Guid> ids, bool trackChanges, bool includeFounders);
        Task<Client> GetClientForDeletionAsync(Guid clientId);
        void DeleteClient(Client client);
    }
}