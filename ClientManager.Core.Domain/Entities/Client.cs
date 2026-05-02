using ClientManager.Core.Domain.Enums;

namespace ClientManager.Core.Domain.Entities
{
    public class Client : BaseEntity
    {
        public Guid Id { get; set; }
        public string? INN { get; set; }
        public string? Name { get; set; }
        public ClientType ClientType { get; set; }

        public ICollection<ClientFounder>? ClientFounders { get; set; } = [];
    }
}