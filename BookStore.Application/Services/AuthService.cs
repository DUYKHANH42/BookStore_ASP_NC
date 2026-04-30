using BookStore.Application.DTO.Auth;
using BookStore.Application.Interfaces;
using BookStore.Domain.Common;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Services
{
    public class AuthService
    {
        // Gọi Interface IAuthService (thực thi ở tầng Infrastructure)
        private readonly IAuthService _identityService;
        private readonly IMailService _mailService;

        public AuthService(IAuthService identityService, IMailService mailService)
        {
            _identityService = identityService;
            _mailService = mailService;
        }

        // Logic Đăng ký
        public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            var user = new ApplicationUser
            {
                UserName = registerDto.Email,
                Email = registerDto.Email,
                FullName = registerDto.FullName,
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            var result = await _identityService.RegisterAsync(user, registerDto.Password, UserRoles.Customer);

            if (!result.Succeeded)
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = string.Join(", ", result.Errors.Select(e => e.Description))
                };
            }

            return new AuthResponseDto { IsSuccess = true, Message = "Đăng ký thành công!" };
        }

        // Logic Đăng nhập
        public async Task<AuthResponseDto?> LoginAsync(LoginDto loginDto)
        {
            // Gọi tầng Infrastructure xử lý xác thực và tạo Token
            var authModel = await _identityService.LoginAsync(loginDto.Email, loginDto.Password);

            if (authModel == null) return null;

            // Mapping từ Domain Model (AuthModel) sang DTO trả về cho Angular
            return new AuthResponseDto
            {
                IsSuccess = true,
                UserId = authModel.User.Id,
                Token = authModel.Token,
                RefreshToken = authModel.RefreshToken,
                Expiration = authModel.Expiration,
                FullName = authModel.User.FullName,
                Email = authModel.User.Email,
                AvtUrl = authModel.User.AvtUrl,
                Address = authModel.User.Address,
                PhoneNumber = authModel.User.PhoneNumber,
                IsActive = authModel.User.IsActive,
                Roles = authModel.Roles.ToList()
            };
        }

        public async Task<bool> IsEmailUniqueAsync(string email)
        {
            return await _identityService.IsEmailUniqueAsync(email);
        }
        public async Task<AuthResponseDto?> RefreshTokenAsync(TokenRequestDto dto)
        {
            var authModel = await _identityService.RefreshTokenAsync(dto.AccessToken, dto.RefreshToken);
            if (authModel == null) return null;

            return new AuthResponseDto
            {
                IsSuccess = true,
                UserId = authModel.User.Id,
                Token = authModel.Token,
                RefreshToken = authModel.RefreshToken,
                Expiration = authModel.Expiration,
                FullName = authModel.User.FullName,
                Email = authModel.User.Email,
                PhoneNumber = authModel.User.PhoneNumber,
                Roles = authModel.Roles.ToList()
            };
        }
        public async Task<AuthResponseDto> UpdateProfileAsync(string userId, UpdateProfileDto dto)
        {
            var user = await _identityService.GetUserByIdAsync(userId);
            if (user == null) return new AuthResponseDto { IsSuccess = false, Message = "User không tồn tại" };

            user.FullName = dto.FullName;
            user.PhoneNumber = dto.PhoneNumber;
            user.IsActive = dto.IsActive;
            if (dto.AvatarFile != null)
            {
                // 1. Quy định thư mục lưu trữ (Chỉ cần 1 biến duy nhất ở đây)
                var folderName = Path.Combine("wwwroot", "uploads", "avatars");
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), folderName);

                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                if (!string.IsNullOrEmpty(user.AvtUrl))
                {
                    var oldFilePath = Path.Combine(folderPath, user.AvtUrl);
                    if (File.Exists(oldFilePath))
                    {
                        File.Delete(oldFilePath);
                    }
                }

                var newFileName = $"{userId}_{Guid.NewGuid()}{Path.GetExtension(dto.AvatarFile.FileName)}";
                var newFilePath = Path.Combine(folderPath, newFileName);

                using (var stream = new FileStream(newFilePath, FileMode.Create))
                {
                    await dto.AvatarFile.CopyToAsync(stream);
                }

                user.AvtUrl = newFileName;
            }

            var result = await _identityService.UpdateUserAsync(user);

            if (!result.Succeeded)
                return new AuthResponseDto { IsSuccess = false, Message = "Lỗi cập nhật database" };

            return new AuthResponseDto
            {
                IsSuccess = true,
                FullName = user.FullName,
                AvtUrl = user.AvtUrl,
                PhoneNumber = user.PhoneNumber,
                Email = user.Email,
                IsActive = user.IsActive,
                Message = "Cập nhật thành công"
            };
        }
        public async Task<AuthResponseDto> ChangePasswordAsync(string userId, ChangePasswordDto dto)
        {
            var user = await _identityService.GetUserByIdAsync(userId);
            if (user == null) return new AuthResponseDto { IsSuccess = false, Message = "Người dùng không tồn tại" };

            var result = await _identityService.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);

            if (!result.Succeeded)
            {
                // Lấy lỗi đầu tiên từ Identity (ví dụ: mật khẩu cũ sai, hoặc mật khẩu mới quá yếu)
                var error = result.Errors.FirstOrDefault()?.Description ?? "Đổi mật khẩu thất bại";
                return new AuthResponseDto { IsSuccess = false, Message = error };
            }

            return new AuthResponseDto { IsSuccess = true, Message = "Đổi mật khẩu thành công!" };
        }
        // 1. Xử lý yêu cầu quên mật khẩu
        public async Task<AuthResponseDto> ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            var user = await _identityService.FindByEmailAsync(dto.Email);
            if (user == null) return new AuthResponseDto { IsSuccess = true, Message = "Vui lòng kiểm tra email." };
            var token = await _identityService.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            string baseUrl = "https://localhost:44326";
            string resetLink = $"http://localhost:53214/reset-password?token={encodedToken}&email={user.Email}";
            string subject = "Khôi phục mật khẩu - Lumen BookStore";

            string content = $@"
