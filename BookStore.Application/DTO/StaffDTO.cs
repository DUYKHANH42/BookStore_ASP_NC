using System;
using System.ComponentModel.DataAnnotations;

namespace BookStore.Application.DTO
{
    public class StaffUserDTO
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Department { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; }

        public string Initials
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FullName)) return "?";
                var parts = FullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2) return (parts[0][0].ToString() + parts[^1][0].ToString()).ToUpper();
                return parts[0].Length >= 2 ? parts[0][..2].ToUpper() : parts[0][0].ToString().ToUpper();
            }
        }
    }

    public class CreateStaffDTO
    {
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? PhoneNumber { get; set; }

        [StringLength(100)]
        public string Department { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vai trò là bắt buộc")]
        [RegularExpression("^(Admin|Employee)$", ErrorMessage = "Vai trò chỉ được là Admin hoặc Employee")]
        public string Role { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [MinLength(8, ErrorMessage = "Mật khẩu phải ít nhất 8 ký tự")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc")]
        [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu admin để xác nhận")]
        public string AdminConfirmPassword { get; set; } = string.Empty;
    }
}
