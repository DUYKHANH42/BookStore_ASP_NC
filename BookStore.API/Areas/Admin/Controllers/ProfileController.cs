using BookStore.Application.DTO;
using BookStore.Application.Interfaces;
using BookStore.Application.Services;
using BookStore.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BookStore.API.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = "Cookies")]
    public class ProfileController : Controller
    {
        private readonly AdminProfileService _profileService;
        private readonly IActivityLogService _activityLog;

        public ProfileController(AdminProfileService profileService, IActivityLogService activityLog)
        {
            _profileService = profileService;
            _activityLog = activityLog;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var profile = await _profileService.GetCurrentProfileAsync();
            if (profile == null) return RedirectToAction("Login", "Auth", new { area = "" });
            return View(profile);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateAdminProfileDTO dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values);
                return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
            }

            var result = await _profileService.UpdateProfileAsync(dto);
            if (result)
            {
                await _activityLog.LogAsync(
                    ActivityModules.Profile,
                    ActivityActions.Update,
                    $"Cập nhật hồ sơ: {dto.FullName}");
            }
            return Json(new { success = result, message = result ? "Cập nhật hồ sơ thành công!" : "Không thể cập nhật hồ sơ" });
        }

        [HttpPost]
        public async Task<IActionResult> UploadAvatar(IFormFile avatar)
        {
            if (avatar == null || avatar.Length == 0)
                return Json(new { success = false, message = "Vui lòng chọn ảnh đại diện" });

            var result = await _profileService.UpdateAvatarAsync(avatar);
            if (!result)
                return Json(new { success = false, message = "Tải ảnh thất bại" });

            await _activityLog.LogAsync(ActivityModules.Profile, ActivityActions.Update, "Cập nhật ảnh đại diện");
            var profile = await _profileService.GetCurrentProfileAsync();
            return Json(new { success = true, message = "Cập nhật ảnh đại diện thành công!", avatarUrl = profile?.DisplayAvatar });
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword([FromBody] ChangeAdminPasswordDTO dto)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Dữ liệu không hợp lệ" });

            var result = await _profileService.ChangePasswordAsync(dto);
            if (result)
            {
                await _activityLog.LogAsync(ActivityModules.Profile, ActivityActions.Update, "Đổi mật khẩu tài khoản");
            }
            return Json(new
            {
                success = result,
                message = result ? "Đổi mật khẩu thành công!" : "Mật khẩu hiện tại không đúng hoặc không thể đổi mật khẩu"
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetHeaderInfo()
        {
            var profile = await _profileService.GetCurrentProfileAsync();
            if (profile == null) return Json(new { success = false });
            return Json(new
            {
                success = true,
                fullName = profile.FullName,
                department = profile.Department,
                email = profile.Email,
                avatarUrl = profile.DisplayAvatar,
                initials = profile.Initials
            });
        }
    }
}
