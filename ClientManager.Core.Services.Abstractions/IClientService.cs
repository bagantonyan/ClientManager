using Shared.DataTransferObjects.Clients;

namespace ClientManager.Core.Services.Abstractions
{
    public interface IClientService
    {
        IEnumerable<ClientDto> GetAllClients(bool trackChanges, bool includeFounders);
        ClientDto GetClient(Guid clientId, bool trackChanges, bool includeFounders);
        ClientDto CreateClient(ClientForCreationDto client);
    }
}