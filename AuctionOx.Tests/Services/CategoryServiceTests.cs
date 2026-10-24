using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuctionOx.Data;
using AuctionOx.DTOs.Categories;
using AuctionOx.Mappings;
using AuctionOx.Models;
using AuctionOx.Repositories.Implementations;
using AuctionOx.Services.Implementations;
using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AuctionOx.Tests.Services
{
    public class CategoryServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly UnitOfWork _unitOfWork;
        private readonly CategoryService _categoryService;

        private static IMapper BuildMapper()
        {
            var configExpression = new MapperConfigurationExpression();
            configExpression.AddProfile<MappingProfile>();
            var config = new MapperConfiguration(configExpression, NullLoggerFactory.Instance);
            return config.CreateMapper();
        }

        public CategoryServiceTests()
        {
            _mapper = BuildMapper();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"CategoryServiceDb_{Guid.NewGuid()}")
                .Options;

            _dbContext = new ApplicationDbContext(options);
            _unitOfWork = new UnitOfWork(_dbContext);
            _categoryService = new CategoryService(_unitOfWork, _mapper);
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }

        [Fact]
        public async Task GetAllAsync_WhenCategoriesExist_ReturnsMappedCategoryDtos()
        {
            // Arrange
            _dbContext.Categories.AddRange(
                new Category { Name = "Electronics", IsDeleted = false },
                new Category { Name = "Antiques", IsDeleted = false }
            );
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _categoryService.GetAllAsync();

            // Assert
            result.Should().HaveCount(2);
            result.Select(c => c.Name).Should().Contain(new[] { "Electronics", "Antiques" });
        }

        [Fact]
        public async Task GetByIdAsync_WhenCategoryExists_ReturnsCategoryDto()
        {
            // Arrange
            var category = new Category { Name = "Collectibles", IsDeleted = false };
            _dbContext.Categories.Add(category);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _categoryService.GetByIdAsync(category.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(category.Id);
            result.Name.Should().Be("Collectibles");
        }

        [Fact]
        public async Task GetByIdAsync_WhenCategoryDoesNotExist_ReturnsNull()
        {
            // Act
            var result = await _categoryService.GetByIdAsync(9999);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task CreateAsync_ValidRequest_CreatesEntityAndReturnsDto()
        {
            // Arrange
            var request = new CreateCategoryRequest { Name = "Fine Art" };

            // Act
            var result = await _categoryService.CreateAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().BeGreaterThan(0);
            result.Name.Should().Be("Fine Art");

            var inDb = await _dbContext.Categories.FindAsync(result.Id);
            inDb.Should().NotBeNull();
            inDb!.Name.Should().Be("Fine Art");
            inDb.IsDeleted.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateAsync_WhenCategoryExists_UpdatesEntityAndReturnsDto()
        {
            // Arrange
            var category = new Category { Name = "Jewelery", IsDeleted = false };
            _dbContext.Categories.Add(category);
            await _dbContext.SaveChangesAsync();

            var updateRequest = new UpdateCategoryRequest { Name = "Jewelry & Watches" };

            // Act
            var result = await _categoryService.UpdateAsync(category.Id, updateRequest);

            // Assert
            result.Should().NotBeNull();
            result!.Name.Should().Be("Jewelry & Watches");

            var inDb = await _dbContext.Categories.FindAsync(category.Id);
            inDb!.Name.Should().Be("Jewelry & Watches");
        }

        [Fact]
        public async Task UpdateAsync_WhenCategoryDoesNotExist_ReturnsNull()
        {
            // Arrange
            var updateRequest = new UpdateCategoryRequest { Name = "NonExistent" };

            // Act
            var result = await _categoryService.UpdateAsync(9999, updateRequest);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task DeleteAsync_WhenCategoryExists_SetsIsDeletedTrueAndReturnsTrue()
        {
            // Arrange
            var category = new Category { Name = "Sports Memorabilia", IsDeleted = false };
            _dbContext.Categories.Add(category);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _categoryService.DeleteAsync(category.Id);

            // Assert
            result.Should().BeTrue();

            // Query with IgnoreQueryFilters to inspect the soft-deleted state
            var inDb = await _dbContext.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == category.Id);
            inDb.Should().NotBeNull();
            inDb!.IsDeleted.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteAsync_WhenCategoryDoesNotExist_ReturnsFalse()
        {
            // Act
            var result = await _categoryService.DeleteAsync(9999);

            // Assert
            result.Should().BeFalse();
        }
    }
}
