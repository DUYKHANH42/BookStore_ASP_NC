using BookStore.Application.Configurations;
using Microsoft.Extensions.Options;
using Net.payOS;
using Net.payOS.Types;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using BookStore.Application.Interfaces;

namespace BookStore.Application.Services
{
    public class PayOSService : IPayOSService
    {
        private readonly PayOS _payOS;

        public PayOSService(IOptions<PayOSConfig> config)
        {
            var settings = config.Value;
            _payOS = new PayOS(settings.ClientId, settings.ApiKey, settings.ChecksumKey);
        }

        public async Task<CreatePaymentResult> CreatePaymentLinkAsync(int orderId, decimal amount, string orderNumber, string description)
        {
            // Mã đơn hàng phải là kiểu long (ví dụ dùng Ticks)
            long orderCode = long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss")); 
            
            var paymentData = new PaymentData(
                orderCode: orderCode,
                amount: (int)amount,
                description: description,
                items: new List<ItemData>(), 
                cancelUrl: "http://localhost:4200/checkout",
                returnUrl: "http://localhost:4200/orders"
            );

            return await _payOS.createPaymentLink(paymentData);
        }

        public async Task<string> ConfirmWebhookAsync(string webhookUrl)
        {
            return await _payOS.confirmWebhook(webhookUrl);
        }

        public WebhookData VerifyWebhookData(WebhookType webhookType)
        {
            return _payOS.verifyPaymentWebhookData(webhookType);
        }
    }
}
