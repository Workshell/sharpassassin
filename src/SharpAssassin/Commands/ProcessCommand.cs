using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpAssassin.Commands;

public sealed class ProcessCommand : CommandBase
{
    public ProcessCommand(Stream body)
        : base(body)
    {
    }

    #region Properties

    public override string Name => "PROCESS";

    #endregion
}
