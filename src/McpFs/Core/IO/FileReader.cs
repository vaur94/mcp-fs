using System.Text;

namespace McpFs.Core.IO;

public sealed class FileReader
{
    public async Task<ReadRangeResult> ReadRangeAsync(
        string filePath,
        int startLine,
        int endLine,
        int maxBytes,
        int maxLines,
        bool enforceEndLineBounds,
        CancellationToken cancellationToken)
    {
        if (startLine <= 0 || endLine <= 0 || endLine < startLine)
        {
            return ReadRangeResult.InvalidRange("Invalid line range.");
        }

        if (maxBytes <= 0 || maxLines <= 0)
        {
            return ReadRangeResult.InvalidRange("Invalid limits.");
        }

        if (!File.Exists(filePath))
        {
            return ReadRangeResult.NotFound("File not found.");
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

            var fileSize = stream.Length;
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

            var lineNo = 0;
            var linesReturned = 0;
            var bytesUsed = 0;
            var truncated = false;
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

                if (linesReturned >= maxLines)
                {
                    truncated = true;
                    break;
                }

                var withNewline = line + "\n";
                var lineBytes = Encoding.UTF8.GetByteCount(withNewline);
                if (bytesUsed + lineBytes > maxBytes)
                {
                    if (bytesUsed == 0)
                    {
                        return ReadRangeResult.TooLarge($"Requested content exceeds maxBytes ({maxBytes}).");
                    }

                    truncated = true;
                    break;
                }

                bytesUsed += lineBytes;
                linesReturned++;
                builder.Append(withNewline);
            }

            if (startLine > lineNo)
            {
                return ReadRangeResult.InvalidRange("startLine is outside file bounds.");
            }

            if (enforceEndLineBounds && endLine > lineNo)
            {
                return ReadRangeResult.InvalidRange("endLine is outside file bounds.");
            }

            var actualEndLine = startLine + linesReturned - 1;
            if (linesReturned == 0)
            {
                actualEndLine = startLine;
            }

            return ReadRangeResult.Success(builder.ToString(), actualEndLine, fileSize, truncated);
        }
        catch (UnauthorizedAccessException)
        {
            return ReadRangeResult.PermissionDenied("Permission denied.");
        }
    }
}

public sealed class ReadRangeResult
{
    private ReadRangeResult(bool ok, string? errorCode, string? message, string? text, int endLine, long fileSize, bool truncated)
    {
        Ok = ok;
        ErrorCode = errorCode;
        Message = message;
        Text = text;
        EndLine = endLine;
        FileSize = fileSize;
        Truncated = truncated;
    }

    public bool Ok { get; }
    public string? ErrorCode { get; }
    public string? Message { get; }
    public string? Text { get; }
    public int EndLine { get; }
    public long FileSize { get; }
    public bool Truncated { get; }

    public static ReadRangeResult Success(string text, int endLine, long fileSize, bool truncated) =>
        new(true, null, null, text, endLine, fileSize, truncated);

    public static ReadRangeResult InvalidRange(string message) =>
        new(false, Rpc.ErrorCodes.InvalidRange, message, null, 0, 0, false);

    public static ReadRangeResult NotFound(string message) =>
        new(false, Rpc.ErrorCodes.NotFound, message, null, 0, 0, false);

    public static ReadRangeResult PermissionDenied(string message) =>
        new(false, Rpc.ErrorCodes.PermissionDenied, message, null, 0, 0, false);

    public static ReadRangeResult TooLarge(string message) =>
        new(false, Rpc.ErrorCodes.TooLarge, message, null, 0, 0, false);
}
