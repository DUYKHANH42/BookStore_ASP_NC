using BookStore.Application.DTO;
using BookStore.Application.Interfaces;
using BookStore.Domain.Common;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookStore.Application.Services
{
    public class StaffService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuthService _authService;
        private readonly IActivityLogService _activityLog;

        private static readonly string[] StaffRoles = { UserRoles.Admin, UserRoles.Employee };

        private static string NormalizeRole(string? role)
        {
            if (string.IsNullOrWhiteSpace(role)) return string.Empty;
            if (role.Equals(UserRoles.Admin, StringComparison.OrdinalIgnoreCase)) return UserRoles.Admin;
            if (role.Equals(UserRoles.Employee, StringComparison.OrdinalIgnoreCase)) return UserRoles.Employee;
            return string.Empty;
        }

        public StaffService(
            UserManager<ApplicationUser> userManager,
            IAuthService authService,
            IActivityLogService activityLog)
        {
            _userManager = userManager;
            _authService = authService;
            _activityLog = activityLog;
        }

        public async Task<PagedResultDTO<StaffUserDTO>> GetPagedStaffAsync(
            int page, int pageSize, string search = "", string? role = null, bool? isActive = null)
        {
            var staffUsers = new List<ApplicationUser>();

            foreach (var staffRole in StaffRoles)
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(staffRole);
                staffUsers.AddRange(usersInRole);
            }

            var distinctStaff = staffUsers
                .GroupBy(u => u.Id)
                .Select(g => g.First())
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                distinctStaff = distinctStaff.Where(u =>
                    u.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (u.Email != null && u.Email.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(search, StringComparison.OrdinalIgnoreCase)));
            }

            if (isActive.HasValue)
                distinctStaff = distinctStaff.Where(u => u.IsActive == isActive.Value);

            var staffList = distinctStaff.ToList();
            var mapped = new List<StaffUserDTO>();

            foreach (var user in staffList)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var primaryRole = roles.FirstOrDefault(r => StaffRoles.Contains(r)) ?? UserRoles.Employee;

                if (!string.IsNullOrWhiteSpace(role) && !primaryRole.Equals(role, StringComparison.OrdinalIgnoreCase))
                    continue;

                mapped.Add(new StaffUserDTO
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    PhoneNumber = user.PhoneNumber,
                    Department = user.Department,
                    Role = primaryRole,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt,
                    IsActive = user.IsActive
                });
            }

            var totalItems = mapped.Count;
            var items = mapped
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResultDTO<StaffUserDTO>
            {
                Items = items,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                CurrentPage = page,
                PageSize = pageSize
            };
        }

        public async Task<(bool Success, string Message, StaffUserDTO? Staff)> CreateStaffAsync(
            string currentAdminId, CreateStaffDTO dto)
        {
            var role = NormalizeRole(dto.Role);
            if (string.IsNullOrEmpty(role))
                return (false, "Vai trò không hợp lệ. Chỉ được tạo Admin hoặc Employee.", null);

            dto.Role = role;

            if (!dto.Password.Equals(dto.ConfirmPassword, StringComparison.Ordinal))
                return (false, "Mật khẩu xác nhận không khớp.", null);

            var currentAdmin = await _authService.GetUserByIdAsync(currentAdminId);
            if (currentAdmin == null)
                return (false, "Không xác định được admin hiện tại.", null);

            if (!await _authService.VerifyPasswordAsync(currentAdmin, dto.AdminConfirmPassword))
                return (false, "Mật khẩu xác nhận danh tính không đúng.", null);

            if (!await _authService.IsEmailUniqueAsync(dto.Email))
                return (false, "Email đã được sử dụng.", null);

            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.FullName.Trim(),
                PhoneNumber = dto.PhoneNumber,
                Department = dto.Department?.Trim() ?? string.Empty,
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _authService.RegisterAsync(user, dto.Password, dto.Role);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                var hint = dto.Role == UserRoles.Admin
                    ? " Mật khẩu Admin cần: tối thiểu 8 ký tự, có chữ hoa, chữ thường và số."
                    : string.Empty;
                return (false, errors + hint, null);
            }

            var actionCode = dto.Role == UserRoles.Admin ? ActivityActions.CreateAdmin : ActivityActions.CreateEmployee;
            await _activityLog.LogAsync(
                ActivityModules.Staff,
                actionCode,
                $"Tạo tài khoản {dto.Role}: {user.FullName} ({user.Email})",
                entityType: "ApplicationUser",
                entityId: user.Id,
                targetUserId: user.Id,
                actorId: currentAdminId,
                actorName: currentAdmin.FullName,
                actorRole: UserRoles.Admin);

            var successMsg = dto.Role == UserRoles.Admin
                ? $"Đã tạo Admin mới thành công! Tài khoản {user.Email} có thể đăng nhập vào trang quản trị."
                : $"Đã tạo tài khoản Employee thành công!";

            return (true, successMsg, new StaffUserDTO
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email!,
                PhoneNumber = user.PhoneNumber,
                Department = user.Department,
                Role = dto.Role,
                CreatedAt = user.CreatedAt,
                IsActive = user.IsActive
            });
        }

        public async Task<bool> UpdateStaffStatusAsync(string id, bool isActive, string? currentAdminId = null)
        {
            if (!string.IsNullOrEmpty(currentAdminId) && currentAdminId == id && !isActive)
                return false;

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return false;

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Any(r => StaffRoles.Contains(r)))
                return false;

            user.IsActive = isActive;
            var result = await _userManager.UpdateAsync(user);
            
            if (result.Succeeded)
            {
                await _activityLog.LogAsync(
                    ActivityModules.Staff,
                    ActivityActions.StatusChange,
                    $"{(isActive ? "Mở khóa" : "Khóa")} tài khoản nhân sự {user.FullName} ({user.Email})",
                    entityType: "ApplicationUser",
                    entityId: user.Id,
                    targetUserId: user.Id,
                    actorId: currentAdminId);
            }

            return result.Succeeded;
        }
    }
}
