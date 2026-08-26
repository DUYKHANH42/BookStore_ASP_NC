using BookStore.Application.DTO;
using BookStore.Application.Interfaces;
using BookStore.Domain.Common;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BookStore.Application.Services
{
    public class ActivityLogService : IActivityLogService
    {
        private readonly IAdminActivityRepository _repository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ActivityLogService(
            IAdminActivityRepository repository,
            IHttpContextAccessor httpContextAccessor)
        {
            _repository = repository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(
            string module,
            string action,
            string details,
            string? entityType = null,
            string? entityId = null,
            string? targetUserId = null,
            string? actorId = null,
            string? actorName = null,
            string? actorRole = null)
        {
            var resolved = ResolveActor(actorId, actorName, actorRole);

            var log = new AdminActivityLog
            {
                AdminId = resolved.ActorId,
                AdminName = resolved.ActorName,
                ActorRole = resolved.ActorRole,
                Module = module,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                TargetUserId = targetUserId,
                Details = details,
                IpAddress = resolved.IpAddress,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(log);
        }

        public async Task<PagedResultDTO<ActivityLogDTO>> GetPagedAsync(
            int page,
            int pageSize,
            ActivityLogFilterDTO? filter = null)
        {
            filter ??= new ActivityLogFilterDTO();

            var (items, total) = await _repository.GetPagedAsync(
                page,
                pageSize,
                filter.Search,
                filter.Module,
                filter.Action,
                filter.ActorId,
                filter.FromDate,
                filter.ToDate);

            return new PagedResultDTO<ActivityLogDTO>
            {
                Items = items.Select(MapToDto).ToList(),
                TotalItems = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                CurrentPage = page,
                PageSize = pageSize
            };
        }

        private (string ActorId, string ActorName, string? ActorRole, string? IpAddress) ResolveActor(
            string? actorId, string? actorName, string? actorRole)
        {
            if (!string.IsNullOrEmpty(actorId) && !string.IsNullOrEmpty(actorName))
            {
                return (actorId, actorName, actorRole, GetClientIp());
            }

            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                return (
                    user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system",
                    user.FindFirstValue(ClaimTypes.Name) ?? user.Identity?.Name ?? "Unknown",
                    user.FindFirstValue(ClaimTypes.Role),
                    GetClientIp()
                );
            }

            return ("system", "Hệ thống", null, null);
        }

        private string? GetClientIp()
        {
            var ctx = _httpContextAccessor.HttpContext;
            if (ctx == null) return null;
            return ctx.Connection.RemoteIpAddress?.ToString();
        }

        private static string ResolveModule(AdminActivityLog log)
        {
            if (!string.IsNullOrWhiteSpace(log.Module))
                return log.Module;

            return log.Action switch
            {
                ActivityActions.CreateAdmin or ActivityActions.CreateEmployee => ActivityModules.Staff,
                ActivityActions.Login => ActivityModules.Auth,
                _ => ActivityModules.Staff
            };
        }

        private static ActivityLogDTO MapToDto(AdminActivityLog log) => new()
        {
            Id = log.Id,
            ActorId = log.AdminId,
            ActorName = log.AdminName,
            ActorRole = log.ActorRole,
            Module = ResolveModule(log),
            Action = log.Action,
            EntityType = log.EntityType,
            EntityId = log.EntityId,
            TargetUserId = log.TargetUserId,
            Details = log.Details ?? string.Empty,
            IpAddress = log.IpAddress,
            CreatedAt = log.CreatedAt
        };
    }
}
