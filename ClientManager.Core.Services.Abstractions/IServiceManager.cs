namespace ClientManager.Core.Services.Abstractions
{
    public interface IServiceManager
    {
        IClientService ClientService { get; }
        IFounderService FounderService { get; }
        IAuthenticationService AuthenticationService { get; }
    }
}