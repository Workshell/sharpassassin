# SharpAssassin

This is a class library for interacting with SpamAssassin and performing commands against it.

## Installation

Stable builds are available as NuGet packages. You can install it via the Package Manager or via the Package Manager Console:

```
Install-Package SharpAssassin
```

## Client Setup

```
var client = new SpamAssassinClient("127.0.0.1", 783);
```

You can set other properties like `Timeout` and `User`.

## Supported Commands

* CHECK
* HEADERS
* PING
* PROCESS
* REPORT
* REPORT_IFSPAM
* SKIP
* SYMBOLS
* TELL

## ISpamAssassinClient

```
public interface ISpamAssassinClient
{
    #region Methods

    Task<ISpamAssassinResult> CheckAsync(Stream body, CancellationToken cancellationToken = default);
    Task<ISpamAssassinResult> HeadersAsync(Stream body, CancellationToken cancellationToken = default);
    Task<ISpamAssassinResult> PingAsync(Stream body, CancellationToken cancellationToken = default);
    Task<ISpamAssassinResult> ProcessAsync(Stream body, CancellationToken cancellationToken = default);
    Task<ISpamAssassinResult> ReportAsync(Stream body, CancellationToken cancellationToken = default);
    Task<ISpamAssassinResult> ReportIfSpamAsync(Stream body, CancellationToken cancellationToken = default);
    Task<ISpamAssassinResult> SkipAsync(Stream body, CancellationToken cancellationToken = default);
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
```

## ISpamAssassinResult

```
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
```

## MIT License

Copyright (c) Workshell Ltd

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.