using ClientManager.Core.Domain.Entities;

namespace ClientManager.Core.Domain.Repositories
{
    public interface IClientRepository
    {
        IEnumerable<Client> GetAllClients(bool trackChanges, bool includeFounders);
        Client GetClient(Guid clientId, bool trackChanges, bool includeFounders);
        void CreateClient(Client client);
        IEnumerable<Client> GetByIds(IEnumerable<Guid> ids, bool trackChanges, bool includeFounders);
        Client GetClientForDeletion(Guid clientId);
        void DeleteClient(Client client);
    }
}