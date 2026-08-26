using BookStore.Application.DTO;
using BookStore.Domain.Common;
using BookStore.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

namespace BookStore.Application.Services
{
    public class CustomerService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IServiceProvider _serviceProvider;

        public CustomerService(UserManager<ApplicationUser> userManager, IServiceProvider serviceProvider)
        {
            _userManager = userManager;
            _serviceProvider = serviceProvider;
        }

        public async Task<PagedResultDTO<UserDTO>> GetPagedCustomersAsync(int page, int pageSize, string search = "", bool? isActive = null)
        {
            if (page < 1) page = 1;

            var customers = (await _userManager.GetUsersInRoleAsync(UserRoles.Customer)).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                customers = customers.Where(u =>
                    u.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (u.Email != null && u.Email.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(search, StringComparison.OrdinalIgnoreCase)));
            }

            if (isActive.HasValue)
                customers = customers.Where(u => u.IsActive == isActive.Value);

            var totalItems = customers.Count();
            var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages > 0 && page > totalPages)
                page = totalPages;

            var pageIds = customers
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => u.Id)
                .ToList();

            List<UserDTO> items;
            if (pageIds.Count == 0)
            {
                items = new List<UserDTO>();
            }
            else
            {
                items = await _userManager.Users
                    .Include(u => u.Orders)
                    .Where(u => pageIds.Contains(u.Id))
                    .OrderByDescending(u => u.CreatedAt)
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
            }

            return new PagedResultDTO<UserDTO>
            {
                Items = items,
                TotalItems = totalItems,
                TotalPages = totalPages,
                CurrentPage = page,
                PageSize = pageSize
            };
        }

        public async Task<UserDTO?> GetCustomerByIdAsync(string id)
        {
            var user = await _userManager.Users.Include(u => u.Orders).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null || !await _userManager.IsInRoleAsync(user, UserRoles.Customer))
                return null;

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
            if (user == null || !await _userManager.IsInRoleAsync(user, UserRoles.Customer))
                return false;

            user.IsActive = isActive;
            
            if (!isActive)
            {
                user.TokenVersion++;
                var redisService = _serviceProvider.GetRequiredService<BookStore.Domain.Interfaces.IRedisService>();
                await redisService.SetAsync($"TokenVersion:{user.Id}", user.TokenVersion);
                await redisService.RemoveAsync($"RefreshToken:{user.Id}");
            }

            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }

        public async Task<bool> ResetCustomerPasswordAsync(string id, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null || !await _userManager.IsInRoleAsync(user, UserRoles.Customer))
                return false;

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
