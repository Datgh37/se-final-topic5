using System;
using System.Collections.Generic;

namespace WebUITopic5_Team4.Models;

public partial class Account
{
    public string AccountId { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; }
    public int RoleId { get; set; }

    public virtual Role Role { get; set; } = null!;
    public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
