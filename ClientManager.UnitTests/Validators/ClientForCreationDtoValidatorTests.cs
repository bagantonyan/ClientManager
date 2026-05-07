using ClientManager.Infrastructure.Presentation.Validators.Clients;
using ClientManager.UnitTests.TestData;
using FluentAssertions;
using Shared.DataTransferObjects.Clients;
using Shared.DataTransferObjects.Founders;
using Shared.Enums;

namespace ClientManager.UnitTests.Validators
{
    public class ClientForCreationDtoValidatorTests
    {
        private readonly ClientForCreationDtoValidator _sut = new();

        private static FounderForCreationDto ValidFounder() =>
            new() { INN = ValidInns.Individual1, FullName = "Иванов И.И." };

        [Fact]
        public void Legal_entity_with_founder_passes() =>
            _sut.Validate(new ClientForCreationDto
            {
                INN = ValidInns.LegalEntity1,
                Name = "ООО",
                ClientType = ClientType.Legal_Entity,
                Founders = new[] { ValidFounder() }
            }).IsValid.Should().BeTrue();

        [Fact]
        public void Individual_entrepreneur_without_founders_passes() =>
            _sut.Validate(new ClientForCreationDto
            {
                INN = ValidInns.Individual2,
                Name = "ИП",
                ClientType = ClientType.Individual_Entrepreneur,
                Founders = null
            }).IsValid.Should().BeTrue();

        [Fact]
        public void Legal_entity_without_founders_fails() =>
            _sut.Validate(new ClientForCreationDto
            {
                INN = ValidInns.LegalEntity1,
                Name = "ООО",
                ClientType = ClientType.Legal_Entity,
                Founders = Array.Empty<FounderForCreationDto>()
            }).IsValid.Should().BeFalse();

        [Fact]
        public void Individual_entrepreneur_with_founders_fails() =>
            _sut.Validate(new ClientForCreationDto
            {
                INN = ValidInns.Individual2,
                Name = "ИП",
                ClientType = ClientType.Individual_Entrepreneur,
                Founders = new[] { ValidFounder() }
            }).IsValid.Should().BeFalse();

        [Fact]
        public void Inn_length_must_match_client_type() =>
            _sut.Validate(new ClientForCreationDto
            {
                INN = ValidInns.Individual1,
                Name = "ООО",
                ClientType = ClientType.Legal_Entity,
                Founders = new[] { ValidFounder() }
            }).IsValid.Should().BeFalse();
    }
}
