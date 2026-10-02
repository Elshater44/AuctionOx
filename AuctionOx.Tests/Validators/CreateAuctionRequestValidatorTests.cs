using AuctionOx.DTOs.Auctions;
using AuctionOx.Validators;
using FluentAssertions;

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
        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        public void Validate_StartingPrice_Should_GreaterThanZero_With_Values_EqualToOrLessThan_Zero(decimal startingPrice)
        {

            var request = CreateValidRequest();

            request.StartingPrice = startingPrice;

            var result = _validator.Validate(request);

            result.IsValid.Should().BeFalse();

        }
        [Fact]
        public void Validate_StartTime_Should_BeInTheFuture()
        {
            var request = CreateValidRequest();
            request.StartTime = DateTime.UtcNow.AddMinutes(-6); // 5 minutes in the past
            var result = _validator.Validate(request);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAuctionRequest.StartTime))
                .Which.ErrorMessage.Should().Contain("Start time cannot be in the past.");
        }

        [Theory]
        [InlineData(-1)] // End time before start time
        [InlineData(0)]  // End time same as start time
        public void Validate_EndTime_Should_BeAfter_StartTime(int endTimeOffsetMinutes)
        {
            var request = CreateValidRequest();
            request.StartTime = DateTime.UtcNow.AddMinutes(10);
            request.EndTime = request.StartTime.AddMinutes(endTimeOffsetMinutes);

            var result = _validator.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAuctionRequest.EndTime))
                .Which.ErrorMessage.Should().Contain("End time must be after start time.");
        }
    }
}
