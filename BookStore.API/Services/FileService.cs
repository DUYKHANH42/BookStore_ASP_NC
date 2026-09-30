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

        private string GetNormalizedRootPath()
        {
            var rootPath = Path.GetFullPath(_environment.WebRootPath ?? Directory.GetCurrentDirectory());
            if (!rootPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                rootPath += Path.DirectorySeparatorChar;
            }
            return rootPath;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string folderName)
        {
            if (file == null) return string.Empty;

            var rootPath = GetNormalizedRootPath();
            var path = Path.GetFullPath(Path.Combine(rootPath, "uploads", folderName ?? string.Empty));

            // Prevent Path Traversal (CWE-22) outside WebRootPath
            if (!path.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Invalid upload folder path.", nameof(folderName));
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
                var rootPath = GetNormalizedRootPath();
                var normalizedRelPath = fileUrlOrName.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
                var fullPath = Path.GetFullPath(Path.Combine(rootPath, normalizedRelPath));

                // Prevent Path Traversal (CWE-22) outside WebRootPath
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
