namespace BookStore.Application.Configurations
{
    public class VnPayConfig
    {
        public const string ConfigName = "VnPay";
        public string TmnCode { get; set; } = string.Empty;
        public string HashSecret { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public string Command { get; set; } = "pay";
        public string CurrCode { get; set; } = "VND";
        public string Version { get; set; } = "2.1.0";
        public string ReturnUrl { get; set; } = string.Empty;
    }
}
