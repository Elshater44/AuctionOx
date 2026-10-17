using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AuctionOx.Models;
using System;
using System.Threading.Tasks;

namespace AuctionOx.Data
{
    public static class DatabaseSeeder
    {
        public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();

            string[] roleNames = { "Admin", "User" };

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Seed Default Admin User
            var adminEmail = "admin@auctionox.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                var newAdmin = new ApplicationUser
                {
                    Id = "a0000000-0000-0000-0000-000000000001",
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "System",
                    LastName = "Administrator",
                    EmailConfirmed = true
                };

                var adminPassword = configuration["AdminPassword"] ?? "AdminPassword123!"; // fallback for local dev if not set
                var createPowerUser = await userManager.CreateAsync(newAdmin, adminPassword);
                if (createPowerUser.Succeeded)
                {
                    await userManager.AddToRoleAsync(newAdmin, "Admin");
                    adminUser = newAdmin;
                }
            }

            // Seed Default Regular User
            var regularEmail = "user@auctionox.com";
            var regularUser = await userManager.FindByEmailAsync(regularEmail);

            if (regularUser == null)
            {
                var newUser = new ApplicationUser
                {
                    Id = "6b69d6df-cc30-4911-a4af-644efd3e4ada",
                    UserName = regularEmail,
                    Email = regularEmail,
                    FirstName = "Regular",
                    LastName = "User",
                    EmailConfirmed = true
                };

                var userPassword = configuration["UserPassword"] ?? "UserPassword123!";
                var createUser = await userManager.CreateAsync(newUser, userPassword);
                if (createUser.Succeeded)
                {
                    await userManager.AddToRoleAsync(newUser, "User");
                    regularUser = newUser;
                }
            }

            // 1. Seed 4 Categories
            if (!await dbContext.Categories.AnyAsync())
            {
                var categories = new[]
                {
                    new Category
                    {
                        Name = "Electronics & Gadgets",
                        Description = "Smartphones, gaming consoles, pro audio gear, and cutting-edge tech."
                    },
                    new Category
                    {
                        Name = "Collectibles & Vintage",
                        Description = "Rare comics, vintage cameras, vinyl records, and historic memorabilia."
                    },
                    new Category
                    {
                        Name = "Watches & Luxury",
                        Description = "Swiss luxury mechanical watches, fine jewelry, and collector timepieces."
                    },
                    new Category
                    {
                        Name = "Art & Design",
                        Description = "Original oil paintings, limited gallery prints, and sculptures."
                    }
                };

                await dbContext.Categories.AddRangeAsync(categories);
                await dbContext.SaveChangesAsync();
            }

            // 2. Seed 3 Active Auction Items
            if (!await dbContext.AuctionItems.AnyAsync())
            {
                var electronics = await dbContext.Categories.FirstOrDefaultAsync(c => c.Name == "Electronics & Gadgets");
                var watches = await dbContext.Categories.FirstOrDefaultAsync(c => c.Name == "Watches & Luxury");
                var vintage = await dbContext.Categories.FirstOrDefaultAsync(c => c.Name == "Collectibles & Vintage");

                var defaultSellerId = adminUser?.Id ?? regularUser?.Id;
                if (!string.IsNullOrEmpty(defaultSellerId))
                {
                    var items = new[]
                    {
                        new AuctionItem
                        {
                            Title = "Sony PlayStation 5 Pro Console (2TB Edition)",
                            Description = "Brand new sealed PlayStation 5 Pro with dual DualSense wireless controllers, 2TB SSD, and 4K 120Hz support. Full manufacturer warranty included.",
                            StartingPrice = 499.00m,
                            CurrentPrice = 550.00m,
                            BuyItNowPrice = 850.00m,
                            StartTime = DateTime.UtcNow.AddHours(-12),
                            EndTime = DateTime.UtcNow.AddDays(2).AddHours(4),
                            AntiSnipingMinutes = 5,
                            Status = ItemStatus.Active,
                            ImageUrl = "https://images.unsplash.com/photo-1606813907291-d86efa9b94db?w=800&auto=format&fit=crop&q=80",
                            SellerId = adminUser?.Id ?? defaultSellerId,
                            CategoryId = electronics?.Id ?? 1
                        },
                        new AuctionItem
                        {
                            Title = "Rolex Submariner Date 41mm (Ref. 126610LN)",
                            Description = "Unworn 2024 Rolex Submariner Date with Cerachrom ceramic bezel and Oystersteel bracelet. Complete set with presentation box, papers, and valid warranty card.",
                            StartingPrice = 9500.00m,
                            CurrentPrice = 11200.00m,
                            BuyItNowPrice = 14500.00m,
                            StartTime = DateTime.UtcNow.AddDays(-1),
                            EndTime = DateTime.UtcNow.AddHours(5), // Ending soon for countdown demonstration
                            AntiSnipingMinutes = 5,
                            Status = ItemStatus.Active,
                            ImageUrl = "https://images.unsplash.com/photo-1522335789203-aabd1fc54bc9?w=800&auto=format&fit=crop&q=80",
                            SellerId = regularUser?.Id ?? defaultSellerId,
                            CategoryId = watches?.Id ?? 1
                        },
                        new AuctionItem
                        {
                            Title = "Vintage 1977 Hasselblad 500C/M Medium Format Camera",
                            Description = "Classic medium format camera with Carl Zeiss Planar 80mm f/2.8 T* lens, A12 120 film magazine, and waist-level finder. Thoroughly inspected and shutter calibrated.",
                            StartingPrice = 1200.00m,
                            CurrentPrice = 1350.00m,
                            BuyItNowPrice = 2100.00m,
                            StartTime = DateTime.UtcNow.AddHours(-6),
                            EndTime = DateTime.UtcNow.AddDays(4),
                            AntiSnipingMinutes = 5,
                            Status = ItemStatus.Active,
                            ImageUrl = "https://images.unsplash.com/photo-1526170375885-4d8ecf77b99f?w=800&auto=format&fit=crop&q=80",
                            SellerId = adminUser?.Id ?? defaultSellerId,
                            CategoryId = vintage?.Id ?? 1
                        }
                    };

                    await dbContext.AuctionItems.AddRangeAsync(items);
                    await dbContext.SaveChangesAsync();

                    // Seed sample bid history
                    if (adminUser != null && regularUser != null)
                    {
                        var bids = new[]
                        {
                            new Bid
                            {
                                AuctionItemId = items[0].Id,
                                BidderId = regularUser.Id,
                                BidAmount = 550.00m,
                                BidTime = DateTime.UtcNow.AddHours(-2),
                                IsWinningBid = false
                            },
                            new Bid
                            {
                                AuctionItemId = items[1].Id,
                                BidderId = adminUser.Id,
                                BidAmount = 10500.00m,
                                BidTime = DateTime.UtcNow.AddHours(-8),
                                IsWinningBid = false
                            },
                            new Bid
                            {
                                AuctionItemId = items[1].Id,
                                BidderId = regularUser.Id,
                                BidAmount = 11200.00m,
                                BidTime = DateTime.UtcNow.AddHours(-1),
                                IsWinningBid = false
                            },
                            new Bid
                            {
                                AuctionItemId = items[2].Id,
                                BidderId = regularUser.Id,
                                BidAmount = 1350.00m,
                                BidTime = DateTime.UtcNow.AddHours(-3),
                                IsWinningBid = false
                            }
                        };
                        await dbContext.Bids.AddRangeAsync(bids);
                        await dbContext.SaveChangesAsync();
                    }
                }
            }
        }
    }
}
