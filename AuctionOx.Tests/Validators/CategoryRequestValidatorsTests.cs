using AuctionOx.DTOs.Categories;
using AuctionOx.Validators;
using FluentAssertions;
using Xunit;

namespace AuctionOx.Tests.Validators
{
    public class CategoryRequestValidatorsTests
    {
        private readonly CreateCategoryRequestValidator _createValidator = new();
        private readonly UpdateCategoryRequestValidator _updateValidator = new();

        [Fact]
        public void CreateCategoryValidator_WhenValid_ShouldPass()
        {
            var request = new CreateCategoryRequest
            {
                Name = "Electronics",
                Description = "All electronic gadgets and hardware."
            };

            var result = _createValidator.Validate(request);
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public void CreateCategoryValidator_WhenNameEmptyOrTooLong_ShouldFail()
        {
            var empty = new CreateCategoryRequest { Name = "" };
            var result1 = _createValidator.Validate(empty);
            result1.IsValid.Should().BeFalse();

            var tooLong = new CreateCategoryRequest { Name = new string('A', 101) };
            var result2 = _createValidator.Validate(tooLong);
            result2.IsValid.Should().BeFalse();
        }

        [Fact]
        public void UpdateCategoryValidator_WhenValid_ShouldPass()
        {
            var request = new UpdateCategoryRequest
            {
                Name = "Antiques & Art",
                Description = "Vintage art pieces"
            };

            var result = _updateValidator.Validate(request);
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public void UpdateCategoryValidator_WhenNameEmpty_ShouldFail()
        {
            var request = new UpdateCategoryRequest { Name = "" };
            var result = _updateValidator.Validate(request);
            result.IsValid.Should().BeFalse();
        }
    }
}
