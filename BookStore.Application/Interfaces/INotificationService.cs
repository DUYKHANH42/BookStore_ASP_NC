using System.Threading.Tasks;

namespace BookStore.Application.Interfaces
{
    public interface INotificationService
    {
        Task SendAdminNotificationAsync(string title, string message, string link);
    }
}
