using ClientManager.Core.Domain.Entities;

namespace ClientManager.Core.Services.Abstractions
{
    public interface IClientService
    {
        IEnumerable<Client> GetAllClients(bool trackChanges);
    }
}