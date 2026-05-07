using ClientManager.Core.Domain.Entities;

namespace ClientManager.Core.Domain.Repositories
{
    public interface IRepositoryManager
    {
        IClientRepository Client { get; }
        IFounderRepository Founder { get; }
        Task SaveAsync(CancellationToken ct = default);
        void SetOriginalRowVersion<T>(T entity, byte[] rowVersion) where T : BaseEntity;
    }
}