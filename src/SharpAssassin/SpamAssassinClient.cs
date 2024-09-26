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
using SharpAssassin.Commands;

namespace SharpAssassin;

public delegate void GetMemoryStreamEventHandler(object sender, MemoryStreamEventArgs e);

public sealed class SpamAssassinClient : ISpamAssassinClient
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
        var result = await SendAsync(new CheckCommand(body), cancellationToken);

        return result;
    }

    public async Task<ISpamAssassinResult> HeadersAsync(Stream body, CancellationToken cancellationToken)
    {
        var result = await SendAsync(new HeadersCommand(body), cancellationToken);

        return result;
    }

    public async Task<ISpamAssassinResult> PingAsync(CancellationToken cancellationToken)
    {
        var result = await SendAsync(new PingCommand(), cancellationToken);

        return result;
    }

    public async Task<ISpamAssassinResult> ProcessAsync(Stream body, CancellationToken cancellationToken)
    {
        var result = await SendAsync(new ProcessCommand(body), cancellationToken);

        return result;
    }

    public async Task<ISpamAssassinResult> ReportAsync(Stream body, CancellationToken cancellationToken)
    {
        var result = await SendAsync(new ReportCommand(body), cancellationToken);

        return result;
    }

    public async Task<ISpamAssassinResult> ReportIfSpamAsync(Stream body, CancellationToken cancellationToken)
    {
        var result = await SendAsync(new ReportIfSpamCommand(body), cancellationToken);

        return result;
    }

    public async Task<ISpamAssassinResult> SkipAsync(CancellationToken cancellationToken)
    {
        var result = await SendAsync(new SkipCommand(), cancellationToken);

        return result;
    }

    public async Task<ISpamAssassinResult> SymbolsAsync(Stream body, CancellationToken cancellationToken)
    {
        var result = await SendAsync(new SymbolsCommand(body), cancellationToken);

        return result;
    }

    public async Task<ISpamAssassinResult> TellAsync(Stream body, CancellationToken cancellationToken)
    {
        var result = await SendAsync(new TellCommand(body), cancellationToken);

        return result;
    }

    public async Task<ISpamAssassinResult> SendAsync(CommandBase command, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        cts.CancelAfter(Timeout);

        using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

        await socket.ConnectAsync(Host, Port, cancellationToken);

        // Send request
        var headersBuffer = new StringBuilder();

        headersBuffer.AppendLine($"{command.Name.ToUpper()} SPAMC/{Version}");

        if (command.SupportsHeaders)
        {
            if (!string.IsNullOrWhiteSpace(User))
            {
                headersBuffer.AppendLine($"User: {User}");
            }

            foreach (var header in command.Headers)
            {
                headersBuffer.AppendLine($"{header.Key}: {header.Value}");
            }
        }

        headersBuffer.AppendLine();

        var buffer = Encoding.Latin1.GetBytes(headersBuffer.ToString());

        await socket.SendAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), SocketFlags.None, cancellationToken);

        if (command.SupportsBody)
        {
            var outBuffer = ArrayPool<byte>.Shared.Rent(4096);

            try
            {
                var numRead = await command.Body.ReadAsync(outBuffer, 0, outBuffer.Length, cancellationToken);

                while (numRead > 0)
                {
                    await socket.SendAsync(new ArraySegment<byte>(outBuffer, 0, numRead), SocketFlags.None, cancellationToken);

                    numRead = await command.Body.ReadAsync(outBuffer, 0, outBuffer.Length, cancellationToken);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(outBuffer);
            }
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
