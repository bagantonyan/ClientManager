namespace ClientManager.Core.Domain.Repositories
{
    public interface IRepositoryManager
    {
        IClientRepository Client { get; }
        IFounderRepository Founder { get; }
        Task SaveAsync(CancellationToken ct = default);
    }
}