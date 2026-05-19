using System;
using System.Collections.Generic;

namespace WebUITopic5_Team4.Models;

public partial class Role
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = null!;

    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();
}
