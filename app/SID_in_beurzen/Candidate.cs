using System;
using System.Collections.Generic;

namespace app.SID_in_beurzen;

public partial class Candidate
{
    public int CandidateId { get; set; }

    public string? GivenName { get; set; }

    public string? Surname { get; set; }

    public DateTime? BirthDate { get; set; }

    public string? Institution { get; set; }

    public string? FieldOfStudy { get; set; }

    public string? QrcodeLink { get; set; }

    public virtual ICollection<Registration> Registrations { get; set; } = new List<Registration>();
}
