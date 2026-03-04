using McpFs.Core;
using McpFs.Core.Hashing;
using McpFs.Core.Ignore;
using McpFs.Logging;
using McpFs.Rpc;

namespace McpFs.Tools;

public sealed class ScanTool
{
    private readonly Workspace _workspace;
    private readonly ContentHasher _hasher;
    private readonly StderrLogger _logger;

    public ScanTool(Workspace workspace, ContentHasher hasher, StderrLogger logger)
    {
        _workspace = workspace;
        _hasher = hasher;
        _logger = logger;
    }

    public async Task<ToolResponse> ExecuteAsync(ScanRequest request, CancellationToken cancellationToken)
    {
        var limit = request.Limit ?? _workspace.Config.ScanLimit;
        if (limit <= 0)
        {
            return ToolResponse.Failure(ErrorCodes.InvalidRange, "limit must be > 0");
        }

        var maxDepth = request.MaxDepth ?? _workspace.Config.ScanMaxDepth;
        if (maxDepth < 0)
        {
            return ToolResponse.Failure(ErrorCodes.InvalidRange, "maxDepth must be >= 0");
        }

        if (!_workspace.PathPolicy.TryResolveDirectory(request.Root, out var rootPath, out var rootRelative, out var pathError))
        {
            return pathError!;
        }

        var rootDepth = rootRelative == "." ? 0 : rootRelative.Split('/').Length;

        var items = new List<ScanItem>(Math.Min(limit, 512));
        var truncated = false;
        var pending = new Stack<string>();
        pending.Push(rootPath);

        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var current = pending.Pop();

            IEnumerable<string> entries;
            try
            {
                entries = Directory.EnumerateFileSystemEntries(current);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                _logger.Warn($"scan skip directory={current}: {ex.Message}");
                continue;
            }

            foreach (var entry in entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                FileSystemInfo info = Directory.Exists(entry)
                    ? new DirectoryInfo(entry)
                    : new FileInfo(entry);

                if (_workspace.ShouldSkipSymlink(info))
                {
                    continue;
                }

                var relativePath = _workspace.PathPolicy.ToRelativePath(entry);
                if (_workspace.IgnoreMatcher.IsIgnored(relativePath, info is DirectoryInfo))
                {
                    continue;
                }

                if (IgnoreMatcher.MatchesExcludeGlobs(request.ExcludeGlobs, relativePath))
                {
                    continue;
                }

                if (!IgnoreMatcher.MatchesIncludeGlobs(request.IncludeGlobs, relativePath))
                {
                    continue;
                }

                var depth = relativePath.Split('/').Length - rootDepth;
                if (depth < 0)
                {
                    continue;
                }

                if (depth > maxDepth)
                {
                    continue;
                }

                ScanItem item;
                if (info is DirectoryInfo directoryInfo)
                {
                    item = new ScanItem
                    {
                        Path = relativePath,
                        Kind = "dir",
                        MtimeUtc = directoryInfo.LastWriteTimeUtc
                    };

                    if (depth < maxDepth)
                    {
                        pending.Push(entry);
                    }
                }
                else
                {
                    var fileInfo = (FileInfo)info;
                    string? quickHash8 = null;
                    if (fileInfo.Length <= 1024 * 1024)
                    {
                        try
                        {
                            var hash = await _hasher.ComputeContextHashAsync(entry, cancellationToken).ConfigureAwait(false);
                            quickHash8 = _hasher.QuickHash8(hash);
                        }
                        catch (Exception ex)
                        {
                            _logger.Warn($"quick hash failed path={relativePath}: {ex.Message}");
                        }
                    }

                    item = new ScanItem
                    {
                        Path = relativePath,
                        Kind = "file",
                        Size = fileInfo.Length,
                        MtimeUtc = fileInfo.LastWriteTimeUtc,
                        QuickHash8 = quickHash8
                    };
                }

                items.Add(item);
                if (items.Count >= limit)
                {
                    truncated = true;
                    break;
                }
            }

            if (truncated)
            {
                break;
            }
        }

        var data = new ScanData
        {
            Root = rootPath,
            Items = items,
            Truncated = truncated
        };

        return ToolResponse.Success(data, McpJsonSerializerContext.Default.ScanData);
    }
}
