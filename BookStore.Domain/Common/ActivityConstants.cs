namespace BookStore.Domain.Common
{
    /// <summary>
    /// Hằng số module & hành động cho nhật ký hoạt động (audit trail).
    /// Tách biệt với bảng Notifications (chỉ cảnh báo đơn hàng real-time).
    /// </summary>
    public static class ActivityModules
    {
        public const string Auth = "Auth";
        public const string Order = "Order";
        public const string Product = "Product";
        public const string Inventory = "Inventory";
        public const string FlashSale = "FlashSale";
        public const string Customer = "Customer";
        public const string Staff = "Staff";
        public const string Category = "Category";
        public const string Profile = "Profile";
        public const string Review = "Review";
    }

    public static class ActivityActions
    {
        public const string Login = "LOGIN";
        public const string Create = "CREATE";
        public const string Update = "UPDATE";
        public const string Delete = "DELETE";
        public const string StatusChange = "STATUS_CHANGE";
        public const string Import = "IMPORT";
        public const string PasswordReset = "PASSWORD_RESET";
        public const string CreateAdmin = "CREATE_ADMIN";
        public const string CreateEmployee = "CREATE_EMPLOYEE";
    }
}
