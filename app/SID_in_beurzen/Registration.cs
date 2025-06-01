using System;
using System.Collections.Generic;

namespace app.SID_in_beurzen;

public partial class Registration
{
    public int RegistrationId { get; set; }

    public int? CandidateId { get; set; }

    public int? TradeShowId { get; set; }

    public string? Notes { get; set; }

    public DateTime? RegisteredAt { get; set; }

    public virtual Candidate? Candidate { get; set; }

    public virtual TradeShow? TradeShow { get; set; }

    public virtual ICollection<Program> Programs { get; set; } = new List<Program>();
}
