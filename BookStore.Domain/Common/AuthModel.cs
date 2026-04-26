using BookStore.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Domain.Common
{
    public class AuthModel
    {
        public string Token { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
        public ApplicationUser User { get; set; } = null!;
        public IList<string> Roles { get; set; } = new List<string>();
        public bool IsActive { get; set; } = true;
        public string RefreshToken { get; set; } = string.Empty; 
    }
}
