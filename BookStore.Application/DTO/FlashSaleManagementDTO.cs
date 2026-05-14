using System;
using System.ComponentModel.DataAnnotations;

namespace BookStore.Application.DTO
{
    public class FlashSaleBaseDTO
    {
        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Giá sale phải lớn hơn hoặc bằng 0")]
        public decimal SalePrice { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng sale phải ít nhất là 1")]
        public int SaleStock { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }
    }

    public class FlashSaleCreateDTO : FlashSaleBaseDTO
    {
        [Required]
        public int ProductId { get; set; }
    }

    public class FlashSaleUpdateDTO : FlashSaleBaseDTO
    {
        [Required]
        public int Id { get; set; }
    }

    public class FlashSaleManagementDTO
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal OriginalPrice { get; set; }
        public decimal SalePrice { get; set; }
        public int SaleStock { get; set; }
        public int SoldCount { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsActive { get; set; }
        public string Status { get; set; } = string.Empty; // Upcoming, Active, Ended
    }
}
