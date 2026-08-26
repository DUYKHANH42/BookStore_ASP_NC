using System;

namespace BookStore.Application.DTO
{
    public class ActivityLogDTO
    {
        public int Id { get; set; }
        public string ActorId { get; set; } = string.Empty;
        public string ActorName { get; set; } = string.Empty;
        public string? ActorRole { get; set; }
        public string Module { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }
        public string? TargetUserId { get; set; }
        public string Details { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        public DateTime CreatedAt { get; set; }

        public string ActionLabel => Action switch
        {
            "LOGIN" => "Đăng nhập",
            "CREATE" => "Tạo mới",
            "UPDATE" => "Cập nhật",
            "DELETE" => "Xóa",
            "STATUS_CHANGE" => "Đổi trạng thái",
            "IMPORT" => "Nhập kho",
            "PASSWORD_RESET" => "Reset mật khẩu",
            "CREATE_ADMIN" => "Tạo Admin",
            "CREATE_EMPLOYEE" => "Tạo Employee",
            _ => Action ?? "—"
        };

        public string ModuleLabel => (Module ?? "System") switch
        {
            "Order" => "Đơn hàng",
            "Product" => "Sản phẩm",
            "Inventory" => "Tồn kho",
            "FlashSale" => "Flash Sale",
            "Customer" => "Khách hàng",
            "Staff" => "Nhân sự",
            "Category" => "Danh mục",
            "Profile" => "Hồ sơ",
            "Auth" => "Đăng nhập",
            "Review" => "Đánh giá",
            "System" => "Hệ thống",
            _ => Module ?? "—"
        };
    }

    public class ActivityLogFilterDTO
    {
        public string? Search { get; set; }
        public string? Module { get; set; }
        public string? Action { get; set; }
        public string? ActorId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
