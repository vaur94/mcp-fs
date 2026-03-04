using McpFs.Core;
using McpFs.Core.Limits;
using McpFs.Rpc;

namespace McpFs.Tools;

public sealed class ReadDirTool
{
    private readonly Workspace _workspace;

    public ReadDirTool(Workspace workspace)
    {
        _workspace = workspace;
    }

    public Task<ToolResponse> ExecuteAsync(ReadDirRequest request, CancellationToken cancellationToken)
    {
        if (request.Limit is <= 0)
        {
            return Task.FromResult(ToolResponse.Failure(ErrorCodes.InvalidRange, "limit must be > 0"));
        }

        var includeFiles = request.IncludeFiles ?? true;
        var includeDirs = request.IncludeDirs ?? true;
        if (!includeFiles && !includeDirs)
        {
            return Task.FromResult(ToolResponse.Failure(ErrorCodes.InvalidRange, "includeFiles/includeDirs cannot both be false"));
        }

        var limit = Math.Min(
            request.Limit ?? _workspace.Config.ScanLimit,
            Math.Min(_workspace.Config.ScanLimit, FsLimits.ScanHardCapLimit));

        if (!_workspace.PathPolicy.TryResolveDirectory(request.Path, out var fullPath, out _, out var pathError))
        {
            return Task.FromResult(pathError!);
        }

        var entries = new List<ReadDirEntry>(Math.Min(limit, 256));
        var truncated = false;

        IEnumerable<string> children;
        try
        {
            children = Directory.EnumerateFileSystemEntries(fullPath);
        }
        catch (UnauthorizedAccessException)
        {
            return Task.FromResult(ToolResponse.Failure(ErrorCodes.PermissionDenied, "Permission denied."));
        }

        foreach (var child in children)
        {
            cancellationToken.ThrowIfCancellationRequested();

            FileSystemInfo info = Directory.Exists(child)
                ? new DirectoryInfo(child)
                : new FileInfo(child);
            var isDirectory = info is DirectoryInfo;

            if (isDirectory && !includeDirs)
            {
                continue;
            }

            if (!isDirectory && !includeFiles)
            {
                continue;
            }

            var isSymlink = info.LinkTarget is not null || info.Attributes.HasFlag(FileAttributes.ReparsePoint);
            if (_workspace.ShouldSkipSymlink(info))
            {
                continue;
            }

            var relativePath = _workspace.PathPolicy.ToRelativePath(child);
            entries.Add(new ReadDirEntry
            {
                Name = info.Name,
                Path = relativePath,
                Kind = isDirectory ? "dir" : "file",
                Size = isDirectory ? null : ((FileInfo)info).Length,
                MtimeUtc = info.LastWriteTimeUtc,
                IsSymlink = isSymlink
            });

            if (entries.Count >= limit)
            {
                truncated = true;
                break;
            }
        }

        var data = new ReadDirData
        {
            Entries = entries,
            Truncated = truncated
        };

        return Task.FromResult(ToolResponse.Success(data, McpJsonSerializerContext.Default.ReadDirData));
    }
}
