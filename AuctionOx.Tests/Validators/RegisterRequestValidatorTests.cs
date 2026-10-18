using AuctionOx.DTOs.Auth;
using AuctionOx.Validators;
using FluentAssertions;
using Xunit;

namespace AuctionOx.Tests.Validators
{
    public class RegisterRequestValidatorTests
    {
        private readonly RegisterRequestValidator _validator = new();

        private static RegisterRequest CreateValidRequest() => new()
        {
            Email = "john.doe@example.com",
            Password = "Password123!",
            FirstName = "John",
            LastName = "Doe"
        };

        [Fact]
        public void Validate_WhenValid_ShouldPass()
        {
            var request = CreateValidRequest();
            var result = _validator.Validate(request);

            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Validate_WhenEmailIsEmpty_ShouldFail(string? email)
        {
            var request = CreateValidRequest();
            request.Email = email!;

            var result = _validator.Validate(request);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterRequest.Email));
        }

        [Theory]
        [InlineData("invalid-email")]
        [InlineData("missingatdomain.com")]
        [InlineData("@missinguser.com")]
        public void Validate_WhenEmailIsMalformed_ShouldFail(string email)
        {
            var request = CreateValidRequest();
            request.Email = email;

            var result = _validator.Validate(request);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterRequest.Email));
        }

        [Theory]
        [InlineData("12345")] // 5 chars, minimum is 6
        [InlineData("")]
        public void Validate_WhenPasswordIsTooShort_ShouldFail(string password)
        {
            var request = CreateValidRequest();
            request.Password = password;

            var result = _validator.Validate(request);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterRequest.Password));
        }

        [Fact]
        public void Validate_WhenNamesAreMissingOrTooLong_ShouldFail()
        {
            var request = CreateValidRequest();
            request.FirstName = "";
            request.LastName = new string('A', 101);

            var result = _validator.Validate(request);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterRequest.FirstName));
            result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterRequest.LastName));
        }
    }
}
