using BookStore.Application.DTO;
using System;
using System.Threading.Tasks;

namespace BookStore.Application.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardDataDTO> GetDashboardDataAsync(DateTime startDate, DateTime endDate);
    }
}
