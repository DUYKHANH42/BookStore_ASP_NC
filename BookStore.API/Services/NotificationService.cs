using BookStore.Application.Interfaces;
using BookStore.API.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using System;

namespace BookStore.API.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IUnitOfWork _unitOfWork;

        public NotificationService(IHubContext<NotificationHub> hubContext, IUnitOfWork unitOfWork)
        {
            _hubContext = hubContext;
            _unitOfWork = unitOfWork;
        }

        public async Task SendAdminNotificationAsync(string title, string message, string link)
        {
            try
            {
                // 1. Lưu vào Database để Admin xem sau
                var notification = new Notification
                {
                    Title = title,
                    Message = message,
                    Link = link,
                    CreatedAt = DateTime.Now
                };
                await _unitOfWork.Notifications.AddAsync(notification);
                await _unitOfWork.SaveChangesAsync();

                // 2. Bắn SignalR thời gian thực
                await _hubContext.Clients.Group("Admin").SendAsync("ReceiveOrderNotification", new
                {
                    title = title,
                    message = message,
                    link = link
                });
            }
            catch (Exception)
            {
                // Log error if needed
            }
        }
    }
}
