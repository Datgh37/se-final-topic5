using System;
using System.Collections.Generic;

namespace WebUITopic5_Team4.Models;

public partial class Order
{
    public int OrderId { get; set; }
    public string? AccountId { get; set; }
    public DateTime OrderDate { get; set; }
    public string FullName { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string TownCity { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string? Email { get; set; }
    public string? OrderNotes { get; set; }
    public string PaymentMethod { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public int StatusId { get; set; }

    public virtual Account? Account { get; set; }
    public virtual Status Status { get; set; } = null!;
    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
