namespace BookStore.Application.Configurations
{
    public class ZaloPayConfig
    {
        public static string ConfigName => "ZaloPay";
        public string AppId { get; set; } = string.Empty;
        public string Key1 { get; set; } = string.Empty;
        public string Key2 { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public string CallbackUrl { get; set; } = string.Empty;
    }
}
