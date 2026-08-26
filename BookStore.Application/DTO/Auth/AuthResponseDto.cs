using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.DTO.Auth
{
    public class AuthResponseDto
    {
        public bool IsSuccess { get; set; }
        public string? UserId { get; set; }
        public string? Message { get; set; }
        public string? Token { get; set; }
        public DateTime? Expiration { get; set; }

        // Thông tin đầy đủ để đổ vào trang Profile
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? AvtUrl { get; set; }
        public string? Address { get; set; }      // THÊM MỚI
        public string? PhoneNumber { get; set; }  
        public bool IsActive { get; set; } = true;
        public string RefreshToken { get; set; } = string.Empty;
        public List<string>? Roles { get; set; }
    }
}
