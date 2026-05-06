using FluentValidation;
using Shared.DataTransferObjects.Founders;

namespace ClientManager.Infrastructure.Presentation.Validators.Founders
{
    public class FounderForCreationDtoValidator : AbstractValidator<FounderForCreationDto>
    {
        public FounderForCreationDtoValidator()
        {
            
        }
    }
}