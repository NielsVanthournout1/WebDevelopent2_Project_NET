using System;
using System.Collections.Generic;

namespace app.SID_in_beurzen;

public partial class TradeShow
{
    public int TradeShowId { get; set; }

    public string? Title { get; set; }

    public DateTime? EventDate { get; set; }

    public string? Venue { get; set; }

    public virtual ICollection<Registration> Registrations { get; set; } = new List<Registration>();
}
