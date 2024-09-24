using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpAssassin.Commands;

public sealed class ReportCommand : CommandBase
{
    public ReportCommand(Stream body)
        : base(body)
    {
    }

    #region Properties

    public override string Name => "REPORT";

    #endregion
}
