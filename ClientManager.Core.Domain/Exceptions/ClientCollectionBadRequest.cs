namespace ClientManager.Core.Domain.Exceptions
{
    public sealed class ClientCollectionBadRequest : BadRequestException
    {
        public ClientCollectionBadRequest()
            : base("Client collection sent from a client is null.")
        {
        }
    }
}