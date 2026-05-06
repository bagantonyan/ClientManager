namespace ClientManager.Core.Domain.Exceptions
{
    public class FounderNotFoundException : NotFoundException
    {
        public FounderNotFoundException(Guid founderId)
            : base($"Founder with id: {founderId} doesn't exist in the database.")
        {
        }
    }
}