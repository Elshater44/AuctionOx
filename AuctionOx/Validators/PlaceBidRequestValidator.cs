using AuctionOx.DTOs.Bids;
using FluentValidation;

namespace AuctionOx.Validators
{
    public class PlaceBidRequestValidator : AbstractValidator<PlaceBidRequest>
    {
        public PlaceBidRequestValidator()
        {
            RuleFor(x => x.BidAmount).GreaterThan(0).WithMessage("Bid amount must be greater than zero.");
        }
    }
}
