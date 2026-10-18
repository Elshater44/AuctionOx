using AuctionOx.DTOs.Auth;
using AuctionOx.Validators;
using FluentAssertions;
using Xunit;

namespace AuctionOx.Tests.Validators
{
    public class LoginRequestValidatorTests
    {
        private readonly LoginRequestValidator _validator = new();

        [Fact]
        public void Validate_WhenCredentialsAreValid_ShouldPass()
        {
            var request = new LoginRequest
            {
                Email = "user@example.com",
                Password = "Password123!"
            };

            var result = _validator.Validate(request);
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Theory]
        [InlineData("", "password")]
        [InlineData("not-an-email", "password")]
        [InlineData("user@example.com", "")]
        public void Validate_WhenFieldsAreInvalid_ShouldFail(string email, string password)
        {
            var request = new LoginRequest
            {
                Email = email,
                Password = password
            };

            var result = _validator.Validate(request);
            result.IsValid.Should().BeFalse();
        }
    }
}
