using System.ComponentModel.DataAnnotations;

namespace AuctionOx.Models
{
    public class Category : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        public string? Description { get; set; }

        public ICollection<AuctionItem> AuctionItems { get; set; } = new List<AuctionItem>();
    }
}
