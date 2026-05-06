using FluentValidation;
using Shared.DataTransferObjects.Founders;

namespace ClientManager.Infrastructure.Presentation.Validators.Founders
{
    public class FounderForCreationDtoValidator : AbstractValidator<FounderForCreationDto>
    {
        public FounderForCreationDtoValidator()
        {
            RuleFor(x => x.INN)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("INN is required.")
                .Length(12).WithMessage("Founder INN must be exactly 12 digits.")
                .Must(inn => inn!.All(char.IsDigit)).WithMessage("INN must contain digits only.")
                .Must(InnValidator.IsValid).WithMessage("INN check digits are invalid.");

            RuleFor(x => x.FullName)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("FullName is required.")
                .MaximumLength(500).WithMessage("FullName must not exceed 500 characters.");
        }
    }
}
