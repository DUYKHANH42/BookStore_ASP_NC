using BookStore.Application.DTO;
using BookStore.Application.Interfaces;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BookStore.Application.Services
{
    public class AdminProfileService
    {
        private readonly IAuthService _authService;
        private readonly IFileService _fileService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AdminProfileService(
            IAuthService authService,
            IFileService fileService,
            IHttpContextAccessor httpContextAccessor)
        {
            _authService = authService;
            _fileService = fileService;
            _httpContextAccessor = httpContextAccessor;
        }

        public string? GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        public async Task<AdminProfileDTO?> GetCurrentProfileAsync()
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId)) return null;

            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null) return null;

            return MapToDto(user);
        }

        public async Task<bool> UpdateProfileAsync(UpdateAdminProfileDTO dto)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId)) return false;

            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null) return false;

            user.FullName = dto.FullName.Trim();
            user.PhoneNumber = dto.PhoneNumber;
            user.Department = dto.Department?.Trim() ?? string.Empty;
            user.LastUpdatedAt = DateTime.UtcNow;

            var result = await _authService.UpdateUserAsync(user);
            return result.Succeeded;
        }

        public async Task<bool> UpdateAvatarAsync(IFormFile file)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId)) return false;

            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null) return false;

            var avtUrl = await _fileService.SaveFileAsync(file, "avatars");
            if (string.IsNullOrEmpty(avtUrl)) return false;

            user.AvtUrl = avtUrl;
            user.LastUpdatedAt = DateTime.UtcNow;

            var result = await _authService.UpdateUserAsync(user);
            return result.Succeeded;
        }

        public async Task<bool> ChangePasswordAsync(ChangeAdminPasswordDTO dto)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId)) return false;

            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null) return false;

            var result = await _authService.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            return result.Succeeded;
        }

        public async Task RecordLoginAsync(string userId)
        {
            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null) return;

            user.LastLoginAt = DateTime.UtcNow;
            await _authService.UpdateUserAsync(user);
        }

        private static AdminProfileDTO MapToDto(ApplicationUser user) => new()
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            Department = user.Department,
            AvtUrl = user.AvtUrl,
            LastLoginAt = user.LastLoginAt
        };
    }
}
