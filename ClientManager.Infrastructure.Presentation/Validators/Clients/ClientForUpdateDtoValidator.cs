using FluentValidation;
using Shared.DataTransferObjects.Clients;

namespace ClientManager.Infrastructure.Presentation.Validators.Clients
{
    public class ClientForUpdateDtoValidator : AbstractValidator<ClientForUpdateDto>
    {
        public ClientForUpdateDtoValidator()
        {
            RuleFor(x => x.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(500).WithMessage("Name must not exceed 500 characters.");
        }
    }
}
