using BookStore.Application.Interfaces;
using BookStore.Domain.Entities;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace BookStore.Application.Services.Payment
{
    public class PayOSGateway : IPaymentGateway
    {
        private readonly IPayOSService _payOSService;

        public PayOSGateway(IPayOSService payOSService)
        {
            _payOSService = payOSService;
        }

        public PaymentMethod SupportedMethod => PaymentMethod.PayOS;

        public Task<string?> CreatePaymentAsync(int orderId, decimal amount, string orderNumber, HttpContext? httpContext = null)
        {
            throw new NotSupportedException("Phương thức thanh toán PayOS hiện đang bảo trì.");
        }
    }
}
