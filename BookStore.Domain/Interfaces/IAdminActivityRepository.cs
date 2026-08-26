using BookStore.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookStore.Domain.Interfaces
{
    public interface IAdminActivityRepository
    {
        Task AddAsync(AdminActivityLog log);

        Task<(List<AdminActivityLog> Items, int Total)> GetPagedAsync(
            int page,
            int pageSize,
            string? search = null,
            string? module = null,
            string? action = null,
            string? actorId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null);
    }
}
