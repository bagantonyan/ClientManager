namespace ClientManager.Core.Domain.Exceptions
{
    public sealed class ClientNotFoundException : NotFoundException
    {
        public ClientNotFoundException(Guid companyId)
            : base($"The client with id: {companyId} doesn't exist in the database.")
        {
        }
    }
}