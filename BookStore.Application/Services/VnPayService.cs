using BookStore.Application.Configurations;
using BookStore.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using BookStore.Application.VnpayProvider;
using BookStore.Application.VnpayProvider.Models;
using BookStore.Application.VnpayProvider.Models.Enums;

namespace BookStore.Application.Services
{
    public class VnPayService : IVnPayService
    {
        private readonly VnPayConfig _config;
        private readonly IVnpayClient _vnpay;

        public VnPayService(IOptions<VnPayConfig> config, IVnpayClient vnpay)
        {
            _config = config.Value;
            _vnpay = vnpay;
        }

        public string CreatePaymentUrl(HttpContext httpContext, int orderId, decimal amount, string orderInfo)
        {
            // Sử dụng phương thức đơn giản của thư viện để tránh vấn đề internal set của PaymentId
            // Tuy nhiên, IVnpayClient.CreatePaymentUrl(double, string, BankCode) tự tạo PaymentId ngẫu nhiên
            // Nếu muốn dùng orderId, ta phải dùng VnpayPaymentRequest
            // Nhưng VnpayPaymentRequest.PaymentId là internal set. 
            // VÌ CHÚNG TA ĐÃ COPY CODE VÀO LOCAL, TA CÓ THỂ SỬA NÓ THÀNH PUBLIC!
            
            var request = new VnpayPaymentRequest
            {
                PaymentId = (long)orderId,
                Money = (double)amount,
                Description = orderInfo,
                BankCode = BankCode.ANY,
                CreatedTime = DateTime.Now // Sử dụng giờ địa phương (Việt Nam) thay vì Utc
            };

            var paymentUrlDetail = _vnpay.CreatePaymentUrl(request);
            Console.WriteLine("VNPAY URL: " + paymentUrlDetail.Url);
            return paymentUrlDetail.Url;
        }

        public bool ValidateCallback(IQueryCollection query)
        {
            try
            {
                var result = _vnpay.GetPaymentResult(query);
                return result != null;
            }
            catch
            {
                return false;
            }
        }

        private string GetIpAddress(HttpContext context)
        {
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            if (ipAddress == "::1") ipAddress = "127.0.0.1";
            return ipAddress;
        }
    }
}
