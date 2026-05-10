using Microsoft.AspNetCore.Identity;
using Shared.DataTransferObjects.Users;

namespace ClientManager.Core.Services.Abstractions
{
    public interface IAuthenticationService
    {
        Task<IdentityResult> RegisterUser(UserForRegistrationDto userForRegistration);
    }
}