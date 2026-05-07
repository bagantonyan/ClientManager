namespace ClientManager.Core.Domain.Entities
{
    public class Founder : BaseEntity
    {
        public Guid Id { get; set; }
        public string INN { get; set; } = null!;
        public string FullName { get; set; } = null!;

        public ICollection<ClientFounder> ClientFounders { get; set; } = new List<ClientFounder>();
    }
}