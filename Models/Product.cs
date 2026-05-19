using System;
using System.Collections.Generic;

namespace WebUITopic5_Team4.Models;

public partial class Product
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public int CategoryId { get; set; }
    public decimal UnitPrice { get; set; }
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public int StockQuantity { get; set; }

    public virtual Category Category { get; set; } = null!;
    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
