namespace ClientManager.Core.Domain.Exceptions
{
    public sealed class ClientWithSameInnExistsException : BadRequestException
    {
        public ClientWithSameInnExistsException(string inn)
            : base($"An active client with INN {inn} already exists.")
        { }
    }
}
