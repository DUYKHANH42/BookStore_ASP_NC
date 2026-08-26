using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace BookStore.Application.Interfaces
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(HttpContext httpContext, int orderId, decimal amount, string orderInfo);
        bool ValidateCallback(IQueryCollection query);
    }
}
