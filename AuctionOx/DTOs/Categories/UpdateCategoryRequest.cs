using System.ComponentModel.DataAnnotations;

namespace AuctionOx.DTOs.Categories
{
    public class UpdateCategoryRequest
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        public string? Description { get; set; }
    }
}
