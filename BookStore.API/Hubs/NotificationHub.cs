using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace BookStore.API.Hubs
{
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        public async Task JoinAdminGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Admin");
        }

        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        // Method specifically for order notifications
        public async Task SendOrderNotification(string title, string message, string link)
        {
            await Clients.Group("Admin").SendAsync("ReceiveOrderNotification", new { title, message, link });
        }
    }
}
