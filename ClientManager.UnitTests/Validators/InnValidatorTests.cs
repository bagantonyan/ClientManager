using ClientManager.Infrastructure.Presentation.Validators;
using ClientManager.UnitTests.TestData;
using FluentAssertions;

namespace ClientManager.UnitTests.Validators
{
    public class InnValidatorTests
    {
        [Theory]
        [InlineData(ValidInns.LegalEntity1)]
        [InlineData(ValidInns.Individual1)]
        public void IsValid_returns_true_for_valid_inn(string inn) =>
            InnValidator.IsValid(inn).Should().BeTrue();

        [Theory]
        [InlineData("7707083890")]
        [InlineData("770708389300")]
        public void IsValid_returns_false_for_wrong_check_digits(string inn) =>
            InnValidator.IsValid(inn).Should().BeFalse();

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("12345")]
        [InlineData("12345678901")]
        [InlineData("abc1234567")]
        public void IsValid_returns_false_for_invalid_format(string? inn) =>
            InnValidator.IsValid(inn).Should().BeFalse();
    }
}