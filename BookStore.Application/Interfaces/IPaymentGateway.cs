using BookStore.Domain.Entities;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace BookStore.Application.Interfaces
{
    public interface IPaymentGateway
    {
        PaymentMethod SupportedMethod { get; }
        Task<string?> CreatePaymentAsync(int orderId, decimal amount, string orderNumber, HttpContext? httpContext = null);
    }
}
