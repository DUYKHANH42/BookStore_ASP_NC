using BookStore.Application.Interfaces;
using BookStore.Domain.Entities;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace BookStore.Application.Services.Payment
{
    public class ZaloPayGateway : IPaymentGateway
    {
        private readonly IZaloPayService _zaloPayService;

        public ZaloPayGateway(IZaloPayService zaloPayService)
        {
            _zaloPayService = zaloPayService;
        }

        public PaymentMethod SupportedMethod => PaymentMethod.ZaloPay;

        public async Task<string?> CreatePaymentAsync(int orderId, decimal amount, string orderNumber, HttpContext? httpContext = null)
        {
            return await _zaloPayService.CreateOrderAsync(orderId, amount, orderNumber);
        }
    }
}
