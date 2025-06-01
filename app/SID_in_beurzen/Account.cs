using System;
using System.Collections.Generic;

namespace app.SID_in_beurzen;

public partial class Account
{
    public int AccountId { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? EmailAddress { get; set; }

    public string? PasswordHash { get; set; }

    public int? RoleTypeId { get; set; }

    public bool? IsActive { get; set; }

    public Guid? RegistrationToken { get; set; }

    public virtual RoleType? RoleType { get; set; }
}
