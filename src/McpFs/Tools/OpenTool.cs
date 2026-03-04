using McpFs.Core;
using McpFs.Core.Hashing;
using McpFs.Core.IO;
using McpFs.Core.Limits;
using McpFs.Rpc;

namespace McpFs.Tools;

public sealed class OpenTool
{
    private readonly Workspace _workspace;
    private readonly FileReader _fileReader;
    private readonly ContentHasher _hasher;

    public OpenTool(Workspace workspace, FileReader fileReader, ContentHasher hasher)
    {
        _workspace = workspace;
        _fileReader = fileReader;
        _hasher = hasher;
    }

    public async Task<ToolResponse> ExecuteAsync(OpenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Path))
        {
            return ToolResponse.Failure(ErrorCodes.InvalidPath, "path is required");
        }

        if (!_workspace.PathPolicy.TryResolveExistingFile(request.Path, out var fullPath, out var relativePath, out _, out var pathError))
        {
            return pathError!;
        }

        if (request.MaxBytes is <= 0)
        {
            return ToolResponse.Failure(ErrorCodes.InvalidRange, "maxBytes must be > 0");
        }

        var maxBytes = Math.Min(
            request.MaxBytes ?? _workspace.Config.OpenMaxBytes,
            Math.Min(_workspace.Config.OpenMaxBytes, FsLimits.OpenHardCapBytes));

        var startLine = request.StartLine ?? 1;
        var requestedEnd = request.EndLine ?? (startLine + _workspace.Config.OpenMaxLines - 1);
        if (startLine <= 0 || requestedEnd <= 0 || requestedEnd < startLine)
        {
            return ToolResponse.Failure(ErrorCodes.InvalidRange, "Invalid line range.");
        }

        var requestedLines = requestedEnd - startLine + 1;
        var effectiveMaxLines = Math.Min(requestedLines, Math.Min(_workspace.Config.OpenMaxLines, FsLimits.OpenHardCapLines));
        var effectiveEnd = startLine + effectiveMaxLines - 1;
        if (effectiveEnd < startLine)
        {
            return ToolResponse.Failure(ErrorCodes.InvalidRange, "Invalid line range.");
        }

        var readResult = await _fileReader.ReadRangeAsync(
            fullPath,
            startLine,
            effectiveEnd,
            maxBytes,
            effectiveMaxLines,
            request.EndLine.HasValue,
            cancellationToken).ConfigureAwait(false);
        if (!readResult.Ok)
        {
            return ToolResponse.Failure(readResult.ErrorCode ?? ErrorCodes.InternalError, readResult.Message ?? "Read failed");
        }

        var contextHash = await _hasher.ComputeContextHashAsync(fullPath, cancellationToken).ConfigureAwait(false);
        var data = new OpenData
        {
            Path = relativePath,
            StartLine = startLine,
            EndLine = readResult.EndLine,
            Text = readResult.Text ?? string.Empty,
            ContextHash = contextHash,
            FileSize = readResult.FileSize,
            Truncated = readResult.Truncated || requestedEnd > effectiveEnd
        };

        return ToolResponse.Success(data, McpJsonSerializerContext.Default.OpenData);
    }
}
