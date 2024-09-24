using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using SharpAssassin.Commands;

namespace SharpAssassin;

public interface ISpamAssassinResult : IDisposable, IAsyncDisposable
{
    #region Methods

    Task<long> CopyBodyToAsync(Stream destination, CancellationToken cancellationToken = default);
    Task<long> CopyBodyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken = default);

    #endregion

    #region Properties

    CommandBase Command { get; }
    bool IsSpam { get; }
    double Threshold { get; }
    double Score { get; }
    IReadOnlyDictionary<string, string> Headers { get; }
    Stream Response { get; }

    #endregion
}
