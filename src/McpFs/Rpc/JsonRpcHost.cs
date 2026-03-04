using System.Text;
using System.Text.Json;
using McpFs.Logging;

namespace McpFs.Rpc;

public sealed class JsonRpcHost
{
    private readonly Router _router;
    private readonly StderrLogger _logger;

    public JsonRpcHost(Router router, StderrLogger logger)
    {
        _router = router;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await using var input = Console.OpenStandardInput();
        await using var output = Console.OpenStandardOutput();

        while (!cancellationToken.IsCancellationRequested)
        {
            JsonRpcRequest? request;
            try
            {
                request = await ReadRequestAsync(input, cancellationToken).ConfigureAwait(false);
                if (request is null)
                {
                    return;
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.Error($"read failure: {ex.Message}");
                continue;
            }

            JsonRpcResponse? response;
            try
            {
                response = await _router.RouteAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Error($"routing failure method={request.Method}: {ex}");
                response = new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError
                    {
                        Code = -32603,
                        Message = "Internal error"
                    }
                };
            }

            if (response is null)
            {
                continue;
            }

            await WriteResponseAsync(output, response, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<JsonRpcRequest?> ReadRequestAsync(Stream input, CancellationToken cancellationToken)
    {
        var contentLength = -1;

        while (true)
        {
            var line = await ReadAsciiLineAsync(input, cancellationToken).ConfigureAwait(false);
            if (line is null)
            {
                return null;
            }

            if (line.Length == 0)
            {
                break;
            }

            if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
            {
                var value = line["Content-Length:".Length..].Trim();
                if (!int.TryParse(value, out contentLength) || contentLength < 0)
                {
                    throw new InvalidOperationException("Invalid Content-Length header");
                }
            }
        }

        if (contentLength < 0)
        {
            throw new InvalidOperationException("Missing Content-Length header");
        }

        var payload = new byte[contentLength];
        var read = 0;
        while (read < contentLength)
        {
            var chunk = await input.ReadAsync(payload.AsMemory(read, contentLength - read), cancellationToken).ConfigureAwait(false);
            if (chunk == 0)
            {
                throw new EndOfStreamException("Unexpected EOF while reading payload");
            }

            read += chunk;
        }

        var request = JsonSerializer.Deserialize(payload, McpJsonSerializerContext.Default.JsonRpcRequest);
        if (request is null)
        {
            throw new InvalidOperationException("JSON-RPC request deserialized as null");
        }

        return request;
    }

    private static async Task<string?> ReadAsciiLineAsync(Stream input, CancellationToken cancellationToken)
    {
        var buffer = new List<byte>(64);
        var single = new byte[1];

        while (true)
        {
            var bytesRead = await input.ReadAsync(single.AsMemory(0, 1), cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                if (buffer.Count == 0)
                {
                    return null;
                }

                break;
            }

            if (single[0] == (byte)'\n')
            {
                break;
            }

            buffer.Add(single[0]);
        }

        if (buffer.Count > 0 && buffer[^1] == (byte)'\r')
        {
            buffer.RemoveAt(buffer.Count - 1);
        }

        return Encoding.ASCII.GetString(buffer.ToArray());
    }

    private static async Task WriteResponseAsync(Stream output, JsonRpcResponse response, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(response, McpJsonSerializerContext.Default.JsonRpcResponse);
        var header = Encoding.ASCII.GetBytes($"Content-Length: {payload.Length}\r\n\r\n");

        await output.WriteAsync(header.AsMemory(0, header.Length), cancellationToken).ConfigureAwait(false);
        await output.WriteAsync(payload.AsMemory(0, payload.Length), cancellationToken).ConfigureAwait(false);
        await output.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
