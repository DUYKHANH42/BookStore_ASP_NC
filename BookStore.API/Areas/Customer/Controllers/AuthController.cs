using BookStore.Application.DTO.Auth;
using BookStore.Application.Interfaces;
using BookStore.Application.Services;
using BookStore.Domain.Common;
using BookStore.Domain.Common;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BookStore.API.Areas.Customer.Controllers
{

    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authAppService;
        private readonly AdminProfileService _adminProfileService;
        private readonly IActivityLogService _activityLog;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

        public AuthController(
            AuthService authAppService,
            AdminProfileService adminProfileService,
            IActivityLogService activityLog,
            Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _authAppService = authAppService;
            _adminProfileService = adminProfileService;
            _activityLog = activityLog;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            var result = await _authAppService.RegisterAsync(registerDto);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("login")]
        public IActionResult Login()
        {
            var clientUrl = _configuration["AppSettings:ClientUrl"] ?? "http://localhost:4200";
            return Redirect($"{clientUrl.TrimEnd('/')}/login");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var result = await _authAppService.LoginAsync(loginDto);

            if (result == null) return Unauthorized(new { message = "Email hoặc mật khẩu không đúng." });
            if (!result.IsActive) return Unauthorized(new { message = "Tài khoản bị khóa." });

            if (result.Roles != null && result.Roles.Contains("Admin"))
            {
                try
                {
                    var adminName = result.FullName ?? result.Email ?? "Admin";
                    var adminEmail = result.Email ?? "";
                    var adminId = result.UserId ?? "";

                    var claims = new List<Claim> {
                        new Claim(ClaimTypes.Name, adminName),
                        new Claim(ClaimTypes.Email, adminEmail),
                        new Claim(ClaimTypes.NameIdentifier, adminId),
                    };
                    foreach (var role in result.Roles)
                    {
                        if (!string.IsNullOrEmpty(role))
                            claims.Add(new Claim(ClaimTypes.Role, role));
                    }

                    var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                    };
                    await HttpContext.SignInAsync("Cookies", new ClaimsPrincipal(claimsIdentity), authProperties);

                    if (!string.IsNullOrEmpty(result.UserId))
                    {
                        await _adminProfileService.RecordLoginAsync(result.UserId);
                        await _activityLog.LogAsync(
                            ActivityModules.Auth,
                            ActivityActions.Login,
                            $"Admin đăng nhập: {adminEmail}",
                            entityType: "ApplicationUser",
                            entityId: result.UserId,
                            actorId: result.UserId,
                            actorName: adminName,
                            actorRole: UserRoles.Admin);
                    }
                }
                catch (Exception)
                {
                    // Tránh làm hỏng luồng login của Admin nếu cookie authentication hoặc logging gặp lỗi
                }
            }

            // Set Refresh Token in HttpOnly Cookie
            if (!string.IsNullOrEmpty(result.RefreshToken))
            {
                Response.Cookies.Append("refreshToken", result.RefreshToken, BuildRefreshTokenCookieOptions());
                result.RefreshToken = null; // Do not return in response body
            }

            return Ok(result); 
        }

        [HttpGet("check-email")]
        public async Task<IActionResult> CheckEmail([FromQuery] string email)
        {
            var isUnique = await _authAppService.IsEmailUniqueAsync(email);
            return Ok(!isUnique);
        }
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] TokenRequestDto dto)
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(refreshToken)) 
                return Unauthorized(new { message = "Không tìm thấy refresh token." });

            var result = await _authAppService.RefreshTokenAsync(new TokenRequestDto { AccessToken = dto.AccessToken, RefreshToken = refreshToken });
            if (result == null)
                return Unauthorized(new { message = "Phiên đăng nhập hết hạn, vui lòng đăng nhập lại." });

            if (!string.IsNullOrEmpty(result.RefreshToken))
            {
                Response.Cookies.Append("refreshToken", result.RefreshToken, BuildRefreshTokenCookieOptions());
                result.RefreshToken = null;
            }

            return Ok(result);
        }
        [Authorize]
        [HttpPut("update-profile")]
        public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _authAppService.UpdateProfileAsync(userId, dto);

            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }
        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _authAppService.ChangePasswordAsync(userId, dto);

            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }
        [EnableRateLimiting("forgot-password")]
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var result = await _authAppService.ForgotPasswordAsync(dto);
            return Ok(result);
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var result = await _authAppService.ResetPasswordAsync(dto);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _authAppService.GetProfileAsync(userId);
            if (result == null) return NotFound(new { message = "Người dùng không tồn tại." });

            return Ok(result);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("Cookies");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                await _authAppService.LogoutAsync(userId);
            }

            Response.Cookies.Delete("refreshToken", BuildRefreshTokenCookieOptions());

            return Ok(new { success = true, message = "Đã đăng xuất thành công." });
        }

        /// <summary>
        /// Tạo CookieOptions cho refresh token.
        /// - Nếu API chạy HTTPS (Secure=true trong config): SameSite=None, Secure=true (cross-site cookie).
        /// - Nếu API chạy HTTP (Secure=false hoặc không cấu hình): SameSite=Lax, Secure=false.
        /// </summary>
        private Microsoft.AspNetCore.Http.CookieOptions BuildRefreshTokenCookieOptions()
        {
            var isSecure = bool.TryParse(_configuration["AppSettings:UseSecureCookie"], out var val) && val;
            return new Microsoft.AspNetCore.Http.CookieOptions
            {
                HttpOnly = true,
                Secure = isSecure,
                SameSite = isSecure
                    ? Microsoft.AspNetCore.Http.SameSiteMode.None
                    : Microsoft.AspNetCore.Http.SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddDays(7),
                Path = "/"
            };
        }
    }
}
