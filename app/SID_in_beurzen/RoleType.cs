using System;
using System.Collections.Generic;

namespace app.SID_in_beurzen;

public partial class RoleType
{
    public int RoleTypeId { get; set; }

    public string RoleName { get; set; } = null!;

    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();
}
