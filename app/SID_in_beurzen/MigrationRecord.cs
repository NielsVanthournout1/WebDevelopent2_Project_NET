using System;
using System.Collections.Generic;

namespace app.SID_in_beurzen;

public partial class MigrationRecord
{
    public string MigrationKey { get; set; } = null!;

    public string VersionInfo { get; set; } = null!;
}
