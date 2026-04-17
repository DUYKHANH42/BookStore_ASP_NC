using BookStore.Domain.Entities;
using System;
using System.Collections.Generic;

public class Order
{
    public int Id { get; set; }

    public string? UserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public decimal TotalPrice { get; set; }

    public OrderStatus Status { get; set; } // Pending, Paid, Cancelled

    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    public Customer Customer { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
public enum OrderStatus
{
    Pending,
    Paid,
    Cancelled
}