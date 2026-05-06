using ClientManager.Infrastructure.Presentation.Validators.Founders;
using FluentValidation;
using Shared.DataTransferObjects.Clients;
using Shared.Enums;

namespace ClientManager.Infrastructure.Presentation.Validators.Clients
{
    public class ClientForCreationDtoValidator : AbstractValidator<ClientForCreationDto>
    {
        public ClientForCreationDtoValidator()
        {
            RuleFor(x => x.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(500).WithMessage("Name must not exceed 500 characters.");

            RuleFor(x => x.ClientType)
                .IsInEnum().WithMessage("ClientType is invalid.");

            RuleFor(x => x.INN)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("INN is required.")
                .Must(inn => inn!.All(char.IsDigit)).WithMessage("INN must contain digits only.");

            When(x => x.ClientType == ClientType.Legal_Entity, () =>
            {
                RuleFor(x => x.INN)
                    .Cascade(CascadeMode.Stop)
                    .Length(10).WithMessage("INN of a legal entity must be exactly 10 digits.")
                    .Must(InnValidator.IsValid).WithMessage("INN check digits are invalid.");

                RuleFor(x => x.Founders)
                    .NotEmpty().WithMessage("A legal entity must have at least one founder.");
            });

            When(x => x.ClientType == ClientType.Individual_Entrepreneur, () =>
            {
                RuleFor(x => x.INN)
                    .Cascade(CascadeMode.Stop)
                    .Length(12).WithMessage("INN of an individual entrepreneur must be exactly 12 digits.")
                    .Must(InnValidator.IsValid).WithMessage("INN check digits are invalid.");

                RuleFor(x => x.Founders)
                    .Must(f => f == null || !f.Any())
                    .WithMessage("An individual entrepreneur cannot have founders.");
            });

            RuleForEach(x => x.Founders).SetValidator(new FounderForCreationDtoValidator());
        }
    }
}
