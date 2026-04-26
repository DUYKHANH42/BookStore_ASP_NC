using BookStore.Application.DTO.Auth;
using BookStore.Domain.Common;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BookStore.API.Areas.Customer.Controllers
{
    [ApiController]
    [Route("api/auth")] // Đường dẫn: api/auth
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly UserManager<ApplicationUser> _userManager;

        public AuthController(IAuthService authService, UserManager<ApplicationUser> userManager)
        {
            _authService = authService;
            _userManager = userManager;
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register( RegisterDto registerDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = new ApplicationUser
            {
                UserName = registerDto.Email,
                Email = registerDto.Email,
                FullName = registerDto.FullName,
                CreatedAt = DateTime.Now,
            };

            // Mặc định đăng ký mới là Role "Customer"
            var result = await _authService.RegisterAsync(user, registerDto.Password, UserRoles.Customer);

            if (!result.Succeeded)
            {
                // Trả về danh sách lỗi từ Identity (ví dụ: mật khẩu yếu, email trùng...)
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            }

            return Ok(new AuthResponseDto
            {
                IsSuccess = true,
                Message = "Đăng ký thành công!"
            });
        }

        // 2. Đăng nhập (Khớp với LoginComponent)
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var authModel = await _authService.LoginAsync(loginDto.Email, loginDto.Password);

            if (authModel == null)
                return Unauthorized(new { message = "Email hoặc mật khẩu không đúng." });

            // Mapping từ AuthModel (Domain) sang AuthResponseDto (Application)
            return Ok(new AuthResponseDto
            {
                IsSuccess = true,
                Token = authModel.Token,
                Expiration = authModel.Expiration,

                // Lấy dữ liệu từ object User bên trong AuthModel
                FullName = authModel.User.FullName,
                Email = authModel.User.Email,
                AvtUrl = authModel.User.AvtUrl,
                Address = authModel.User.Address,
                PhoneNumber = authModel.User.PhoneNumber,
                IsActive = authModel.User.IsActive,
                Roles = authModel.Roles.ToList(),
                RefreshToken = authModel.RefreshToken,

            });
        }
        [HttpGet("check-email")]
        public async Task<IActionResult> CheckEmail([FromQuery] string email)
        {
            var isUnique = await _authService.IsEmailUniqueAsync(email);
            return Ok(!isUnique);
        }
    }
}
