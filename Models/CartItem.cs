using System;
using System.Collections.Generic;

namespace WebUITopic5_Team4.Models;

public partial class CartItem
{
    public int CartItemId { get; set; }
    public string CartId { get; set; } = null!;
    public int ProductId { get; set; }
    public int Quantity { get; set; }

    public virtual Cart Cart { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}
