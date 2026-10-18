using System.ComponentModel.DataAnnotations;

namespace AuctionOx.Models
{
    public class Notification : BaseEntity
    {
        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;

        public NotificationType Type { get; set; }

        public int? AuctionItemId { get; set; }
        public AuctionItem? AuctionItem { get; set; }

        public bool IsRead { get; set; } = false;
    }
}
