namespace ClientManager.Core.Domain.Exceptions
{
    public class FounderNotFoundException : NotFoundException
    {
        public FounderNotFoundException(Guid employeeId)
            : base($"Founder with id: {employeeId} doesn't exist in the database.")
        {
        }
    }
}