namespace ClientManager.Core.Domain.Exceptions
{
    public sealed class ClientNotFoundException : NotFoundException
    {
        public ClientNotFoundException(Guid clientId)
            : base($"The client with id: {clientId} doesn't exist in the database.")
        {
        }
    }
}