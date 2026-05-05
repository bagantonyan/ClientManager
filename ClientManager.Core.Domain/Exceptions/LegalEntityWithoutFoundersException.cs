namespace ClientManager.Core.Domain.Exceptions
{
    public sealed class LegalEntityWithoutFoundersException : BadRequestException
    {
        public LegalEntityWithoutFoundersException()
            : base("A client of type Legal_Entity must have at least one founder.")
        { }
    }
}
