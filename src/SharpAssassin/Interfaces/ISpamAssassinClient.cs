using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SharpAssassin.Interfaces;

public interface ISpamAssassinClient
{
    #region Methods

    Task<ISpamAssassinResult> CheckAsync(Stream body, CancellationToken cancellationToken = default);
    Task<ISpamAssassinResult> CheckAsync(Stream body, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken = default);
    Task<ISpamAssassinResult> HeadersAsync(Stream body, CancellationToken cancellationToken = default);
    Task<ISpamAssassinResult> HeadersAsync(Stream body, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken = default);
    Task<SpamAssassinResult> PingAsync(Stream body, CancellationToken cancellationToken = default);
    Task<SpamAssassinResult> PingAsync(Stream body, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken = default);
    Task<SpamAssassinResult> ProcessAsync(Stream body, CancellationToken cancellationToken = default);
    Task<SpamAssassinResult> ProcessAsync(Stream body, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken = default);
    Task<SpamAssassinResult> ReportAsync(Stream body, CancellationToken cancellationToken = default);
    Task<SpamAssassinResult> ReportAsync(Stream body, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken = default);
    Task<SpamAssassinResult> ReportIfSpamAsync(Stream body, CancellationToken cancellationToken = default);
    Task<SpamAssassinResult> ReportIfSpamAsync(Stream body, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken = default);
    Task<SpamAssassinResult> SkipAsync(Stream body, CancellationToken cancellationToken = default);
    Task<SpamAssassinResult> SkipAsync(Stream body, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken = default);
    Task<SpamAssassinResult> SymbolsAsync(Stream body, CancellationToken cancellationToken = default);
    Task<SpamAssassinResult> SymbolsAsync(Stream body, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken = default);
    Task<SpamAssassinResult> TellAsync(Stream body, CancellationToken cancellationToken = default);
    Task<SpamAssassinResult> TellAsync(Stream body, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken = default);

    #endregion

    #region Properties

    string Host { get; set; }
    int Port { get; set; }
    string? User { get; set; }
    TimeSpan Timeout { get; set; }
    public string Version { get; set; }

    #endregion

    #region Events

    event GetMemoryStreamEventHandler? GetMemoryStream;

    #endregion
}