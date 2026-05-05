using ClientManager.Core.Domain.Entities;

namespace ClientManager.Core.Domain.Repositories
{
    public interface IFounderRepository
    {
        IEnumerable<Founder> GetFounders(Guid clientId, bool trackChanges);
        Founder GetFounder(Guid clientId, Guid id, bool trackChanges);
        void CreateFounderForClient(Client client, Founder founder);
    }
}