using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using AuctionOx.Controllers;
using AuctionOx.DTOs.Auth;
using AuctionOx.Exceptions;
using AuctionOx.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AuctionOx.Tests.Controllers
{
    public class AuthControllerSecurityTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly AuthController _controller;

        public AuthControllerSecurityTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _controller = new AuthController(_mockAuthService.Object);
        }

        #region Security & Role Attributes

        [Fact]
        public void CreateAdmin_HasAuthorizeAttributeWithAdminRole()
        {
            var method = typeof(AuthController).GetMethod(nameof(AuthController.CreateAdmin));
            method.Should().NotBeNull();

            var authAttr = method!.GetCustomAttribute<AuthorizeAttribute>();
            authAttr.Should().NotBeNull("CreateAdmin must be protected with [Authorize]");
            authAttr!.Roles.Should().Be("Admin", "Only users in the Admin role are permitted to create new admins");
        }

        [Fact]
        public void GetAdmins_HasAuthorizeAttributeWithAdminRole()
        {
            var method = typeof(AuthController).GetMethod(nameof(AuthController.GetAdmins));
            method.Should().NotBeNull();

            var authAttr = method!.GetCustomAttribute<AuthorizeAttribute>();
            authAttr.Should().NotBeNull("GetAdmins must be protected with [Authorize]");
            authAttr!.Roles.Should().Be("Admin", "Only users in the Admin role are permitted to list all admins");
        }

        [Fact]
        public void UpdateAdminRole_HasAuthorizeAttributeWithAdminRole()
        {
            var method = typeof(AuthController).GetMethod(nameof(AuthController.UpdateAdminRole));
            method.Should().NotBeNull();

            var authAttr = method!.GetCustomAttribute<AuthorizeAttribute>();
            authAttr.Should().NotBeNull("UpdateAdminRole must be protected with [Authorize]");
            authAttr!.Roles.Should().Be("Admin", "Only users in the Admin role are permitted to update admin roles");
        }

        [Fact]
        public void DeleteAdmin_HasAuthorizeAttributeWithAdminRole()
        {
            var method = typeof(AuthController).GetMethod(nameof(AuthController.DeleteAdmin));
            method.Should().NotBeNull();

            var authAttr = method!.GetCustomAttribute<AuthorizeAttribute>();
            authAttr.Should().NotBeNull("DeleteAdmin must be protected with [Authorize]");
            authAttr!.Roles.Should().Be("Admin", "Only users in the Admin role are permitted to delete admins");
        }

        [Fact]
        public void GetAllUsers_HasAuthorizeAttributeWithAdminRole()
        {
            var method = typeof(AuthController).GetMethod(nameof(AuthController.GetAllUsers));
            method.Should().NotBeNull();

            var authAttr = method!.GetCustomAttribute<AuthorizeAttribute>();
            authAttr.Should().NotBeNull("GetAllUsers must be protected with [Authorize]");
            authAttr!.Roles.Should().Be("Admin", "Only users in the Admin role are permitted to view user lists");
        }

        [Fact]
        public void SuspendUser_HasAuthorizeAttributeWithAdminRole()
        {
            var method = typeof(AuthController).GetMethod(nameof(AuthController.SuspendUser));
            method.Should().NotBeNull();

            var authAttr = method!.GetCustomAttribute<AuthorizeAttribute>();
            authAttr.Should().NotBeNull("SuspendUser must be protected with [Authorize]");
            authAttr!.Roles.Should().Be("Admin", "Only users in the Admin role are permitted to suspend users");
        }

        [Fact]
        public void BanUser_HasAuthorizeAttributeWithAdminRole()
        {
            var method = typeof(AuthController).GetMethod(nameof(AuthController.BanUser));
            method.Should().NotBeNull();

            var authAttr = method!.GetCustomAttribute<AuthorizeAttribute>();
            authAttr.Should().NotBeNull("BanUser must be protected with [Authorize]");
            authAttr!.Roles.Should().Be("Admin", "Only users in the Admin role are permitted to ban users");
        }

        [Fact]
        public void ReactivateUser_HasAuthorizeAttributeWithAdminRole()
        {
            var method = typeof(AuthController).GetMethod(nameof(AuthController.ReactivateUser));
            method.Should().NotBeNull();

            var authAttr = method!.GetCustomAttribute<AuthorizeAttribute>();
            authAttr.Should().NotBeNull("ReactivateUser must be protected with [Authorize]");
            authAttr!.Roles.Should().Be("Admin", "Only users in the Admin role are permitted to reactivate users");
        }

        [Fact]
        public void DeleteUser_HasAuthorizeAttributeWithAdminRole()
        {
            var method = typeof(AuthController).GetMethod(nameof(AuthController.DeleteUser));
            method.Should().NotBeNull();

            var authAttr = method!.GetCustomAttribute<AuthorizeAttribute>();
            authAttr.Should().NotBeNull("DeleteUser must be protected with [Authorize]");
            authAttr!.Roles.Should().Be("Admin", "Only users in the Admin role are permitted to delete users");
        }

        #endregion

        #region Action Behavior

        [Fact]
        public async Task CreateAdmin_WhenValid_ReturnsOkWithAdminDto()
        {
            var request = new CreateAdminRequest
            {
                Email = "newadmin@auctionox.com",
                Password = "AdminSecurePassword123!",
                FirstName = "Admin",
                LastName = "User"
            };

            var expectedDto = new AdminUserDto
            {
                Id = "admin-100",
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                CreatedAt = DateTime.UtcNow,
                Roles = new List<string> { "Admin" }
            };

            _mockAuthService.Setup(s => s.CreateAdminAsync(request))
                .ReturnsAsync(expectedDto);

            var result = await _controller.CreateAdmin(request);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(expectedDto);
        }

        [Fact]
        public async Task CreateAdmin_WhenServiceThrowsBusinessRuleException_ReturnsBadRequest()
        {
            var request = new CreateAdminRequest
            {
                Email = "existing@auctionox.com",
                Password = "AdminSecurePassword123!",
                FirstName = "Admin",
                LastName = "User"
            };

            _mockAuthService.Setup(s => s.CreateAdminAsync(request))
                .ThrowsAsync(new BusinessRuleException("A user with this email already exists."));

            var result = await _controller.CreateAdmin(request);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GetAdmins_ReturnsOkWithAdminList()
        {
            var adminList = new List<AdminUserDto>
            {
                new AdminUserDto { Id = "admin-1", Email = "admin1@auctionox.com", FirstName = "Admin", LastName = "One" }
            };

            _mockAuthService.Setup(s => s.GetAdminsAsync())
                .ReturnsAsync(adminList);

            var result = await _controller.GetAdmins();

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(adminList);
        }

        [Fact]
        public async Task UpdateAdminRole_WhenValid_ReturnsOk()
        {
            var controller = new AuthController(_mockAuthService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                        {
                            new Claim(ClaimTypes.NameIdentifier, "caller-admin-id")
                        }))
                    }
                }
            };

            var request = new UpdateAdminRoleRequest { Role = "User" };
            var expectedDto = new AdminUserDto { Id = "target-1", Roles = new List<string> { "User" } };

            _mockAuthService.Setup(s => s.UpdateAdminRoleAsync("target-1", "User", "caller-admin-id"))
                .ReturnsAsync(expectedDto);

            var result = await controller.UpdateAdminRole("target-1", request);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(expectedDto);
        }

        [Fact]
        public async Task UpdateUserRole_WhenAdminPromotesUser_ReturnsOk()
        {
            var controller = new AuthController(_mockAuthService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                        {
                            new Claim(ClaimTypes.NameIdentifier, "caller-admin-id")
                        }))
                    }
                }
            };

            var request = new UpdateAdminRoleRequest { Role = "Admin" };
            var expectedDto = new AdminUserDto { Id = "target-user-1", Roles = new List<string> { "Admin", "User" } };

            _mockAuthService.Setup(s => s.UpdateAdminRoleAsync("target-user-1", "Admin", "caller-admin-id"))
                .ReturnsAsync(expectedDto);

            var result = await controller.UpdateUserRole("target-user-1", request);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(expectedDto);
        }

        [Fact]
        public async Task DeleteAdmin_WhenValid_ReturnsNoContent()
        {
            var controller = new AuthController(_mockAuthService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                        {
                            new Claim(ClaimTypes.NameIdentifier, "caller-admin-id")
                        }))
                    }
                }
            };

            _mockAuthService.Setup(s => s.DeleteAdminAsync("target-1", "caller-admin-id"))
                .Returns(Task.CompletedTask);

            var result = await controller.DeleteAdmin("target-1");

            result.Should().BeOfType<NoContentResult>();
        }

        #endregion
    }
}
