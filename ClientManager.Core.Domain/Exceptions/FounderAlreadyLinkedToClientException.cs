namespace ClientManager.Core.Domain.Exceptions
{
    public sealed class FounderAlreadyLinkedToClientException : BadRequestException
    {
        public FounderAlreadyLinkedToClientException(Guid clientId, Guid founderId)
            : base($"Founder {founderId} is already linked to client {clientId}.")
        { }
    }
}
