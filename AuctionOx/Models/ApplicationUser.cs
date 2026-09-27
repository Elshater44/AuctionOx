using Microsoft.AspNetCore.Identity;

namespace AuctionOx.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }

        public ICollection<AuctionItem> AuctionsCreated { get; set; } = new List<AuctionItem>();
        public ICollection<Bid> BidsPlaced { get; set; } = new List<Bid>();
    }
}
