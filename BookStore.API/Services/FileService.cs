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
            if (file == null || file.Length == 0) return string.Empty;

            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
            {
                return string.Empty;
            }

            var rootPath = _environment.WebRootPath ?? Directory.GetCurrentDirectory();
            var uploadsBase = Path.Combine(rootPath, "uploads");

            var normalizedFolder = (folderName ?? string.Empty)
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar);

            var targetFolder = Path.Combine(uploadsBase, normalizedFolder);

            if (!IsSubPathOf(uploadsBase, targetFolder))
            {
                return string.Empty;
            }

            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
            }

            var fileName = $"{Guid.NewGuid()}{ext}";
            var fileNameWithPath = Path.Combine(targetFolder, fileName);

            if (!IsSubPathOf(uploadsBase, fileNameWithPath))
            {
                return string.Empty;
            }

            using var stream = new FileStream(fileNameWithPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return fileName;
        }

        public Task<bool> DeleteFileAsync(string fileUrlOrName)
        {
            if (string.IsNullOrEmpty(fileUrlOrName)) return Task.FromResult(false);

            try
            {
                var rootPath = _environment.WebRootPath ?? Directory.GetCurrentDirectory();
                var normalizedPath = fileUrlOrName
                    .Replace('\\', Path.DirectorySeparatorChar)
                    .Replace('/', Path.DirectorySeparatorChar);

                var path = Path.Combine(rootPath, normalizedPath);

                if (!IsSubPathOf(rootPath, path))
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

        private static bool IsSubPathOf(string basePath, string subPath)
        {
            var fullBase = Path.GetFullPath(basePath);
            var fullSub = Path.GetFullPath(subPath);

            if (string.Equals(fullBase, fullSub, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!fullBase.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                fullBase += Path.DirectorySeparatorChar;
            }

            return fullSub.StartsWith(fullBase, StringComparison.OrdinalIgnoreCase);
        }
    }
}
