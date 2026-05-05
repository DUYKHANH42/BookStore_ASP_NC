using BookStore.Application.DTO.Auth;
using BookStore.Application.Services;
using BookStore.Domain.Common;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
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

        public AuthController(AuthService authAppService)
        {
            _authAppService = authAppService;
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
            //return Redirect("http://localhost:53214/login");
            return Redirect("https://book-store-giao-dien.vercel.app/login");

        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var result = await _authAppService.LoginAsync(loginDto);

            if (result == null) return Unauthorized(new { message = "Email hoặc mật khẩu không đúng." });
            if (!result.IsActive) return Unauthorized(new { message = "Tài khoản bị khóa." });

            if (result.Roles != null && result.Roles.Contains("Admin"))
            {
                var claims = new List<Claim> {
                    new Claim(ClaimTypes.Name, result.FullName!),
                    new Claim(ClaimTypes.Email, result.Email!),
                    new Claim(ClaimTypes.NameIdentifier, result.UserId!),
                };
                foreach (var role in result.Roles) claims.Add(new Claim(ClaimTypes.Role, role));

                var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                };
                await HttpContext.SignInAsync("Cookies", new ClaimsPrincipal(claimsIdentity), authProperties);
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
            var result = await _authAppService.RefreshTokenAsync(dto);
            if (result == null)
                return Unauthorized(new { message = "Phiên đăng nhập hết hạn, vui lòng đăng nhập lại." });

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
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("Cookies");
            return Ok(new { success = true, message = "Đã đăng xuất thành công." });
        }
    }
}
