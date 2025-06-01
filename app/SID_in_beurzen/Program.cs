using System;
using System.Collections.Generic;

namespace app.SID_in_beurzen;

public partial class Program
{
    public int ProgramId { get; set; }

    public string? ProgramName { get; set; }

    public virtual ICollection<Registration> Registrations { get; set; } = new List<Registration>();
}
