using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace BookStore.API.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = "Cookies")]
    public class SettingsController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        public SettingsController(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            return View(await LoadSettingsAsync());
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            return Json(await LoadSettingsAsync());
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromForm] AdminAppSettings settings)
        {
            await SaveSettingsAsync(settings);
            return Json(new { success = true, message = "Đã lưu cấu hình ứng dụng." });
        }

        private string SettingsPath
        {
            get
            {
                // Security: Store configuration in ContentRootPath/App_Data to prevent public static file exposure via wwwroot
                var folder = Path.Combine(_env.ContentRootPath, "App_Data");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                return Path.Combine(folder, "admin_appsettings.json");
            }
        }

        private async Task<AdminAppSettings> LoadSettingsAsync()
        {
            var legacyPath = Path.Combine(_env.WebRootPath, "admin", "appsettings.json");

            if (!System.IO.File.Exists(SettingsPath))
            {
                // Security migration: Move legacy settings from public wwwroot to secure App_Data and delete public file
                if (System.IO.File.Exists(legacyPath))
                {
                    try
                    {
                        var legacyContent = await System.IO.File.ReadAllTextAsync(legacyPath);
                        await System.IO.File.WriteAllTextAsync(SettingsPath, legacyContent);
                        System.IO.File.Delete(legacyPath);
                    }
                    catch { }
                }

                if (!System.IO.File.Exists(SettingsPath))
                {
                    var defaults = new AdminAppSettings();
                    await SaveSettingsAsync(defaults);
                    return defaults;
                }
            }
            else if (System.IO.File.Exists(legacyPath))
            {
                // Ensure legacy public file is removed if secure file already exists
                try { System.IO.File.Delete(legacyPath); } catch { }
            }

            var json = await System.IO.File.ReadAllTextAsync(SettingsPath);
            if (string.IsNullOrWhiteSpace(json)) return new AdminAppSettings();
            return JsonSerializer.Deserialize<AdminAppSettings>(json) ?? new AdminAppSettings();
        }

        private async Task SaveSettingsAsync(AdminAppSettings settings)
        {
            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            await System.IO.File.WriteAllTextAsync(SettingsPath, json);
        }
    }

    public class AdminAppSettings
    {
        public string StoreName { get; set; } = "Lumen BookStore";
        public string StorePhone { get; set; } = "028.123.4567";
        public string StoreAddress { get; set; } = "TP. Hồ Chí Minh";
        public string BankCode { get; set; } = "MB";
        public string BankName { get; set; } = "MB Bank";
        public string BankAccountNumber { get; set; } = "";
        public string BankAccountName { get; set; } = "BOOK STORE";
    }
}
