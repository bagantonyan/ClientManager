using Shared.DataTransferObjects.Clients;

namespace ClientManager.Core.Services.Abstractions
{
    public interface IClientService
    {
        IEnumerable<ClientDto> GetAllClients(bool trackChanges, bool includeFounders);
        ClientDto GetClient(Guid clientId, bool trackChanges, bool includeFounders);
        ClientDto CreateClient(ClientForCreationDto client);
        IEnumerable<ClientDto> GetByIds(IEnumerable<Guid> ids, bool trackChanges);
        (IEnumerable<ClientDto> clients, string ids) CreateClientCollection(IEnumerable<ClientForCreationDto> clientCollection);
    }
}