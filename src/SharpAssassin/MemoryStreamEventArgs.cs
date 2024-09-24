using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpAssassin;

public sealed class MemoryStreamEventArgs : EventArgs
{
    internal MemoryStreamEventArgs()
    {
        Length = null;
        Buffer = null;
    }

    internal MemoryStreamEventArgs(int length)
    {
        Length = length;
        Buffer = null;
    }

    internal MemoryStreamEventArgs(byte[] buffer)
    {
        Length = null;
        Buffer = buffer;
    }

    #region Properties

    public MemoryStream? Stream { get; set; }
    public int? Length { get; }
    public byte[]? Buffer { get; }

    #endregion
}