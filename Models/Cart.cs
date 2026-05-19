using System;
using System.Collections.Generic;

namespace WebUITopic5_Team4.Models;

public partial class Cart
{
    public string CartId { get; set; } = null!;
    public string? AccountId { get; set; }

    public virtual Account? Account { get; set; }
    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
}
