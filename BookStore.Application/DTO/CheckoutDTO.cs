using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.DTO
{
    public class CheckoutDTO
    {
        [Required(ErrorMessage = "Tên người nhận là bắt buộc")]
        public string ShippingName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string ShippingPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ giao hàng là bắt buộc")]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
        public PaymentMethod PaymentMethod { get; set; }
    }
}
