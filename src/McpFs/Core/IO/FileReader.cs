using System.Text;

namespace McpFs.Core.IO;

public sealed class FileReader
{
    public async Task<ReadRangeResult> ReadRangeAsync(
        string filePath,
        int startLine,
        int endLine,
        int maxBytes,
        CancellationToken cancellationToken)
    {
        if (startLine <= 0 || endLine <= 0 || endLine < startLine)
        {
            return ReadRangeResult.InvalidRange("Invalid line range.");
        }

        if (!File.Exists(filePath))
        {
            return ReadRangeResult.NotFound($"File not found: {filePath}");
        }

        try
        {
            await using var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 16 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

            var lineNo = 0;
            var bytesUsed = 0;
            var builder = new StringBuilder();

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (line is null)
                {
                    break;
                }

                lineNo++;
                if (lineNo < startLine)
                {
                    continue;
                }

                if (lineNo > endLine)
                {
                    break;
                }

                var withNewline = line + "\n";
                var lineBytes = Encoding.UTF8.GetByteCount(withNewline);
                if (bytesUsed + lineBytes > maxBytes)
                {
                    return ReadRangeResult.TooLarge($"Requested content exceeds maxBytes ({maxBytes}).");
                }

                bytesUsed += lineBytes;
                builder.Append(withNewline);
            }

            var actualEndLine = Math.Min(lineNo, endLine);
            if (actualEndLine < startLine)
            {
                actualEndLine = startLine;
            }

            return ReadRangeResult.Success(builder.ToString(), actualEndLine);
        }
        catch (UnauthorizedAccessException)
        {
            return ReadRangeResult.PermissionDenied($"Permission denied: {filePath}");
        }
    }
}

public sealed class ReadRangeResult
{
    private ReadRangeResult(bool ok, string? errorCode, string? message, string? text, int endLine)
    {
        Ok = ok;
        ErrorCode = errorCode;
        Message = message;
        Text = text;
        EndLine = endLine;
    }

    public bool Ok { get; }
    public string? ErrorCode { get; }
    public string? Message { get; }
    public string? Text { get; }
    public int EndLine { get; }

    public static ReadRangeResult Success(string text, int endLine) => new(true, null, null, text, endLine);
    public static ReadRangeResult InvalidRange(string message) => new(false, Rpc.ErrorCodes.InvalidRange, message, null, 0);
    public static ReadRangeResult NotFound(string message) => new(false, Rpc.ErrorCodes.NotFound, message, null, 0);
    public static ReadRangeResult PermissionDenied(string message) => new(false, Rpc.ErrorCodes.PermissionDenied, message, null, 0);
    public static ReadRangeResult TooLarge(string message) => new(false, Rpc.ErrorCodes.TooLarge, message, null, 0);
}
