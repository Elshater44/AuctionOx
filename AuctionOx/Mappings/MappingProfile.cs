using AutoMapper;
using AuctionOx.DTOs.Auctions;
using AuctionOx.DTOs.Auth;
using AuctionOx.DTOs.Bids;
using AuctionOx.DTOs.Categories;
using AuctionOx.Models;

namespace AuctionOx.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Category Mappings
            CreateMap<Category, CategoryDto>();
            CreateMap<CreateCategoryRequest, Category>();
            CreateMap<UpdateCategoryRequest, Category>();

            // Auth Mappings
            CreateMap<ApplicationUser, UserProfileDto>();

            // Bid Mappings
            CreateMap<Bid, BidDto>()
                .ForMember(dest => dest.BidderName, 
                           opt => opt.MapFrom(src => $"{src.Bidder!.FirstName} {src.Bidder!.LastName}".Trim()));

            // Auction Mappings
            CreateMap<AuctionItem, AuctionItemDto>()
                .ForMember(dest => dest.SellerName, 
                           opt => opt.MapFrom(src => $"{src.Seller!.FirstName} {src.Seller!.LastName}".Trim()))
                .ForMember(dest => dest.CategoryName, 
                           opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : "Unknown"))
                .ForMember(dest => dest.BidCount, 
                           opt => opt.MapFrom(src => src.Bids != null ? src.Bids.Count : 0));

            CreateMap<AuctionItem, AuctionDetailDto>()
                .IncludeBase<AuctionItem, AuctionItemDto>()
                .ForMember(dest => dest.RecentBids, opt => opt.Ignore()); // We map RecentBids manually or handle it in service

            CreateMap<CreateAuctionRequest, AuctionItem>()
                .ForMember(dest => dest.CurrentPrice, opt => opt.MapFrom(src => src.StartingPrice));

            CreateMap<UpdateAuctionRequest, AuctionItem>();
        }
    }
}
