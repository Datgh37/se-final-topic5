using System;
using System.Collections.Generic;

namespace WebUITopic5_Team4.Models;

public partial class Status
{
    public int StatusId { get; set; }
    public string StatusName { get; set; } = null!;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
