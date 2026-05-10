using BookStore.Application.DTO.Auth;
using BookStore.Domain.Common;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Infrastructure.Identity
{
    public class AuthRepository : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly IRedisService _redisService;

        public AuthRepository(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            IRedisService redisService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _redisService = redisService;
        }
      
        public async Task<IdentityResult> RegisterAsync(ApplicationUser user, string password, string role)
        {
            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                // Kiểm tra và tạo Role nếu chưa tồn tại
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }
                await _userManager.AddToRoleAsync(user, role);
            }
            return result;
        }

        public async Task<AuthModel?> LoginAsync(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, password))
                return null;

            var tokenData = await GenerateJwtToken(user);
            
            // Generate Refresh Token
            var refreshToken = CreateRefreshToken(user.Id);
            
            // Store Refresh Token in Redis with TTL (7 days)
            var expiry = TimeSpan.FromDays(7);
            await _redisService.SetAsync($"RefreshToken:{user.Id}", refreshToken, expiry);

            await _userManager.UpdateAsync(user);
            var roles = await _userManager.GetRolesAsync(user);

            return new AuthModel
            {
                Token = tokenData.token,
                Expiration = tokenData.expiration,
                User = user, 
                Roles = roles.ToList(),
                RefreshToken = refreshToken,
                IsActive = user.IsActive,
            };
        }
        public async Task<bool> IsEmailUniqueAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            return user == null;
        }
        public async Task<AuthModel?> RefreshTokenAsync(string accessToken, string refreshToken)
        {
            var parts = refreshToken?.Split(':');
            if (parts == null || parts.Length < 2) return null;
            
            var userId = parts[0];
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            // 2. Validate RefreshToken from Redis
            var storedRefreshToken = await _redisService.GetAsync<string>($"RefreshToken:{user.Id}");

            // Reuse detection / Invalid token check
            if (string.IsNullOrEmpty(storedRefreshToken) || storedRefreshToken != refreshToken)
            {
                // If it doesn't match or is empty, it could be a reused/revoked token.
                // Revoke all sessions for safety by deleting the token and incrementing TokenVersion
                await _redisService.RemoveAsync($"RefreshToken:{user.Id}");
                user.TokenVersion++;
                await _userManager.UpdateAsync(user);
                // Also update TokenVersion in Redis
                await _redisService.SetAsync($"TokenVersion:{user.Id}", user.TokenVersion);
                return null;
            }

            // 3. Create new Tokens (Rotation)
            var newAccessToken = await GenerateJwtToken(user);
            var newRefreshToken = CreateRefreshToken(user.Id);

            // 4. Update in Redis
            var expiry = TimeSpan.FromDays(7);
            await _redisService.SetAsync($"RefreshToken:{user.Id}", newRefreshToken, expiry);

            // We don't need to call UpdateAsync on user unless TokenVersion changed, but let's just do it
            await _userManager.UpdateAsync(user);

            var roles = await _userManager.GetRolesAsync(user);

            return new AuthModel
            {
                Token = newAccessToken.token,
                RefreshToken = newRefreshToken,
                Expiration = newAccessToken.expiration,
                User = user,
                Roles = roles.ToList(),
                IsActive = user.IsActive
            };
        }
        public async Task<ApplicationUser?> GetUserByIdAsync(string userId)
        {
            return await _userManager.FindByIdAsync(userId);
        }

        public async Task<IdentityResult> UpdateUserAsync(ApplicationUser user)
        {
            user.LastUpdatedAt = BookStore.Domain.Common.TimeHelper.GetVnTime();
            return await _userManager.UpdateAsync(user);
        }
        public async Task<IdentityResult> ChangePasswordAsync(ApplicationUser user, string currentPassword, string newPassword)
        {
            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (result.Succeeded)
            {
                // Increment TokenVersion on password change to revoke old tokens
                user.TokenVersion++;
                await _userManager.UpdateAsync(user);
                await _redisService.SetAsync($"TokenVersion:{user.Id}", user.TokenVersion);
                // Also revoke existing refresh token
                await _redisService.RemoveAsync($"RefreshToken:{user.Id}");
            }
            return result;
        }
        public async Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user)
        {
            // Hàm này của Identity sẽ tạo ra một chuỗi mã hóa cực kỳ bảo mật và có thời hạn
            return await _userManager.GeneratePasswordResetTokenAsync(user);
        }

        public async Task<IdentityResult> ResetPasswordAsync(ApplicationUser user, string token, string newPassword)
        {
            // Hàm này sẽ tự kiểm tra token có đúng của user này không, có hết hạn chưa rồi mới đổi pass
            return await _userManager.ResetPasswordAsync(user, token, newPassword);
        }
        // Hàm bổ trợ để đọc Token hết hạn
        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]!)),
                ValidateLifetime = false // Quan trọng: Không kiểm tra thời gian hết hạn ở đây
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

            if (!(securityToken is JwtSecurityToken jwtSecurityToken) ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                return null;

            return principal;
        }
        private string CreateRefreshToken(string userId)
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return $"{userId}:{Convert.ToBase64String(randomNumber)}";
        }


        private async Task<(string token, DateTime expiration)> GenerateJwtToken(ApplicationUser user)
        {
            var userRoles = await _userManager.GetRolesAsync(user);

            // Ensure TokenVersion is in Redis
            await _redisService.SetAsync($"TokenVersion:{user.Id}", user.TokenVersion);

            var authClaims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.FullName),
        new Claim(ClaimTypes.Email, user.Email!),
        new Claim(ClaimTypes.NameIdentifier, user.Id),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim("TokenVersion", user.TokenVersion.ToString())
    };

            foreach (var userRole in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, userRole));
            }

            // Access Token usually lives for a short time (e.g. 15 mins)
            var expirationTime = BookStore.Domain.Common.TimeHelper.GetVnTime().AddMinutes(15);

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]!));

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: expirationTime, // Dùng biến vừa tạo
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            // Trả về cả 2 giá trị dưới dạng Tuple
            return (tokenString, expirationTime);
        }

        public async Task<ApplicationUser> FindByEmailAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            return user!;
        }
    }
}

