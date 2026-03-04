using McpFs.Core;
using McpFs.Core.Hashing;
using McpFs.Rpc;

namespace McpFs.Tools;

public sealed class StatTool
{
    private readonly Workspace _workspace;
    private readonly ContentHasher _hasher;

    public StatTool(Workspace workspace, ContentHasher hasher)
    {
        _workspace = workspace;
        _hasher = hasher;
    }

    public async Task<ToolResponse> ExecuteAsync(StatRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Path))
        {
            return ToolResponse.Failure(ErrorCodes.InvalidPath, "path is required");
        }

        if (!_workspace.PathPolicy.TryResolveExistingPath(request.Path, out var fullPath, out var relativePath, out var isSymlink, out var pathError))
        {
            return pathError!;
        }

        if (Directory.Exists(fullPath))
        {
            var dir = new DirectoryInfo(fullPath);
            var dirData = new StatData
            {
                Path = relativePath,
                Kind = "dir",
                MtimeUtc = dir.LastWriteTimeUtc,
                IsSymlink = isSymlink
            };

            return ToolResponse.Success(dirData, McpJsonSerializerContext.Default.StatData);
        }

        try
        {
            var file = new FileInfo(fullPath);
            var contextHash = await _hasher.ComputeContextHashAsync(fullPath, cancellationToken).ConfigureAwait(false);
            var data = new StatData
            {
                Path = relativePath,
                Kind = "file",
                Size = file.Length,
                MtimeUtc = file.LastWriteTimeUtc,
                ContextHash = contextHash,
                QuickHash8 = _hasher.QuickHash8(contextHash),
                IsSymlink = isSymlink
            };

            return ToolResponse.Success(data, McpJsonSerializerContext.Default.StatData);
        }
        catch (UnauthorizedAccessException)
        {
            return ToolResponse.Failure(ErrorCodes.PermissionDenied, "Permission denied.");
        }
    }
}
