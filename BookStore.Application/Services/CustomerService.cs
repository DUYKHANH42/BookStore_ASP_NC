using BookStore.Application.DTO;
using BookStore.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookStore.Application.Services
{
    public class CustomerService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomerService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<PagedResultDTO<UserDTO>> GetPagedCustomersAsync(int page, int pageSize, string search = "", bool? isActive = null)
        {
            var query = _userManager.Users.Include(u => u.Orders).AsQueryable();
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => u.FullName.Contains(search) || 
                                         u.Email.Contains(search) || 
                                         u.PhoneNumber.Contains(search));
            }

            // Lọc theo trạng thái
            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            var totalItems = await query.CountAsync();
            var items = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserDTO
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    CreatedAt = u.CreatedAt,
                    IsActive = u.IsActive,
                    OrderCount = u.Orders.Count
                })
                .ToListAsync();

            return new PagedResultDTO<UserDTO>
            {
                Items = items,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                CurrentPage = page,
                PageSize = pageSize
            };
        }

        public async Task<UserDTO?> GetCustomerByIdAsync(string id)
        {
            var user = await _userManager.Users.Include(u => u.Orders).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return null;

            return new UserDTO
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                CreatedAt = user.CreatedAt,
                IsActive = user.IsActive,
                OrderCount = user.Orders.Count
            };
        }

        public async Task<bool> UpdateCustomerStatusAsync(string id, bool isActive)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return false;

            user.IsActive = isActive;
            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }

        public async Task<bool> ResetCustomerPasswordAsync(string id, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return false;

            // Xóa mật khẩu cũ và đặt lại mật khẩu mới
            var removeResult = await _userManager.RemovePasswordAsync(user);
            if (removeResult.Succeeded || removeResult.Errors.Any(e => e.Code == "UserHasNoPassword"))
            {
                var addResult = await _userManager.AddPasswordAsync(user, newPassword);
                return addResult.Succeeded;
            }

            return false;
        }
    }
}
