using BookStore.Application.Interfaces;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BookStore.Infrastructure.Services
{
    public class CloudinaryService : IFileService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IConfiguration configuration)
        {
            var section = configuration.GetSection("Cloudinary");
            var account = new Account(
                section["CloudName"],
                section["ApiKey"],
                section["ApiSecret"]
            );
            _cloudinary = new Cloudinary(account);
            _cloudinary.Api.Secure = true;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string folderName)
        {
            if (file == null || file.Length == 0) return string.Empty;

            using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = $"BookStore/{folderName}", // Đặt vào thư mục gốc BookStore
                DisplayName = file.FileName
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
            
            if (uploadResult.Error != null)
            {
                throw new Exception($"Cloudinary upload error: {uploadResult.Error.Message}");
            }

            return uploadResult.SecureUrl.ToString();
        }

        public async Task<bool> DeleteFileAsync(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl)) return false;

            // Extract Public ID from URL string. 
            // E.g: https://res.cloudinary.com/cloud_name/image/upload/v162343/BookStore/folder/my_image_name.jpg
            var publicId = ExtractPublicId(fileUrl);
            if (string.IsNullOrEmpty(publicId)) return false;

            var deletionParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deletionParams);

            return result.Result == "ok";
        }

        private string ExtractPublicId(string url)
        {
            try
            {
                // Cloudinary doesn't store extension natively inside the direct PublicId unless you configure it differently.
                // Normally Public ID goes after '/upload/v[digits]/' or just '/upload/'
                
                if (!url.Contains("/upload/")) return string.Empty;

                var segments = url.Split(new[] { "/upload/" }, StringSplitOptions.None);
                if (segments.Length < 2) return string.Empty;

                // Trích xuất phần sau '/upload/' (thường bắt đầu bằng 'v12345/' hoặc trực tiếp là folder/id)
                var afterUpload = segments[1];
                
                // Bỏ qua version string nếu có (ví dụ v1698273/)
                var slashIndex = afterUpload.IndexOf('/');
                if (slashIndex != -1 && afterUpload.StartsWith("v") && long.TryParse(afterUpload.Substring(1, slashIndex - 1), out _))
                {
                    afterUpload = afterUpload.Substring(slashIndex + 1);
                }

                // Bỏ đuôi file (.png, .jpg...)
                var dotIndex = afterUpload.LastIndexOf('.');
                if (dotIndex != -1)
                {
                    afterUpload = afterUpload.Substring(0, dotIndex);
                }

                return afterUpload;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
