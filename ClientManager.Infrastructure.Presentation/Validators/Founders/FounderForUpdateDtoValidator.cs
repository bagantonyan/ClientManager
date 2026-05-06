using FluentValidation;
using Shared.DataTransferObjects.Founders;

namespace ClientManager.Infrastructure.Presentation.Validators.Founders
{
    public class FounderForUpdateDtoValidator : AbstractValidator<FounderForUpdateDto>
    {
        public FounderForUpdateDtoValidator()
        {
            RuleFor(x => x.FullName)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("FullName is required.")
                .MaximumLength(500).WithMessage("FullName must not exceed 500 characters.");
        }
    }
}
