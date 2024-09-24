using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SharpAssassin;

public delegate void GetMemoryStreamEventHandler(object sender, MemoryStreamEventArgs e);

public sealed class SpamAssassinClient
{
    private static readonly IReadOnlyDictionary<string, string> EmptyDict = new Dictionary<string, string>(0);

    public SpamAssassinClient(string host, int port)
    {
        Host = host;
        Port = port;
        Timeout = TimeSpan.FromMinutes(5);
        Version = "1.5";
    }

    #region Methods

    public async Task<ISpamAssassinResult> CheckAsync(Stream body, CancellationToken cancellationToken)
    {
        return await CheckAsync(body, EmptyDict, cancellationToken);
    }

    public async Task<ISpamAssassinResult> CheckAsync(Stream body, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
    {
        var result = await SendAsync("CHECK", headers, body, cancellationToken);

        return result;
    }

    public async Task<ISpamAssassinResult> HeadersAsync(Stream body, CancellationToken cancellationToken)
    {
        return await HeadersAsync(body, EmptyDict, cancellationToken);
    }

    public async Task<ISpamAssassinResult> HeadersAsync(Stream body, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
    {
        var result = await SendAsync("HEADERS", headers, body, cancellationToken);

        return result;
    }

    public async Task<SpamAssassinResult> PingAsync(Stream body, CancellationToken cancellationToken)
    {
        return await PingAsync(body, EmptyDict, cancellationToken);
    }

    public async Task<SpamAssassinResult> PingAsync(Stream body, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
    {
        var result = await SendAsync("PING", headers, body, cancellationToken);

        return result;
    }

    public async Task<SpamAssassinResult> ProcessAsync(Stream body, CancellationToken cancellationToken)
    {
        return await ProcessAsync(body, EmptyDict, cancellationToken);
    }

    public async Task<SpamAssassinResult> ProcessAsync(Stream body, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
    {
        var result = await SendAsync("PROCESS", headers ?? EmptyDict, body, cancellationToken);

        return result;
    }

    public async Task<SpamAssassinResult> ReportAsync(Stream body, CancellationToken cancellationToken)
    {
        return await ReportAsync(body, EmptyDict, cancellationToken);
    }

    public async Task<SpamAssassinResult> ReportAsync(Stream body, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
    {
        var result = await SendAsync("REPORT", headers ?? EmptyDict, body, cancellationToken);

        return result;
    }

    public async Task<SpamAssassinResult> ReportIfSpamAsync(Stream body, CancellationToken cancellationToken)
    {
        return await ReportIfSpamAsync(body, EmptyDict, cancellationToken);
    }

    public async Task<SpamAssassinResult> ReportIfSpamAsync(Stream body, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
    {
        var result = await SendAsync("REPORT_IFSPAM", headers ?? EmptyDict, body, cancellationToken);

        return result;
    }

    public async Task<SpamAssassinResult> SkipAsync(Stream body, CancellationToken cancellationToken)
    {
        return await SkipAsync(body, EmptyDict, cancellationToken);
    }

    public async Task<SpamAssassinResult> SkipAsync(Stream body, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
    {
        var result = await SendAsync("SKIP", headers ?? EmptyDict, body, cancellationToken);

        return result;
    }

    public async Task<SpamAssassinResult> SymbolsAsync(Stream body, CancellationToken cancellationToken)
    {
        return await SymbolsAsync(body, EmptyDict, cancellationToken);
    }

    public async Task<SpamAssassinResult> SymbolsAsync(Stream body, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
    {
        var result = await SendAsync("SYMBOLS", headers ?? EmptyDict, body, cancellationToken);

        return result;
    }

    public async Task<SpamAssassinResult> TellAsync(Stream body, CancellationToken cancellationToken)
    {
        return await TellAsync(body, EmptyDict, cancellationToken);
    }

    public Task<SpamAssassinResult> TellAsync(Stream body, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    internal async Task<SpamAssassinResult> SendAsync(string command, IReadOnlyDictionary<string, string> headers, Stream body, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        cts.CancelAfter(Timeout);

        using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

        await socket.ConnectAsync(Host, Port, cancellationToken);

        // Send request
        var headersBuffer = new StringBuilder();

        headersBuffer.AppendLine($"{command.ToUpper()} SPAMC/{Version}");

        if (!string.IsNullOrWhiteSpace(User))
        {
            headersBuffer.AppendLine($"User: {User}");
        }

        foreach (var header in headers)
        {
            headersBuffer.AppendLine($"{header.Key}: {header.Value}");
        }

        headersBuffer.AppendLine();

        var buffer = Encoding.Latin1.GetBytes(headersBuffer.ToString());

        await socket.SendAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), SocketFlags.None, cancellationToken);

        var outBuffer = ArrayPool<byte>.Shared.Rent(4096);

        try
        {
            var numRead = await body.ReadAsync(outBuffer, 0, outBuffer.Length, cancellationToken);

            while (numRead > 0)
            {
                await socket.SendAsync(new ArraySegment<byte>(outBuffer, 0, numRead), SocketFlags.None, cancellationToken);

                numRead = await body.ReadAsync(outBuffer, 0, outBuffer.Length, cancellationToken);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(outBuffer);
        }

        socket.Shutdown(SocketShutdown.Send);

        // Receive response
        var mem = CreateMemoryStream();

        try
        {
            var inBuffer = ArrayPool<byte>.Shared.Rent(4096);

            try
            {
                var numRead = await socket.ReceiveAsync(inBuffer, SocketFlags.None, cancellationToken);

                while (numRead > 0)
                {
                    await mem.WriteAsync(inBuffer, 0, numRead, cancellationToken);

                    numRead = await socket.ReceiveAsync(inBuffer, SocketFlags.None, cancellationToken);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(inBuffer);
            }
        }
        catch
        {
            await mem.DisposeAsync();

            throw;
        }

        mem.Seek(0, SeekOrigin.Begin);

        var response = await SpamAssassinResult.ParseAsync(this, command, mem, cancellationToken);

        return response;
    }

    internal MemoryStream CreateMemoryStream()
    {
        return CreateMemoryStream(this);
    }

    internal MemoryStream CreateMemoryStream(object sender)
    {
        var args = new MemoryStreamEventArgs();

        if (GetMemoryStream is not null)
        {
            GetMemoryStream.Invoke(sender, args);
        }

        return args.Stream ?? new MemoryStream();
    }

    #endregion

    #region Properties

    public string Host { get; set; }
    public int Port { get; set; }
    public string? User { get; set; }
    public TimeSpan Timeout { get; set; }
    public string Version { get; set; }

    #endregion

    #region Events

    public event GetMemoryStreamEventHandler? GetMemoryStream;

    #endregion
}
