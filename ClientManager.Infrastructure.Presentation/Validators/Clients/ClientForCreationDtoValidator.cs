using FluentValidation;
using Shared.DataTransferObjects.Clients;

namespace ClientManager.Infrastructure.Presentation.Validators.Clients
{
    public class ClientForCreationDtoValidator : AbstractValidator<ClientForCreationDto>
    {
        public ClientForCreationDtoValidator()
        {
            
        }
    }
}