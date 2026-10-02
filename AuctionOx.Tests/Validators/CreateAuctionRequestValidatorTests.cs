using System;
using AuctionOx.DTOs.Auctions;
using AuctionOx.Validators;
using FluentAssertions;
using Xunit;

namespace AuctionOx.Tests.Validators
{
    public class CreateAuctionRequestValidatorTests
    {
        private readonly CreateAuctionRequestValidator _validator;

        public CreateAuctionRequestValidatorTests()
        {
            // The constructor runs before EVERY single test in this class.
            // This ensures a clean, fresh validator instance every time.
            _validator = new CreateAuctionRequestValidator();
        }

        /// <summary>
        /// Helper to generate a baseline valid request.
        /// In individual tests, we only mutate the single property we want to test.
        /// </summary>
        private static CreateAuctionRequest CreateValidRequest()
        {
            return new CreateAuctionRequest
            {
                Title = "Vintage Leather Jacket",
                Description = "Authentic 1980s leather jacket in mint condition.",
                StartingPrice = 50.00m,
                BuyItNowPrice = 120.00m,
                StartTime = DateTime.UtcNow.AddMinutes(10),
                EndTime = DateTime.UtcNow.AddDays(3),
                CategoryId = 1,
                AntiSnipingMinutes = 5
            };
        }

        [Fact]
        public void Validate_WhenAllFieldsAreValid_ShouldPassValidation()
        {
            // 1. Arrange: Prepare a completely valid model
            var request = CreateValidRequest();

            // 2. Act: Run the validator
            var result = _validator.Validate(request);

            // 3. Assert: Verify result is valid with zero errors
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public void Validate_WhenTitleIsEmpty_ShouldFailValidation()
        {
            // 1. Arrange: Start from valid, but break Title
            var request = CreateValidRequest();
            request.Title = string.Empty;

            // 2. Act
            var result = _validator.Validate(request);

            // 3. Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAuctionRequest.Title));
        }

        [Fact]
        public void Validate_WhenBuyItNowIsLowerThanStartingPrice_ShouldFailValidation()
        {
            // 1. Arrange: BuyItNow ($40) is lower than StartingPrice ($50)
            var request = CreateValidRequest();
            request.StartingPrice = 50.00m;
            request.BuyItNowPrice = 40.00m;

            // 2. Act
            var result = _validator.Validate(request);

            // 3. Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAuctionRequest.BuyItNowPrice))
                .Which.ErrorMessage.Should().Contain("Buy It Now price must be greater than starting price");
        }
    }
}