<div style='font-family: ""Plus Jakarta Sans"", Helvetica, Arial, sans-serif; background-color: #f8fafc; padding: 40px 20px; line-height: 1.6;'>
    <div style='max-width: 500px; margin: 0 auto; background-color: #ffffff; border-radius: 32px; overflow: hidden; box-shadow: 0 20px 40px rgba(0,0,0,0.05); border: 1px solid #f1f5f9;'>
        
        <!-- Header với Logo thực tế -->
        <div style='background-color: #0f172a; padding: 40px 30px; text-align: center;'>
             <img src='{baseUrl}/imgs/logo_sach.png' alt='Lumen BookStore' style='height: 60px; width: auto;' />
        </div>
        <div style='padding: 40px;'>
            <h2 style='color: #1e293b; margin-top: 0; font-size: 22px; font-weight: 800; letter-spacing: -0.5px;'>Chào {user.FullName},</h2>
            <p style='color: #64748b; font-size: 15px;'>Chúng tôi đã nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn. Đừng lo lắng, bạn có thể lấy lại quyền truy cập chỉ với một cú click.</p>
            
            <div style='margin: 40px 0; text-align: center;'>
                <p style='color: #64748b; font-size: 14px; margin-bottom: 25px;'>Vui lòng nhấn vào nút bên dưới để tiến hành đặt mật khẩu mới:</p>
                
                <!-- Nút bấm nổi bật -->
                <a href='{resetLink}' style='display: inline-block; background-color: #2563eb; color: #ffffff; padding: 18px 40px; border-radius: 18px; font-weight: 900; font-size: 13px; text-decoration: none; text-transform: uppercase; letter-spacing: 1px; box-shadow: 0 10px 20px rgba(37, 99, 235, 0.25);'>Đặt lại mật khẩu ngay</a>
            </div>
            <div style='margin-top: 40px; padding-top: 25px; border-top: 1px solid #f1f5f9;'>
                <p style='color: #94a3b8; font-size: 12px; margin-bottom: 8px;'>• Đường dẫn này sẽ hết hiệu lực trong vòng <strong>2 tiếng</strong>.</p>
                <p style='color: #94a3b8; font-size: 12px;'>• Nếu bạn không yêu cầu thay đổi này, hãy bỏ qua email này. Tài khoản của bạn vẫn sẽ được bảo mật.</p>
            </div>
        </div>
        <!-- Footer -->
        <div style='background-color: #f8fafc; padding: 25px; text-align: center;'>
            <p style='color: #cbd5e1; font-size: 11px; margin: 0;'>© 2026 Lumen BookStore. Hành trình tri thức của bạn.</p>
        </div>
    </div>
</div>";

            await _mailService.SendEmailAsync(user.Email!, subject, content);


            return new AuthResponseDto
            {
                IsSuccess = true,
                Message = "Mã xác nhận đã được gửi vào email của bạn."
            };
        }

        // 2. Xử lý cập nhật mật khẩu mới
        public async Task<AuthResponseDto> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var user = await _identityService.FindByEmailAsync(dto.Email);
            if (user == null) return new AuthResponseDto { IsSuccess = false, Message = "Lỗi xác thực" };

            string actualToken;
            try
            {
                var decodedTokenBytes = WebEncoders.Base64UrlDecode(dto.Token);
                actualToken = Encoding.UTF8.GetString(decodedTokenBytes);
            }
            catch
            {
                return new AuthResponseDto { IsSuccess = false, Message = "Mã xác nhận không hợp lệ." };
            }

            var result = await _identityService.ResetPasswordAsync(user, actualToken, dto.NewPassword);

            if (!result.Succeeded)
            {
                var error = result.Errors.FirstOrDefault()?.Description ?? "Token không hợp lệ hoặc đã hết hạn";
                return new AuthResponseDto { IsSuccess = false, Message = error };
            }

            return new AuthResponseDto { IsSuccess = true, Message = "Mật khẩu đã được cập nhật thành công!" };
        }
    }
}