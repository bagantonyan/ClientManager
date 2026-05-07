using Shared.Enums;

namespace ClientManager.Core.Domain.Entities
{
    public class Client : BaseEntity
    {
        public Guid Id { get; set; }
        public string INN { get; set; } = null!;
        public string Name { get; set; } = null!;
        public ClientType ClientType { get; set; }

        public ICollection<ClientFounder> ClientFounders { get; set; } = new List<ClientFounder>();
    }
}