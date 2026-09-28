using System;
using AuctionOx.DTOs.Auctions;
using FluentValidation;

namespace AuctionOx.Validators
{
    public class UpdateAuctionRequestValidator : AbstractValidator<UpdateAuctionRequest>
    {
        public UpdateAuctionRequestValidator()
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).NotEmpty();
            
            RuleFor(x => x.StartTime).GreaterThanOrEqualTo(DateTime.UtcNow.AddMinutes(-5))
                .WithMessage("Start time cannot be in the past.");
                
            RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime)
                .WithMessage("End time must be after start time.");
                
            RuleFor(x => x.ImageUrl).MaximumLength(500);
        }
    }
}
