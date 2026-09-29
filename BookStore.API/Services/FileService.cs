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

        private static string EnsureTrailingSeparator(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            return path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? path
                : path + Path.DirectorySeparatorChar;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string folderName)
        {
            if (file == null) return string.Empty;

            var webRootPath = _environment.WebRootPath ?? Path.GetFullPath("wwwroot");
            var uploadsBasePath = Path.GetFullPath(Path.Combine(webRootPath, "uploads"));
            var uploadsBasePathWithSeparator = EnsureTrailingSeparator(uploadsBasePath);

            var path = Path.GetFullPath(Path.Combine(uploadsBasePath, folderName ?? string.Empty));

            // Prevent Path Traversal (CWE-22): Ensure destination path is strictly within uploads directory
            if (!EnsureTrailingSeparator(path).StartsWith(uploadsBasePathWithSeparator, StringComparison.OrdinalIgnoreCase) &&
                !path.Equals(uploadsBasePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Invalid upload path specified.", nameof(folderName));
            }

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
                var webRootPath = Path.GetFullPath(_environment.WebRootPath ?? Path.GetFullPath("wwwroot"));
                var webRootPathWithSeparator = EnsureTrailingSeparator(webRootPath);

                var normalizedRelativePath = fileUrlOrName.Replace("/", Path.DirectorySeparatorChar.ToString()).Replace("\\", Path.DirectorySeparatorChar.ToString());
                var fullPath = Path.GetFullPath(Path.Combine(webRootPath, normalizedRelativePath));

                // Prevent Path Traversal (CWE-22): Ensure target file is strictly within webRootPath
                if (!fullPath.StartsWith(webRootPathWithSeparator, StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(false);
                }

                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                    return Task.FromResult(true);
                }
            }
            catch { }

            return Task.FromResult(false);
        }
    }
}
