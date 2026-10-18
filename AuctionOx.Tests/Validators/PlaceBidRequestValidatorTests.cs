using AuctionOx.DTOs.Bids;
using AuctionOx.Validators;
using FluentAssertions;
using Xunit;

namespace AuctionOx.Tests.Validators
{
    public class PlaceBidRequestValidatorTests
    {
        private readonly PlaceBidRequestValidator _validator = new();

        [Theory]
        [InlineData(0.01)]
        [InlineData(10.00)]
        [InlineData(5000.50)]
        public void Validate_WhenBidAmountIsPositive_ShouldPass(decimal amount)
        {
            var request = new PlaceBidRequest { BidAmount = amount };
            var result = _validator.Validate(request);

            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100.50)]
        public void Validate_WhenBidAmountIsZeroOrNegative_ShouldFail(decimal amount)
        {
            var request = new PlaceBidRequest { BidAmount = amount };
            var result = _validator.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(PlaceBidRequest.BidAmount))
                .Which.ErrorMessage.Should().Be("Bid amount must be greater than zero.");
        }
    }
}
