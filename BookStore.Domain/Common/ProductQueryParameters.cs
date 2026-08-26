namespace BookStore.Domain.Common
{
    public class ProductQueryParameters
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public string? SortBy { get; set; } // e.g., "price_asc", "price_desc", "newest"
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool IsFlashSale { get; set; }
        public int? CategoryId { get; set; }
        public int? SubCategoryId { get; set; }
        public bool? IsActive { get; set; } = true; // Mặc định chỉ hiện sản phẩm đang hoạt động
    }
}
