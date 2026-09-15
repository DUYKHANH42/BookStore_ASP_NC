using BookStore.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BookStore.API.Services
{
    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _environment;

        public FileService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string folderName)
        {
            if (file == null) return string.Empty;

            var contentPath = _environment.WebRootPath;
            var path = Path.Combine(contentPath, "uploads", folderName);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var ext = Path.GetExtension(file.FileName);
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
                var contentPath = _environment.WebRootPath;
                if (string.IsNullOrEmpty(contentPath)) return Task.FromResult(false);

                // Security: Sanitize and prevent Path Traversal / Arbitrary File Deletion
                var fullRootPath = Path.GetFullPath(contentPath);
                var rootWithSep = fullRootPath.EndsWith(Path.DirectorySeparatorChar.ToString())
                    ? fullRootPath
                    : fullRootPath + Path.DirectorySeparatorChar;

                var relativePath = fileUrlOrName.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
                var fullFilePath = Path.GetFullPath(Path.Combine(fullRootPath, relativePath));

                // Reject any path traversal attempt trying to break out of WebRootPath
                if (!fullFilePath.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase) &&
                    !fullFilePath.Equals(fullRootPath, StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(false);
                }

                if (System.IO.File.Exists(fullFilePath))
                {
                    System.IO.File.Delete(fullFilePath);
                    return Task.FromResult(true);
                }
            }
            catch { }

            return Task.FromResult(false);
        }
    }
}
