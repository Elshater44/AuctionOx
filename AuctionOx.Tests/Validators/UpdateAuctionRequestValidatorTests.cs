using System;
using AuctionOx.DTOs.Auctions;
using AuctionOx.Validators;
using FluentAssertions;
using Xunit;

namespace AuctionOx.Tests.Validators
{
    public class UpdateAuctionRequestValidatorTests
    {
        private readonly UpdateAuctionRequestValidator _validator = new();

        private static UpdateAuctionRequest CreateValidRequest() => new()
        {
            Title = "Updated Auction Title",
            Description = "Updated detailed description of the lot.",
            StartTime = DateTime.UtcNow.AddMinutes(5),
            EndTime = DateTime.UtcNow.AddDays(2),
            ImageUrl = "https://example.com/item.jpg"
        };

        [Fact]
        public void Validate_WhenAllFieldsAreValid_ShouldPass()
        {
            var request = CreateValidRequest();
            var result = _validator.Validate(request);

            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public void Validate_WhenTitleOrDescriptionEmpty_ShouldFail()
        {
            var request = CreateValidRequest();
            request.Title = "";
            request.Description = "";

            var result = _validator.Validate(request);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateAuctionRequest.Title));
            result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateAuctionRequest.Description));
        }

        [Fact]
        public void Validate_WhenEndTimeBeforeStartTime_ShouldFail()
        {
            var request = CreateValidRequest();
            request.StartTime = DateTime.UtcNow.AddDays(2);
            request.EndTime = DateTime.UtcNow.AddDays(1);

            var result = _validator.Validate(request);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateAuctionRequest.EndTime))
                .Which.ErrorMessage.Should().Contain("End time must be after start time.");
        }

        [Fact]
        public void Validate_WhenImageUrlExceedsMaxLength_ShouldFail()
        {
            var request = CreateValidRequest();
            request.ImageUrl = "https://example.com/" + new string('a', 500);

            var result = _validator.Validate(request);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateAuctionRequest.ImageUrl));
        }
    }
}
