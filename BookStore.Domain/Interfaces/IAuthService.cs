using BookStore.Domain.Common;
using BookStore.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Domain.Interfaces
{
    public interface IAuthService
    {
        Task<IdentityResult> RegisterAsync(ApplicationUser user, string password, string role);
        Task<AuthModel?> LoginAsync(string email, string password);

        // Kiểm tra Email đã tồn tại chưa (Dùng cho Async Validator bên Angular)
        Task<bool> IsEmailUniqueAsync(string email);

        // Sau này có thể thêm: ChangePassword, ResetPassword...
    }
}
