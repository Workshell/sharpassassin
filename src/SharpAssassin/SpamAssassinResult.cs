using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using SharpAssassin.Commands;

namespace SharpAssassin;

public sealed class SpamAssassinResult : ISpamAssassinResult
{
    private const string SAVersionHeader = "__sa_version";
    private const string SAStatusCodeHeader = "__sa_status_code";
    private const string SAStatusStringHeader = "__sa_status_string";

    public static readonly IReadOnlyDictionary<string, SpamAssassinStatus> StatusStrings = new Dictionary<string, SpamAssassinStatus>()
    {
        { "EX_OK", SpamAssassinStatus.OK },
        { "EX_USAGE", SpamAssassinStatus.Usage },
        { "EX_DATAERR", SpamAssassinStatus.DataError },
        { "EX_NOINPUT", SpamAssassinStatus.NoInput },
        { "EX_NOUSER", SpamAssassinStatus.NoUser },
        { "EX_NOHOST", SpamAssassinStatus.NoHost },
        { "EX_UNAVAILABLE", SpamAssassinStatus.Unavailable },
        { "EX_SOFTWARE", SpamAssassinStatus.Software },
        { "EX_OSERR", SpamAssassinStatus.OSError },
        { "EX_OSFILE", SpamAssassinStatus.OSFile },
        { "EX_CANTCREAT", SpamAssassinStatus.CantCreate },
        { "EX_IOERR", SpamAssassinStatus.IOError },
        { "EX_TEMPFAIL", SpamAssassinStatus.TemporaryFail },
        { "EX_PROTOCOL", SpamAssassinStatus.Protocol },
        { "EX_NOPERM", SpamAssassinStatus.NoPermission },
        { "EX_CONFIG", SpamAssassinStatus.Config },
        { "EX_TIMEOUT", SpamAssassinStatus.Timeout },
    };
    public static readonly IReadOnlyDictionary<SpamAssassinStatus, string> StatusDescriptions = new Dictionary<SpamAssassinStatus, string>()
    {
        { SpamAssassinStatus.OK, "No problems were found." },
        { SpamAssassinStatus.Usage, "Command line usage error." },
        { SpamAssassinStatus.DataError, "Data format error." },
        { SpamAssassinStatus.NoInput, "Cannot open input." },
        { SpamAssassinStatus.NoUser, "Addressee unknown." },
        { SpamAssassinStatus.NoHost, "Hostname unknown." },
        { SpamAssassinStatus.Unavailable, "Service unavailable." },
        { SpamAssassinStatus.Software, "Internal software error." },
        { SpamAssassinStatus.OSError, " System error (e.g. can’t fork the process)." },
        { SpamAssassinStatus.OSFile, "Critical operating system file missing." },
        { SpamAssassinStatus.CantCreate, "Can’t create (user) output file." },
        { SpamAssassinStatus.IOError, "Input/output error." },
        { SpamAssassinStatus.TemporaryFail, "Temporary failure, user is invited to retry." },
        { SpamAssassinStatus.Protocol, "Remote error in protocol." },
        { SpamAssassinStatus.NoPermission, "Permission denied." },
        { SpamAssassinStatus.Config, "Configuration error." },
        { SpamAssassinStatus.Timeout, "Read timeout." },
    };
    private static readonly Regex ResponseRegex = new Regex(@"^SPAMD/(.*?)\s(\d+)\s(.*?)$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
    private static readonly Regex SpamRegex = new Regex(@"^(.+?)\s;\s(-?\d+(?:\.\d+)?)\s*\/\s*(-?\d+(?:\.\d+)?)$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

    private readonly long _bodyPosition;
    private volatile bool _disposed;

    public SpamAssassinResult(CommandBase command, MemoryStream response, long bodyPosition, SpamAssassinStatus status, IReadOnlyDictionary<string, string> headers)
    {
        Command = command;
        Response = response;
        Status = status;
        Headers = headers;
        IsSpam = false;
        Threshold = 0.0;
        Score = 0.0;

        if (headers.TryGetValue("Spam", out var spamHeader))
        {
            var match = SpamRegex.Match(spamHeader);

            if (match.Success)
            {
                IsSpam = TryParseBoolDefault(match.Groups[1].Value, false);
                Threshold = TryParseDoubleDefault(match.Groups[3].Value, 0.0);
                Score = TryParseDoubleDefault(match.Groups[2].Value, 0.0);
            }
        }

        _bodyPosition = bodyPosition;
        _disposed = false;
    }

    #region Static Methods

    internal static async Task<SpamAssassinResult> ParseAsync(SpamAssassinClient client, CommandBase command, MemoryStream response, CancellationToken cancellationToken)
    {
        var inHeader = true;
        var lineBreak = new List<byte>(4);
        var headerBytes = new List<byte>(1024);
        var bodyPosition = -1L;

        while (response.Position < response.Length)
        {
            var b = (byte)response.ReadByte();

            if (inHeader)
            {
                headerBytes.Add(b);

                if (b == 13 || b == 10) // CR or LF
                {
                    lineBreak.Add(b);
                }
                else
                {
                    lineBreak.Clear();
                }

                if (lineBreak.Count == 4)
                {
                    if (lineBreak[0] == '\r' && lineBreak[1] == '\n' && lineBreak[2] == '\r' && lineBreak[3] == '\n')
                    {
                        inHeader = false;
                    }
                    else
                    {
                        lineBreak.Clear();
                    }
                }
            }
            else
            {
                bodyPosition = response.Position - 1;

                break;
            }
        }

        response.Seek(0, SeekOrigin.Begin);

        // Parse headers from response
        var headers = await ParseHeadersAsync(headerBytes.ToArray(), cancellationToken);
        var status = SpamAssassinStatus.OK;

        if (command.Name.Equals("PING", StringComparison.OrdinalIgnoreCase))
        {
            if (!SAStatusStringHeader.Equals("PONG", StringComparison.OrdinalIgnoreCase))
            {
                throw new SpamAssassinException("Ping received unexpected response.");
            }
        }
        else
        {
            if (!headers.TryGetValue(SAStatusStringHeader, out var statusCodeString))
            {
                throw new SpamAssassinException("Could not parse status code from response.");
            }

            if (!StatusStrings.TryGetValue(statusCodeString, out status))
            {
                throw new SpamAssassinException($"Unknown status code - {statusCodeString}");
            }
        }

        headers.Remove(SAVersionHeader);
        headers.Remove(SAStatusCodeHeader);
        headers.Remove(SAStatusStringHeader);

        // Return new response
        return new SpamAssassinResult(command, response, bodyPosition, status, headers);
    }

    private static async Task<Dictionary<string, string>> ParseHeadersAsync(byte[] headers, CancellationToken cancellationToken)
    {
        var results = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        using var reader = new StreamReader(new MemoryStream(headers), Encoding.Latin1, false);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();

            if (string.IsNullOrWhiteSpace(line))
            {
                break;
            }

            if (line.StartsWith("SPAMD/", StringComparison.OrdinalIgnoreCase))
            {
                var match = ResponseRegex.Match(line);

                if (match.Success)
                {
                    var version = match.Groups[1].Value;
                    var statusCode = match.Groups[2].Value;
                    var statusString = match.Groups[3].Value;

                    results.Add(SAVersionHeader, version);
                    results.Add(SAStatusCodeHeader, statusCode);
                    results.Add(SAStatusStringHeader, statusString);
                }
                else
                {
                    throw new SpamAssassinException("Could not parse response.");
                }
            }
            else
            {
                var idx = line.IndexOf(':');

                if (idx == -1)
                {
                    throw new SpamAssassinException("Could not parse header value.");
                }

                var key = line.Substring(0, idx);
                var value = line.Substring(idx + 1);

                results.Add(key, value);
            }
        }

        return results;
    }

    private static bool TryParseBoolDefault(string value, bool defaultValue)
    {
        if (!bool.TryParse(value, out var result))
        {
            return defaultValue;
        }

        return result;
    }

    private static double TryParseDoubleDefault(string value, double defaultValue)
    {
        if (!double.TryParse(value, out var result))
        {
            return defaultValue;
        }

        return result;
    }

    #endregion

    #region Methods

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Response.Dispose();

        _disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await Response.DisposeAsync();

        _disposed = true;
    }

    public async Task<long> CopyBodyToAsync(Stream destination, CancellationToken cancellationToken)
    {
        return await CopyBodyToAsync(destination, 4096, cancellationToken);
    }

    public async Task<long> CopyBodyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
    {
        if (_bodyPosition == -1L)
        {
            return 0;
        }

        var result = 0L;
        var position = Response.Position;

        try
        {
            Response.Seek(_bodyPosition, SeekOrigin.Begin);

            var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);

            try
            {
                var numRead = await destination.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                while (numRead > 0)
                {
                    await destination.WriteAsync(buffer, 0, numRead, cancellationToken);

                    result += numRead;
                    numRead = await destination.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        finally
        {
            Response.Seek(position, SeekOrigin.Begin);
        }

        return result;
    }

    #endregion

    #region Properties

    public CommandBase Command { get; }

    public SpamAssassinStatus Status { get; }

    public string StatusDescription
    {
        get
        {
            if (StatusDescriptions.TryGetValue(Status, out var result))
            {
                return result;
            }

            return string.Empty;
        }
    }

    public bool IsSpam { get; }
    public double Threshold { get; }
    public double Score { get; }
    public IReadOnlyDictionary<string, string> Headers { get; }
    public Stream Response { get; }

    #endregion
}
