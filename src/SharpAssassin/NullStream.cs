using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpAssassin;

internal sealed class NullStream : Stream
{
    public NullStream()
    {
    }

    #region Methods

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return 0;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return 0;
    }

    public override void SetLength(long value)
    {
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
    }

    #endregion

    #region Properties

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => true;
    public override long Length => 0;

    public override long Position
    {
        get => 0;
        set
        {
            // Do nothing...
        }
    }

    #endregion
}
