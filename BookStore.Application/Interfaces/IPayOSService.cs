using Net.payOS.Types;
using System.Threading.Tasks;

namespace BookStore.Application.Interfaces
{
    public interface IPayOSService
    {
        Task<CreatePaymentResult> CreatePaymentLinkAsync(int orderId, decimal amount, string orderNumber, string description);
        Task<string> ConfirmWebhookAsync(string webhookUrl);
        WebhookData VerifyWebhookData(WebhookType webhookType);
    }
}
