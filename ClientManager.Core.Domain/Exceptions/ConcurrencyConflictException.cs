namespace ClientManager.Core.Domain.Exceptions
{
    public sealed class ConcurrencyConflictException : ConflictException
    {
        public ConcurrencyConflictException()
            : base("The resource was modified by another request. Reload it and try again.")
        { }
    }
}
