namespace ClientManager.Core.Domain.Entities
{
    public class ClientFounder
    {
        public Guid ClientId { get; set; }
        public Client Client { get; set; } = null!;

        public Guid FounderId { get; set; }
        public Founder Founder { get; set; } = null!;
    }
}
