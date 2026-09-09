using BookStore.Application.Interfaces;
using BookStore.Domain.Entities;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace BookStore.Application.Services.Payment
{
    public class VNPayGateway : IPaymentGateway
    {
        private readonly IVnPayService _vnPayService;

        public VNPayGateway(IVnPayService vnPayService)
        {
            _vnPayService = vnPayService;
        }

        public PaymentMethod SupportedMethod => PaymentMethod.VNPay;

        public Task<string?> CreatePaymentAsync(int orderId, decimal amount, string orderNumber, HttpContext? httpContext = null)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext), "HttpContext is required for VNPay payment creation.");
            }

            var url = _vnPayService.CreatePaymentUrl(httpContext, orderId, amount, $"ThanhToanDonHang_{orderNumber}");
            return Task.FromResult<string?>(url);
        }
    }
}
