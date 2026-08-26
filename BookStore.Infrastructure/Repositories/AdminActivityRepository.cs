using BookStore.Domain.Common;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using BookStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookStore.Infrastructure.Repositories
{
    public class AdminActivityRepository : IAdminActivityRepository
    {
        private readonly BookStoreDbContext _context;

        public AdminActivityRepository(BookStoreDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(AdminActivityLog log)
        {
            await _context.AdminActivityLogs.AddAsync(log);
            await _context.SaveChangesAsync();
        }

        public async Task<(List<AdminActivityLog> Items, int Total)> GetPagedAsync(
            int page,
            int pageSize,
            string? search = null,
            string? module = null,
            string? action = null,
            string? actorId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            var query = _context.AdminActivityLogs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(l =>
                    l.AdminName.Contains(search) ||
                    l.Details.Contains(search) ||
                    (l.EntityId != null && l.EntityId.Contains(search)) ||
                    (l.AdminId != null && l.AdminId.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(module))
            {
                if (module == ActivityModules.Staff)
                {
                    query = query.Where(l =>
                        l.Module == ActivityModules.Staff ||
                        (l.Module == null && (l.Action == ActivityActions.CreateAdmin || l.Action == ActivityActions.CreateEmployee)));
                }
                else
                {
                    query = query.Where(l => l.Module == module);
                }
            }

            if (!string.IsNullOrWhiteSpace(action))
                query = query.Where(l => l.Action == action);

            if (!string.IsNullOrWhiteSpace(actorId))
                query = query.Where(l => l.AdminId == actorId);

            if (fromDate.HasValue)
                query = query.Where(l => l.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
            {
                var end = toDate.Value.Date.AddDays(1);
                query = query.Where(l => l.CreatedAt < end);
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }
    }
}
