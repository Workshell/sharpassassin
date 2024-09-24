using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpAssassin;

public sealed class SpamAssassinException : Exception
{
    public SpamAssassinException() 
        : base()
    {
    }

    public SpamAssassinException(string message) 
        : base(message)
    {
    }

    public SpamAssassinException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
