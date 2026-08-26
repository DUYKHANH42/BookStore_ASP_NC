using System;
using System.Collections.Generic;
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

    public class FlashSaleCampaignDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsActive { get; set; }
        public string Status { get; set; } = string.Empty;
        public int ProductCount { get; set; }
        public int TotalSold { get; set; }
        public int TotalStock { get; set; }
    }

    public class FlashSaleCampaignCreateDTO
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        public List<FlashSaleItemCreateDTO> Items { get; set; } = new List<FlashSaleItemCreateDTO>();
    }

    public class FlashSaleItemCreateDTO
    {
        public int ProductId { get; set; }
        public decimal SalePrice { get; set; }
        public int SaleStock { get; set; }
    }

    public class FlashSaleManagementDTO
    {
        public int Id { get; set; }
        public int FlashSaleCampaignId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public decimal OriginalPrice { get; set; }
        public decimal SalePrice { get; set; }
        public int SaleStock { get; set; }
        public int SoldCount { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsActive { get; set; }
        public string Status { get; set; } = string.Empty; 
    }

    public class FlashSaleCampaignEditDTO
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên chiến dịch là bắt buộc")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Thời gian bắt đầu là bắt buộc")]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "Thời gian kết thúc là bắt buộc")]
        public DateTime EndTime { get; set; }

        public bool IsActive { get; set; }

        public List<FlashSaleItemEditDTO> Items { get; set; } = new List<FlashSaleItemEditDTO>();
    }

    public class FlashSaleItemEditDTO
    {
        public int? Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public decimal OriginalPrice { get; set; }
        public decimal SalePrice { get; set; }
        public int SaleStock { get; set; }
        public int SoldCount { get; set; }
    }
}
