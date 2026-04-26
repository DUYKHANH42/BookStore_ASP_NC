using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string AvtUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
        public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

        // Danh sách các đơn hàng của User này
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

        // Danh sách các đánh giá (Review) User đã viết
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public DateTime LastUpdatedAt { get; set; }
        public virtual ICollection<ShippingAddress> ShippingAddresses { get; set; } = new List<ShippingAddress>();
    }
}
