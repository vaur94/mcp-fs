using McpFs.Core;
using McpFs.Core.Hashing;
using McpFs.Core.IO;
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

        if (!_workspace.PathPolicy.TryResolvePath(request.Path, out var fullPath, out var relativePath, out var pathError))
        {
            return pathError!;
        }

        if (!File.Exists(fullPath))
        {
            return ToolResponse.Failure(ErrorCodes.NotFound, $"File not found: {request.Path}");
        }

        var maxBytes = request.MaxBytes ?? _workspace.Config.OpenMaxBytes;
        if (maxBytes <= 0)
        {
            return ToolResponse.Failure(ErrorCodes.InvalidRange, "maxBytes must be > 0");
        }

        var startLine = request.StartLine ?? 1;
        var endLine = request.EndLine ?? (startLine + _workspace.Config.OpenMaxLines - 1);
        if (startLine <= 0 || endLine <= 0 || endLine < startLine)
        {
            return ToolResponse.Failure(ErrorCodes.InvalidRange, "Invalid line range.");
        }

        var readResult = await _fileReader.ReadRangeAsync(fullPath, startLine, endLine, maxBytes, cancellationToken).ConfigureAwait(false);
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
            ContextHash = contextHash
        };

        return ToolResponse.Success(data, McpJsonSerializerContext.Default.OpenData);
    }
}
