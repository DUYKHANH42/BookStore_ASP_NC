using BookStore.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace BookStore.API.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = "Cookies")]
    public class NotificationController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public NotificationController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<IActionResult> GetLatest()
        {
            var notifications = await _unitOfWork.Notifications.GetAllAsync();
            var latest = notifications.OrderByDescending(n => n.CreatedAt).Take(10).ToList();
            return Json(latest);
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await _unitOfWork.Notifications.GetByIdAsync(id);
            if (notification != null)
            {
                notification.IsRead = true;
                await _unitOfWork.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }
    }
}
