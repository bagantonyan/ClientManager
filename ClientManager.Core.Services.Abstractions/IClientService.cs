using ClientManager.Core.Domain.Entities;
using Shared.DataTransferObjects.Clients;

namespace ClientManager.Core.Services.Abstractions
{
    public interface IClientService
    {
        Task<IEnumerable<ClientDto>> GetAllClientsAsync(bool trackChanges, bool includeFounders, CancellationToken ct = default);
        Task<ClientDto> GetClientAsync(Guid clientId, bool trackChanges, bool includeFounders, CancellationToken ct = default);
        Task<ClientDto> CreateClientAsync(ClientForCreationDto client, CancellationToken ct = default);
        Task<IEnumerable<ClientDto>> GetByIdsAsync(IEnumerable<Guid> ids, bool trackChanges, bool includeFounders, CancellationToken ct = default);
        Task<(IEnumerable<ClientDto> clients, string ids)> CreateClientCollectionAsync(IEnumerable<ClientForCreationDto> clientCollection, CancellationToken ct = default);
        Task DeleteClientAsync(Guid clientId, CancellationToken ct = default);
        Task<(ClientForUpdateDto clientToPatch, Client clientEntity)> GetClientForPatchAsync(Guid clientId, bool trackChanges, CancellationToken ct = default);
        Task SaveChangesForPatchAsync(ClientForUpdateDto clientToPatch, Client clientEntity, CancellationToken ct = default);
    }
}