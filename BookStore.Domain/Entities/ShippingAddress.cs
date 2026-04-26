using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Domain.Entities
{
    public class ShippingAddress
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser User { get; set; } = null!;

        public string ReceiverName { get; set; } = string.Empty;   // Tên người nhận
        public string PhoneNumber { get; set; } = string.Empty;    // SĐT người nhận
        public string AddressLine { get; set; } = string.Empty;    // Địa chỉ chi tiết

        public bool IsDefault { get; set; } = false; // Địa chỉ mặc định
    }
}
