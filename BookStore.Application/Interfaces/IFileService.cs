using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace BookStore.Application.Interfaces
{
    public interface IFileService
    {
        Task<string> SaveFileAsync(IFormFile file, string folderName);
        Task<bool> DeleteFileAsync(string fileUrl);
    }
}
