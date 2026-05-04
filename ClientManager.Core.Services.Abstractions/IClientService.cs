using Shared.DataTransferObjects.Clients;

namespace ClientManager.Core.Services.Abstractions
{
    public interface IClientService
    {
        IEnumerable<ClientDto> GetAllClients(bool trackChanges);
        ClientDto GetClient(Guid clientId, bool trackChanges);
    }
}