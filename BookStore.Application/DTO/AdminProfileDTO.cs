using System;
using System.ComponentModel.DataAnnotations;

namespace BookStore.Application.DTO
{
    public class AdminProfileDTO
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Department { get; set; } = string.Empty;
        public string AvtUrl { get; set; } = string.Empty;
        public DateTime? LastLoginAt { get; set; }
        public string DisplayAvatar => string.IsNullOrEmpty(AvtUrl)
            ? string.Empty
            : (AvtUrl.StartsWith("http") ? AvtUrl : "/uploads/" + AvtUrl.TrimStart('/'));
        public string Initials
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FullName)) return "AD";
                var parts = FullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                    return (parts[0][0].ToString() + parts[^1][0].ToString()).ToUpper();
                return parts[0].Length >= 2 ? parts[0][..2].ToUpper() : parts[0][0].ToString().ToUpper();
            }
        }
    }

    public class UpdateAdminProfileDTO
    {
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? PhoneNumber { get; set; }

        [StringLength(100)]
        public string Department { get; set; } = string.Empty;
    }

    public class ChangeAdminPasswordDTO
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(8, ErrorMessage = "Mật khẩu mới phải ít nhất 8 ký tự")]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare(nameof(NewPassword), ErrorMessage = "Xác nhận mật khẩu không khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
