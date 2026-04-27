using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.DTO.Auth
{
    public class UpdateProfileDto
    {
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public IFormFile? AvatarFile { get; set; } 
        public bool IsActive { get; set; } = true;
    }
}
