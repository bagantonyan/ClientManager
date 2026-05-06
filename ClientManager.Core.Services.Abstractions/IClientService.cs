using Shared.DataTransferObjects.Clients;

namespace ClientManager.Core.Services.Abstractions
{
    public interface IClientService
    {
        Task<IEnumerable<ClientDto>> GetAllClientsAsync(bool trackChanges, bool includeFounders);
        Task<ClientDto> GetClientAsync(Guid clientId, bool trackChanges, bool includeFounders);
        Task<ClientDto> CreateClientAsync(ClientForCreationDto client);
        Task<IEnumerable<ClientDto>> GetByIdsAsync(IEnumerable<Guid> ids, bool trackChanges, bool includeFounders);
        Task<(IEnumerable<ClientDto> clients, string ids)> CreateClientCollectionAsync(IEnumerable<ClientForCreationDto> clientCollection);
        Task DeleteClientAsync(Guid clientId);
    }
}