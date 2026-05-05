namespace ClientManager.Core.Domain.Exceptions
{
    public sealed class FounderNotAllowedForClientException : BadRequestException
    {
        public FounderNotAllowedForClientException(Guid clientId)
            : base($"Founders can only be assigned to clients of type Legal_Entity. Client {clientId} has a different type.")
        { }
    }
}
