using System;

namespace BookStore.Domain.Entities
{
    public class AdminActivityLog
    {
        public int Id { get; set; }
        /// <summary>Id tài khoản thực hiện (Admin/Employee).</summary>
        public string AdminId { get; set; } = string.Empty;
        public string AdminName { get; set; } = string.Empty;
        public string? ActorRole { get; set; }
        /// <summary>Order, Product, Staff, Inventory...</summary>
        public string Module { get; set; } = "System";
        public string Action { get; set; } = string.Empty;
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }
        public string? TargetUserId { get; set; }
        public string Details { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
