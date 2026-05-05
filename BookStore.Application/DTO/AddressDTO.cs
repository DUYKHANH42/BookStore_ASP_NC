using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.DTO
{
    public class AddressDTO
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên người nhận là bắt buộc")]
        public string ReceiverName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ chi tiết là bắt buộc")]
        public string AddressLine { get; set; } = string.Empty;

        public bool IsDefault { get; set; }
    }
}
