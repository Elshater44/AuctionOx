using AuctionOx.DTOs.Auctions;
using FluentValidation;
using System;

namespace AuctionOx.Validators
{
    public class CreateAuctionRequestValidator : AbstractValidator<CreateAuctionRequest>
    {
        public CreateAuctionRequestValidator()
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).NotEmpty();
            RuleFor(x => x.StartingPrice).GreaterThan(0);
            
            RuleFor(x => x.BuyItNowPrice)
                .GreaterThan(x => x.StartingPrice).When(x => x.BuyItNowPrice.HasValue)
                .WithMessage("Buy It Now price must be greater than starting price.");

            RuleFor(x => x.StartTime).GreaterThanOrEqualTo(DateTime.UtcNow.AddMinutes(-5))
                .WithMessage("Start time cannot be in the past.");
                
            RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime)
                .WithMessage("End time must be after start time.");

            RuleFor(x => x.AntiSnipingMinutes).GreaterThanOrEqualTo(0);
        }
    }
}
