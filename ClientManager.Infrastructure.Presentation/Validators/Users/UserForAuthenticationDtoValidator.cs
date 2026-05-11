using FluentValidation;
using Shared.DataTransferObjects.Users;

namespace ClientManager.Infrastructure.Presentation.Validators.Users
{
    public class UserForAuthenticationDtoValidator : AbstractValidator<UserForAuthenticationDto>
    {
        public UserForAuthenticationDtoValidator()
        {
            RuleFor(x => x.UserName)
                .NotEmpty().WithMessage("UserName is required.")
                .MaximumLength(256).WithMessage("UserName must not exceed 256 characters.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MaximumLength(256).WithMessage("Password must not exceed 256 characters.");
        }
    }
}
