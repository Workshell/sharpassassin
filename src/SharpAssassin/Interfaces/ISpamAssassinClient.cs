using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using SharpAssassin.Commands;

namespace SharpAssassin;

public interface ISpamAssassinClient
{
    #region Methods

    Task<ISpamAssassinResult> CheckAsync(Stream body, CancellationToken cancellationToken = default);
    Task<ISpamAssassinResult> HeadersAsync(Stream body, CancellationToken cancellationToken = default);
    Task<ISpamAssassinResult> PingAsync(CancellationToken cancellationToken = default);
    Task<ISpamAssassinResult> ProcessAsync(Stream body, CancellationToken cancellationToken = default);
    Task<ISpamAssassinResult> ReportAsync(Stream body, CancellationToken cancellationToken = default);
    Task<ISpamAssassinResult> ReportIfSpamAsync(Stream body, CancellationToken cancellationToken = default);
    Task<ISpamAssassinResult> SkipAsync(CancellationToken cancellationToken = default);
    Task<ISpamAssassinResult> SymbolsAsync(Stream body, CancellationToken cancellationToken = default);
    Task<ISpamAssassinResult> TellAsync(Stream body, CancellationToken cancellationToken = default);
    Task<ISpamAssassinResult> SendAsync(CommandBase command, CancellationToken cancellationToken = default);

    #endregion

    #region Properties

    string Host { get; set; }
    int Port { get; set; }
    string? User { get; set; }
    TimeSpan Timeout { get; set; }
    string Version { get; set; }

    #endregion

    #region Events

    event GetMemoryStreamEventHandler? GetMemoryStream;

    #endregion
}
