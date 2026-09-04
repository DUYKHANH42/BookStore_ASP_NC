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

            var rootPath = Path.GetFullPath(_environment.WebRootPath);
            if (!rootPath.EndsWith(Path.DirectorySeparatorChar))
            {
                rootPath += Path.DirectorySeparatorChar;
            }

            var targetDir = Path.GetFullPath(Path.Combine(rootPath, "uploads", folderName));

            // Prevent Path Traversal in folderName
            if (!targetDir.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase) && !targetDir.Equals(rootPath.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Invalid folder path.");
            }

            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            var ext = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var fileNameWithPath = Path.Combine(targetDir, fileName);

            using var stream = new FileStream(fileNameWithPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return fileName;
        }

        public Task<bool> DeleteFileAsync(string fileUrlOrName)
        {
            if (string.IsNullOrEmpty(fileUrlOrName)) return Task.FromResult(false);

            try 
            {
                var rootPath = Path.GetFullPath(_environment.WebRootPath);
                if (!rootPath.EndsWith(Path.DirectorySeparatorChar))
                {
                    rootPath += Path.DirectorySeparatorChar;
                }

                var normalizedRelativePath = fileUrlOrName.Replace("/", Path.DirectorySeparatorChar.ToString())
                                                         .Replace("\\", Path.DirectorySeparatorChar.ToString())
                                                         .TrimStart(Path.DirectorySeparatorChar);
                var fullPath = Path.GetFullPath(Path.Combine(rootPath, normalizedRelativePath));

                // Path Traversal check: Ensure fullPath is inside WebRootPath
                if (!fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
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
