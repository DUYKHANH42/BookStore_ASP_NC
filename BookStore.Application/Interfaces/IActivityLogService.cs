using BookStore.Application.DTO;
using System.Threading.Tasks;

namespace BookStore.Application.Interfaces
{
    /// <summary>
    /// Ghi và tra cứu nhật ký hoạt động admin/nhân viên (audit trail).
    /// Khác với INotificationService (cảnh báo đơn hàng real-time).
    /// </summary>
    public interface IActivityLogService
    {
        Task LogAsync(
            string module,
            string action,
            string details,
            string? entityType = null,
            string? entityId = null,
            string? targetUserId = null,
            string? actorId = null,
            string? actorName = null,
            string? actorRole = null);

        Task<PagedResultDTO<ActivityLogDTO>> GetPagedAsync(
            int page,
            int pageSize,
            ActivityLogFilterDTO? filter = null);
    }
}
