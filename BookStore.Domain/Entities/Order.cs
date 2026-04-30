using BookStore.Domain.Entities;
using System;
using System.Collections.Generic;

using System.ComponentModel.DataAnnotations.Schema;

public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty; // Mã đơn hàng (ORD-123...)

    public string? UserId { get; set; }
    public virtual ApplicationUser User { get; set; } = null!;

    // THÔNG TIN CHỐT KHI ĐẶT HÀNG (Copy từ ShippingAddress sang)
    public string ShippingName { get; set; } = string.Empty;
    public string ShippingPhone { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPrice { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.COD;

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}

public enum PaymentMethod
{
    COD, 
    CreditCard,
    PayPal,
    BankTransfer,
    ZaloPay
}
public enum OrderStatus
{
    Pending,      
    Paid,        
    Cancelled,    
    Shipping,    
    Completed     
}