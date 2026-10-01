using BookStore.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BookStore.API.Services
{
    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _environment;
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        public FileService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string folderName)
        {
            if (file == null) return string.Empty;

            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
            {
                throw new ArgumentException("Chỉ chấp nhận các tệp hình ảnh hợp lệ (.jpg, .jpeg, .png, .gif, .webp).");
            }

            var contentPath = Path.GetFullPath(_environment.WebRootPath);
            var path = Path.GetFullPath(Path.Combine(contentPath, "uploads", folderName ?? string.Empty));

            if (!path.StartsWith(contentPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Thư mục lưu trữ không hợp lệ.");
            }

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var fileName = $"{Guid.NewGuid()}{ext}";
            var fileNameWithPath = Path.Combine(path, fileName);

            using var stream = new FileStream(fileNameWithPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return fileName;
        }

        public Task<bool> DeleteFileAsync(string fileUrlOrName)
        {
            if (string.IsNullOrEmpty(fileUrlOrName)) return Task.FromResult(false);

            try 
            {
                var contentPath = Path.GetFullPath(_environment.WebRootPath);
                var relativePath = fileUrlOrName.Replace("/", Path.DirectorySeparatorChar.ToString())
                                                .Replace("\\", Path.DirectorySeparatorChar.ToString());
                var path = Path.GetFullPath(Path.Combine(contentPath, relativePath));

                // Path Traversal Guard: Ensure path is within WebRootPath
                if (!path.StartsWith(contentPath, StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(false);
                }

                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                    return Task.FromResult(true);
                }
            }
            catch { }

            return Task.FromResult(false);
        }
    }
}
