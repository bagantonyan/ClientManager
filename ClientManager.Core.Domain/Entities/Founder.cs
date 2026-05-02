namespace ClientManager.Core.Domain.Entities
{
    public class Founder : BaseEntity
    {
        public Guid Id { get; set; }
        public string? INN { get; set; }
        public string? FullName { get; set; }

        public ICollection<ClientFounder>? ClientFounders { get; set; } = [];
    }
}