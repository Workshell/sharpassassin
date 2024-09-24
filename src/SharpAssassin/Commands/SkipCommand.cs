using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpAssassin.Commands;

public sealed class SkipCommand : CommandBase
{
    public SkipCommand()
        : base(new NullStream())
    {
    }

    #region Properties

    public override string Name => "SKIP";
    public override bool SupportsHeaders => false;
    public override bool SupportsBody => false;

    #endregion
}
