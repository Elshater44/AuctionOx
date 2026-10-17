using System;
using System.Linq;
using System.Threading.Tasks;
using AuctionOx.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using Xunit;

namespace AuctionOx.Tests.Integration
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly MsSqlContainer _dbContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();

        public async Task InitializeAsync()
        {
            // 1. Boot isolated SQL Server Docker container
            await _dbContainer.StartAsync();

            // 2. Run EF Core migrations against the real container database & seed default data
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Database.MigrateAsync();
            await DatabaseSeeder.SeedRolesAsync(scope.ServiceProvider);

            if (!await context.Categories.AnyAsync())
            {
                context.Categories.Add(new AuctionOx.Models.Category
                {
                    Name = "General",
                    Description = "General test category"
                });
                await context.SaveChangesAsync();
            }
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                // Remove existing SQL Server registration
                var descriptors = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                    d.ServiceType == typeof(ApplicationDbContext)).ToList();

                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }

                // Point EF Core to the Testcontainer instance
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseSqlServer(_dbContainer.GetConnectionString());
                });
            });
        }

        public new async Task DisposeAsync()
        {
            await _dbContainer.DisposeAsync();
        }
    }
}
