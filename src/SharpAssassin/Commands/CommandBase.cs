using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpAssassin.Commands;

public abstract class CommandBase
{
    private Stream _body;

    protected CommandBase() 
        : this(new NullStream())
    {
    }

    protected CommandBase(Stream body)
    {
        Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        Body = body;
        SupportsHeaders = true;
        SupportsBody = true;
    }

    #region Properties

    public abstract string Name { get; }
    public IDictionary<string, string> Headers { get; }

    public Stream Body { get; }
    public virtual bool SupportsHeaders { get; }
    public virtual bool SupportsBody { get; }

    #endregion
}
