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
                var path = Path.Combine(contentPath, fileUrlOrName.Replace("/", "\\")); // Generic fallback
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
