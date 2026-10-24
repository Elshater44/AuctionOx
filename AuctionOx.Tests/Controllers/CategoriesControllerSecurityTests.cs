using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AuctionOx.Controllers;
using AuctionOx.DTOs.Categories;
using AuctionOx.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AuctionOx.Tests.Controllers
{
    public class CategoriesControllerSecurityTests
    {
        private readonly Mock<ICategoryService> _mockCategoryService;
        private readonly CategoriesController _controller;

        public CategoriesControllerSecurityTests()
        {
            _mockCategoryService = new Mock<ICategoryService>();
            _controller = new CategoriesController(_mockCategoryService.Object);
        }

        #region Security and Authorization Attribute Tests

        [Fact]
        public void CategoriesController_HasExpectedRoutingAndApiControllerAttributes()
        {
            var type = typeof(CategoriesController);

            type.GetCustomAttribute<ApiControllerAttribute>().Should().NotBeNull();
            var route = type.GetCustomAttribute<RouteAttribute>();
            route.Should().NotBeNull();
            route!.Template.Should().Be("api/[controller]");
        }

        [Theory]
        [InlineData(nameof(CategoriesController.Create))]
        [InlineData(nameof(CategoriesController.Update))]
        [InlineData(nameof(CategoriesController.Delete))]
        public void MutationEndpoints_AreProtectedWithAdminRoleAuthorization(string methodName)
        {
            var method = typeof(CategoriesController)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .FirstOrDefault(m => m.Name == methodName);

            method.Should().NotBeNull($"Method {methodName} must exist on CategoriesController");

            var authAttr = method!.GetCustomAttribute<AuthorizeAttribute>();
            authAttr.Should().NotBeNull($"Method {methodName} must be decorated with [Authorize]");
            authAttr!.Roles.Should().Be("Admin", $"Method {methodName} must require Admin role");
        }

        [Theory]
        [InlineData(nameof(CategoriesController.GetAll))]
        [InlineData(nameof(CategoriesController.GetById))]
        public void ReadEndpoints_DoNotRequireAdminRoleAuthorization(string methodName)
        {
            var method = typeof(CategoriesController)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .FirstOrDefault(m => m.Name == methodName);

            method.Should().NotBeNull();

            var authAttr = method!.GetCustomAttribute<AuthorizeAttribute>();
            authAttr.Should().BeNull($"Public read method {methodName} should not require [Authorize]");
        }

        #endregion

        #region Controller Action Response Tests

        [Fact]
        public async Task GetAll_ReturnsOk_WithCategoryList()
        {
            var categories = new List<CategoryDto>
            {
                new() { Id = 1, Name = "Electronics" },
                new() { Id = 2, Name = "Books" }
            };
            _mockCategoryService.Setup(s => s.GetAllAsync()).ReturnsAsync(categories);

            var result = await _controller.GetAll();

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(categories);
        }

        [Fact]
        public async Task GetById_WhenFound_ReturnsOk_WithCategory()
        {
            var category = new CategoryDto { Id = 5, Name = "Art" };
            _mockCategoryService.Setup(s => s.GetByIdAsync(5)).ReturnsAsync(category);

            var result = await _controller.GetById(5);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(category);
        }

        [Fact]
        public async Task GetById_WhenNotFound_ReturnsNotFound()
        {
            _mockCategoryService.Setup(s => s.GetByIdAsync(99)).ReturnsAsync((CategoryDto?)null);

            var result = await _controller.GetById(99);

            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task Create_ReturnsCreatedAtAction_WithNewCategory()
        {
            var request = new CreateCategoryRequest { Name = "Automobiles" };
            var created = new CategoryDto { Id = 10, Name = "Automobiles" };
            _mockCategoryService.Setup(s => s.CreateAsync(request)).ReturnsAsync(created);

            var result = await _controller.Create(request);

            var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdResult.ActionName.Should().Be(nameof(CategoriesController.GetById));
            createdResult.RouteValues!["id"].Should().Be(10);
            createdResult.Value.Should().BeEquivalentTo(created);
        }

        [Fact]
        public async Task Update_WhenFound_ReturnsOk_WithUpdatedCategory()
        {
            var request = new UpdateCategoryRequest { Name = "Updated Art" };
            var updated = new CategoryDto { Id = 5, Name = "Updated Art" };
            _mockCategoryService.Setup(s => s.UpdateAsync(5, request)).ReturnsAsync(updated);

            var result = await _controller.Update(5, request);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(updated);
        }

        [Fact]
        public async Task Update_WhenNotFound_ReturnsNotFound()
        {
            var request = new UpdateCategoryRequest { Name = "Missing" };
            _mockCategoryService.Setup(s => s.UpdateAsync(999, request)).ReturnsAsync((CategoryDto?)null);

            var result = await _controller.Update(999, request);

            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task Delete_WhenFound_ReturnsNoContent()
        {
            _mockCategoryService.Setup(s => s.DeleteAsync(5)).ReturnsAsync(true);

            var result = await _controller.Delete(5);

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task Delete_WhenNotFound_ReturnsNotFound()
        {
            _mockCategoryService.Setup(s => s.DeleteAsync(999)).ReturnsAsync(false);

            var result = await _controller.Delete(999);

            result.Should().BeOfType<NotFoundResult>();
        }

        #endregion
    }
}
