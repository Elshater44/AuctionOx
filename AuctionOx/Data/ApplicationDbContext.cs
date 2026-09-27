using AuctionOx.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AuctionOx.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<AuctionItem> AuctionItems { get; set; }
        public DbSet<Bid> Bids { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure Entity Relationships
            builder.Entity<AuctionItem>()
                .HasOne(a => a.Seller)
                .WithMany(u => u.AuctionsCreated)
                .HasForeignKey(a => a.SellerId);

            builder.Entity<Bid>()
                .HasOne(b => b.Bidder)
                .WithMany(u => u.BidsPlaced)
                .HasForeignKey(b => b.BidderId);

            builder.Entity<Bid>()
                .HasOne(b => b.AuctionItem)
                .WithMany(a => a.Bids)
                .HasForeignKey(b => b.AuctionItemId);

            // Configure Decimals
            builder.Entity<AuctionItem>()
                .Property(a => a.StartingPrice)
                .HasColumnType("decimal(18,2)");
                
            builder.Entity<AuctionItem>()
                .Property(a => a.CurrentPrice)
                .HasColumnType("decimal(18,2)");
                
            builder.Entity<AuctionItem>()
                .Property(a => a.BuyItNowPrice)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Bid>()
                .Property(b => b.BidAmount)
                .HasColumnType("decimal(18,2)");

            // Global Query Filters for Soft Deletes
            builder.Entity<ApplicationUser>().HasQueryFilter(u => !u.IsDeleted);
            builder.Entity<Category>().HasQueryFilter(c => !c.IsDeleted);
            builder.Entity<AuctionItem>().HasQueryFilter(a => !a.IsDeleted);
            builder.Entity<Bid>().HasQueryFilter(b => !b.IsDeleted);

            // Disable Cascade Deletes globally
            var cascadeFKs = builder.Model.GetEntityTypes()
                .SelectMany(t => t.GetForeignKeys())
                .Where(fk => !fk.IsOwnership && fk.DeleteBehavior == DeleteBehavior.Cascade);

            foreach (var fk in cascadeFKs)
            {
                fk.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            UpdateAuditFields();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            UpdateAuditFields();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void UpdateAuditFields()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entityEntry in entries)
            {
                if (entityEntry.Entity is BaseEntity baseEntity)
                {
                    if (entityEntry.State == EntityState.Added)
                    {
                        baseEntity.CreatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        // Don't update CreatedAt if we're just modifying
                        entityEntry.Property(nameof(BaseEntity.CreatedAt)).IsModified = false;
                        baseEntity.UpdatedAt = DateTime.UtcNow;
                    }
                }
                else if (entityEntry.Entity is ApplicationUser appUser)
                {
                    if (entityEntry.State == EntityState.Added)
                    {
                        appUser.CreatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        entityEntry.Property(nameof(ApplicationUser.CreatedAt)).IsModified = false;
                        appUser.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }
        }
    }
}
