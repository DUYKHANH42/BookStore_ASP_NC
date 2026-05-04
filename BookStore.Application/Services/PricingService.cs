using BookStore.Domain.Entities;
using System;
using System.Linq;

namespace BookStore.Application.Services
{
    public class PricingService
    {
        public decimal GetEffectivePrice(Product product, int quantity)
        {
            if (product == null) return 0;

            var now = DateTime.Now;
            // Tìm Flash Sale đang hoạt động
            var activeSale = product.FlashSales?.FirstOrDefault(s => 
                s.IsActive && 
                s.StartTime <= now && 
                s.EndTime >= now && 
                s.SoldCount < s.SaleStock);

            if (activeSale != null)
            {
                int availableSaleSlots = activeSale.SaleStock - activeSale.SoldCount;
                // Nếu số lượng mua nằm trong suất sale còn lại
                if (quantity <= availableSaleSlots)
                {
                    return activeSale.SalePrice;
                }
            }

            return product.Price;
        }
    }
}
