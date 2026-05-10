using FluentValidation;
using Shared.DataTransferObjects.Users;

namespace ClientManager.Infrastructure.Presentation.Validators.Users
{
    public class UserForRegistrationDtoValidator : AbstractValidator<UserForRegistrationDto>
    {
        public UserForRegistrationDtoValidator()
        {
            RuleFor(x => x.UserName)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("UserName is required.")
                .Length(3, 50).WithMessage("UserName must be between 3 and 50 characters.")
                .Matches(@"^[a-zA-Z0-9_.\-]+$")
                    .WithMessage("UserName may contain only letters, digits, '_', '.' and '-'.");

            RuleFor(x => x.Password)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
                .MaximumLength(100).WithMessage("Password must not exceed 100 characters.")
                .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                .Matches(@"\d").WithMessage("Password must contain at least one digit.")
                .Matches(@"[^a-zA-Z0-9]").WithMessage("Password must contain at least one non-alphanumeric character.");

            RuleFor(x => x.Email)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email is not a valid email address.")
                .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");

            RuleFor(x => x.FirstName)
                .MaximumLength(100).WithMessage("FirstName must not exceed 100 characters.");

            RuleFor(x => x.LastName)
                .MaximumLength(100).WithMessage("LastName must not exceed 100 characters.");

            RuleFor(x => x.PhoneNumber)
                .Matches(@"^\+?\d{7,15}$")
                    .WithMessage("PhoneNumber must be 7-15 digits, optionally starting with '+'.")
                .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

            RuleFor(x => x.Roles)
                .Must(roles => roles == null || roles.All(r => !string.IsNullOrWhiteSpace(r)))
                .WithMessage("Roles must not contain empty values.");
        }
    }
}
