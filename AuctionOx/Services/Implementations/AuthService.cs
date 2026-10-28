using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AuctionOx.DTOs.Auth;
using AuctionOx.Exceptions;
using AuctionOx.Models;
using AuctionOx.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace AuctionOx.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthService> _logger;

        public AuthService(UserManager<ApplicationUser> userManager, IConfiguration config, ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _config = config;
            _logger = logger;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Failed registration attempt for email {Email}. Errors: {Errors}", request.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                throw new BusinessRuleException(string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            await _userManager.AddToRoleAsync(user, "User");
            _logger.LogInformation("User {Email} registered successfully.", request.Email);

            return await GenerateTokenAsync(user);
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
            {
                _logger.LogWarning("Failed login attempt for email {Email}.", request.Email);
                throw new BusinessRuleException("Invalid credentials");
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                _logger.LogWarning("Login attempt for locked out user {Email}.", request.Email);
                var isPermanent = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow.AddYears(10);
                var message = isPermanent
                    ? "This account has been permanently banned by an administrator."
                    : $"This account is temporarily suspended until {user.LockoutEnd:g} UTC. Please contact support.";
                throw new BusinessRuleException(message);
            }

            _logger.LogInformation("User {Email} logged in successfully.", request.Email);
            return await GenerateTokenAsync(user);
        }

        public async Task<UserProfileDto?> GetProfileAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            return new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                CreatedAt = user.CreatedAt
            };
        }

        public async Task<AdminUserDto> CreateAdminAsync(CreateAdminRequest request)
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("Failed admin creation attempt. Email {Email} is already registered.", request.Email);
                throw new BusinessRuleException("A user with this email already exists.");
            }

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Failed admin creation for email {Email}. Errors: {Errors}", request.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                throw new BusinessRuleException(string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            var roleResult = await _userManager.AddToRoleAsync(user, "Admin");
            if (!roleResult.Succeeded)
            {
                _logger.LogError("Failed assigning Admin role to newly created user {Email}.", request.Email);
                throw new BusinessRuleException("Failed to assign Administrator role.");
            }

            _logger.LogInformation("Admin user {Email} created successfully.", request.Email);

            return new AdminUserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                CreatedAt = user.CreatedAt == default ? DateTime.UtcNow : user.CreatedAt,
                Roles = new List<string> { "Admin" }
            };
        }

        public async Task<IEnumerable<AdminUserDto>> GetAdminsAsync()
        {
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var result = new List<AdminUserDto>();

            foreach (var user in admins.Where(u => !u.IsDeleted))
            {
                result.Add(new AdminUserDto
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    CreatedAt = user.CreatedAt,
                    Roles = new List<string> { "Admin" }
                });
            }

            return result.OrderByDescending(u => u.CreatedAt);
        }

        public async Task<AdminUserDto> UpdateAdminRoleAsync(string targetUserId, string newRole, string currentUserId)
        {
            var targetUser = await _userManager.FindByIdAsync(targetUserId);
            if (targetUser == null || targetUser.IsDeleted)
            {
                throw new BusinessRuleException("User account not found.");
            }

            if (string.Equals(targetUser.Id, currentUserId, StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessRuleException("You cannot modify your own administrator role.");
            }

            if (string.Equals(targetUser.Email, "admin@auctionox.com", StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessRuleException("The root system administrator account cannot be demoted or modified.");
            }

            if (!string.Equals(newRole, "Admin", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(newRole, "User", StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessRuleException("Invalid role specified. Supported roles are 'Admin' or 'User'.");
            }

            var currentRoles = await _userManager.GetRolesAsync(targetUser);

            if (string.Equals(newRole, "User", StringComparison.OrdinalIgnoreCase))
            {
                if (currentRoles.Contains("Admin"))
                {
                    var allAdmins = await _userManager.GetUsersInRoleAsync("Admin");
                    if (allAdmins.Count(a => !a.IsDeleted) <= 1)
                    {
                        throw new BusinessRuleException("Cannot demote the only remaining administrator on the platform.");
                    }

                    var removeResult = await _userManager.RemoveFromRoleAsync(targetUser, "Admin");
                    if (!removeResult.Succeeded)
                    {
                        throw new BusinessRuleException("Failed to remove Admin role.");
                    }
                }

                if (!currentRoles.Contains("User"))
                {
                    await _userManager.AddToRoleAsync(targetUser, "User");
                }
            }
            else if (string.Equals(newRole, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                if (targetUser.LockoutEnd.HasValue && targetUser.LockoutEnd.Value > DateTimeOffset.UtcNow)
                {
                    throw new BusinessRuleException("Cannot grant administrator privileges to a suspended or banned user.");
                }

                if (!currentRoles.Contains("Admin"))
                {
                    var addResult = await _userManager.AddToRoleAsync(targetUser, "Admin");
                    if (!addResult.Succeeded)
                    {
                        throw new BusinessRuleException("Failed to assign Admin role.");
                    }
                }
            }

            // Invalidate existing sessions and refresh tokens immediately
            targetUser.RefreshToken = null;
            targetUser.RefreshTokenExpiryTime = null;
            await _userManager.UpdateSecurityStampAsync(targetUser);
            await _userManager.UpdateAsync(targetUser);

            _logger.LogInformation("Role for user {Email} updated to {Role} by admin {CurrentUserId}.", targetUser.Email, newRole, currentUserId);

            var updatedRoles = await _userManager.GetRolesAsync(targetUser);
            return new AdminUserDto
            {
                Id = targetUser.Id,
                Email = targetUser.Email ?? string.Empty,
                FirstName = targetUser.FirstName,
                LastName = targetUser.LastName,
                CreatedAt = targetUser.CreatedAt,
                Roles = updatedRoles
            };
        }

        public async Task DeleteAdminAsync(string targetUserId, string currentUserId)
        {
            var targetUser = await _userManager.FindByIdAsync(targetUserId);
            if (targetUser == null || targetUser.IsDeleted)
            {
                throw new BusinessRuleException("Administrator account not found.");
            }

            if (string.Equals(targetUser.Id, currentUserId, StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessRuleException("You cannot delete your own administrator account.");
            }

            if (string.Equals(targetUser.Email, "admin@auctionox.com", StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessRuleException("The root system administrator account cannot be deleted.");
            }

            var allAdmins = await _userManager.GetUsersInRoleAsync("Admin");
            if (allAdmins.Count(a => !a.IsDeleted) <= 1)
            {
                throw new BusinessRuleException("Cannot delete the only remaining administrator on the platform.");
            }

            // Invalidate session immediately
            targetUser.RefreshToken = null;
            targetUser.RefreshTokenExpiryTime = null;
            targetUser.IsDeleted = true;
            await _userManager.UpdateSecurityStampAsync(targetUser);

            var deleteResult = await _userManager.DeleteAsync(targetUser);
            if (!deleteResult.Succeeded)
            {
                throw new BusinessRuleException(string.Join(", ", deleteResult.Errors.Select(e => e.Description)));
            }

            _logger.LogInformation("Administrator {Email} deleted by admin {CurrentUserId}.", targetUser.Email, currentUserId);
        }

        public async Task<IEnumerable<UserManagementDto>> GetAllUsersAsync(string? roleFilter = null, string? search = null)
        {
            var query = _userManager.Users.Where(u => !u.IsDeleted);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(u =>
                    (u.Email != null && u.Email.ToLower().Contains(s)) ||
                    u.FirstName.ToLower().Contains(s) ||
                    u.LastName.ToLower().Contains(s));
            }

            var users = query.OrderByDescending(u => u.CreatedAt).ToList();
            var result = new List<UserManagementDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                if (!string.IsNullOrWhiteSpace(roleFilter) && !roles.Any(r => string.Equals(r, roleFilter, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var isLockedOut = await _userManager.IsLockedOutAsync(user);

                result.Add(new UserManagementDto
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    CreatedAt = user.CreatedAt,
                    Roles = roles,
                    IsLockedOut = isLockedOut,
                    LockoutEnd = user.LockoutEnd
                });
            }

            return result;
        }

        public async Task<UserManagementDto> SuspendUserAsync(string targetUserId, int days, string currentUserId, string? reason = null)
        {
            var targetUser = await _userManager.FindByIdAsync(targetUserId);
            if (targetUser == null || targetUser.IsDeleted)
            {
                throw new BusinessRuleException("User account not found.");
            }

            if (string.Equals(targetUser.Id, currentUserId, StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessRuleException("You cannot suspend your own account.");
            }

            if (string.Equals(targetUser.Email, "admin@auctionox.com", StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessRuleException("The root system administrator account cannot be suspended.");
            }

            var roles = (await _userManager.GetRolesAsync(targetUser)) ?? new List<string>();
            if (roles.Contains("Admin"))
            {
                var allAdmins = await _userManager.GetUsersInRoleAsync("Admin");
                if (allAdmins.Count(a => !a.IsDeleted && !(a.LockoutEnd.HasValue && a.LockoutEnd.Value > DateTimeOffset.UtcNow)) <= 1)
                {
                    throw new BusinessRuleException("Cannot suspend the only remaining active administrator on the platform.");
                }
            }

            if (days <= 0) days = 7;
            var lockoutEnd = DateTimeOffset.UtcNow.AddDays(days);

            await _userManager.SetLockoutEnabledAsync(targetUser, true);
            await _userManager.SetLockoutEndDateAsync(targetUser, lockoutEnd);

            // Invalidate current refresh tokens and active JWT sessions
            targetUser.RefreshToken = null;
            targetUser.RefreshTokenExpiryTime = null;
            await _userManager.UpdateSecurityStampAsync(targetUser);
            await _userManager.UpdateAsync(targetUser);

            _logger.LogWarning("User {Email} suspended for {Days} days by admin {CurrentUserId}. Reason: {Reason}", targetUser.Email, days, currentUserId, reason ?? "No reason specified");

            return new UserManagementDto
            {
                Id = targetUser.Id,
                Email = targetUser.Email ?? string.Empty,
                FirstName = targetUser.FirstName,
                LastName = targetUser.LastName,
                CreatedAt = targetUser.CreatedAt,
                Roles = roles,
                IsLockedOut = true,
                LockoutEnd = lockoutEnd
            };
        }

        public async Task<UserManagementDto> BanUserAsync(string targetUserId, string currentUserId, string? reason = null)
        {
            var targetUser = await _userManager.FindByIdAsync(targetUserId);
            if (targetUser == null || targetUser.IsDeleted)
            {
                throw new BusinessRuleException("User account not found.");
            }

            if (string.Equals(targetUser.Id, currentUserId, StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessRuleException("You cannot ban your own account.");
            }

            if (string.Equals(targetUser.Email, "admin@auctionox.com", StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessRuleException("The root system administrator account cannot be banned.");
            }

            var roles = (await _userManager.GetRolesAsync(targetUser)) ?? new List<string>();
            if (roles.Contains("Admin"))
            {
                var allAdmins = await _userManager.GetUsersInRoleAsync("Admin");
                if (allAdmins.Count(a => !a.IsDeleted && !(a.LockoutEnd.HasValue && a.LockoutEnd.Value > DateTimeOffset.UtcNow)) <= 1)
                {
                    throw new BusinessRuleException("Cannot ban the only remaining active administrator on the platform.");
                }
            }

            var permanentBanEnd = DateTimeOffset.UtcNow.AddYears(100);

            await _userManager.SetLockoutEnabledAsync(targetUser, true);
            await _userManager.SetLockoutEndDateAsync(targetUser, permanentBanEnd);

            // Invalidate current refresh tokens and active JWT sessions
            targetUser.RefreshToken = null;
            targetUser.RefreshTokenExpiryTime = null;
            await _userManager.UpdateSecurityStampAsync(targetUser);
            await _userManager.UpdateAsync(targetUser);

            _logger.LogWarning("User {Email} permanently banned by admin {CurrentUserId}. Reason: {Reason}", targetUser.Email, currentUserId, reason ?? "No reason specified");

            return new UserManagementDto
            {
                Id = targetUser.Id,
                Email = targetUser.Email ?? string.Empty,
                FirstName = targetUser.FirstName,
                LastName = targetUser.LastName,
                CreatedAt = targetUser.CreatedAt,
                Roles = roles,
                IsLockedOut = true,
                LockoutEnd = permanentBanEnd
            };
        }

        public async Task<UserManagementDto> ReactivateUserAsync(string targetUserId, string currentUserId)
        {
            var targetUser = await _userManager.FindByIdAsync(targetUserId);
            if (targetUser == null || targetUser.IsDeleted)
            {
                throw new BusinessRuleException("User account not found.");
            }

            await _userManager.SetLockoutEndDateAsync(targetUser, null);
            await _userManager.ResetAccessFailedCountAsync(targetUser);
            await _userManager.UpdateSecurityStampAsync(targetUser);
            await _userManager.UpdateAsync(targetUser);

            _logger.LogInformation("User {Email} reactivated (suspension/ban lifted) by admin {CurrentUserId}.", targetUser.Email, currentUserId);

            var roles = await _userManager.GetRolesAsync(targetUser);
            return new UserManagementDto
            {
                Id = targetUser.Id,
                Email = targetUser.Email ?? string.Empty,
                FirstName = targetUser.FirstName,
                LastName = targetUser.LastName,
                CreatedAt = targetUser.CreatedAt,
                Roles = roles,
                IsLockedOut = false,
                LockoutEnd = null
            };
        }

        public async Task DeleteUserAccountAsync(string targetUserId, string currentUserId)
        {
            var targetUser = await _userManager.FindByIdAsync(targetUserId);
            if (targetUser == null || targetUser.IsDeleted)
            {
                throw new BusinessRuleException("User account not found.");
            }

            if (string.Equals(targetUser.Id, currentUserId, StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessRuleException("You cannot delete your own account.");
            }

            if (string.Equals(targetUser.Email, "admin@auctionox.com", StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessRuleException("The root system administrator account cannot be deleted.");
            }

            var roles = (await _userManager.GetRolesAsync(targetUser)) ?? new List<string>();
            if (roles.Contains("Admin"))
            {
                var allAdmins = await _userManager.GetUsersInRoleAsync("Admin");
                if (allAdmins.Count(a => !a.IsDeleted && !(a.LockoutEnd.HasValue && a.LockoutEnd.Value > DateTimeOffset.UtcNow)) <= 1)
                {
                    throw new BusinessRuleException("Cannot delete the only remaining administrator on the platform.");
                }
            }

            // Invalidate session immediately
            targetUser.RefreshToken = null;
            targetUser.RefreshTokenExpiryTime = null;
            targetUser.IsDeleted = true;
            await _userManager.UpdateSecurityStampAsync(targetUser);

            var deleteResult = await _userManager.DeleteAsync(targetUser);
            if (!deleteResult.Succeeded)
            {
                throw new BusinessRuleException(string.Join(", ", deleteResult.Errors.Select(e => e.Description)));
            }

            _logger.LogInformation("User {Email} deleted by admin {CurrentUserId}.", targetUser.Email, currentUserId);
        }

        public async Task<AuthResponse> RefreshTokenAsync(string token, string refreshToken)
        {
            var jwtSettings = _config.GetSection("JwtSettings");
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false, // We want to validate an expired token
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Secret"]!))
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new BusinessRuleException("Invalid token");
            }

            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId!);
            if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                throw new BusinessRuleException("Invalid refresh token");
            }

            return await GenerateTokenAsync(user);
        }

        private async Task<AuthResponse> GenerateTokenAsync(ApplicationUser user)
        {
            var jwtSettings = _config.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Secret"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            };

            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var expiration = DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpiryMinutes"]!));

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: expiration,
                signingCredentials: creds
            );

            var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); // 7 days valid
            await _userManager.UpdateAsync(user);

            return new AuthResponse
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                RefreshToken = refreshToken,
                Expiration = expiration
            };
        }
    }
}
