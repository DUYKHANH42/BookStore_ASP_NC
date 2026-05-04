using System.Threading.Tasks;

namespace BookStore.Application.Interfaces
{
    public interface IZaloPayService
    {
        Task<string?> CreateOrderAsync(int orderId, decimal amount, string orderNumber);
        bool ValidateCallback(string data, string requestMac);
    }
}
