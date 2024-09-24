using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpAssassin.Commands;

public sealed class ReportIfSpamCommand : CommandBase
{
    public ReportIfSpamCommand(Stream body)
        : base(body)
    {
    }

    #region Properties

    public override string Name => "REPORT_IFSPAM";

    #endregion
}
