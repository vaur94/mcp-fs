using McpFs.Core;
using McpFs.Core.Hashing;
using McpFs.Core.Ignore;
using McpFs.Core.Limits;
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
        if (request.Limit is <= 0)
        {
            return ToolResponse.Failure(ErrorCodes.InvalidRange, "limit must be > 0");
        }

        if (request.MaxDepth is < 0)
        {
            return ToolResponse.Failure(ErrorCodes.InvalidRange, "maxDepth must be >= 0");
        }

        var effectiveLimit = Math.Min(
            request.Limit ?? _workspace.Config.ScanLimit,
            Math.Min(_workspace.Config.ScanLimit, FsLimits.ScanHardCapLimit));

        var effectiveDepth = Math.Min(
            request.MaxDepth ?? _workspace.Config.ScanMaxDepth,
            Math.Min(_workspace.Config.ScanMaxDepth, FsLimits.ScanHardCapDepth));

        if (!_workspace.PathPolicy.TryResolveDirectory(request.Root, out var rootPath, out _, out var pathError))
        {
            return pathError!;
        }

        var entries = new List<ScanItem>(Math.Min(effectiveLimit, 512));
        var truncated = false;
        var pending = new Stack<(string path, int depth)>();
        pending.Push((rootPath, 0));

        while (pending.Count > 0 && !truncated)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (current, depth) = pending.Pop();

            IEnumerable<string> children;
            try
            {
                children = Directory.EnumerateFileSystemEntries(current);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                _logger.Warn($"scan skip directory: {ex.Message}");
                continue;
            }

            foreach (var child in children)
            {
                cancellationToken.ThrowIfCancellationRequested();

                FileSystemInfo info = Directory.Exists(child)
                    ? new DirectoryInfo(child)
                    : new FileInfo(child);

                if (_workspace.ShouldSkipSymlink(info))
                {
                    continue;
                }

                var relativePath = _workspace.PathPolicy.ToRelativePath(child);
                var isDirectory = info is DirectoryInfo;

                if (_workspace.IgnoreMatcher.IsIgnored(relativePath, isDirectory))
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

                var nextDepth = depth + 1;
                if (nextDepth > effectiveDepth)
                {
                    continue;
                }

                if (isDirectory)
                {
                    entries.Add(new ScanItem
                    {
                        Path = relativePath,
                        Kind = "dir",
                        MtimeUtc = info.LastWriteTimeUtc
                    });

                    if (nextDepth < effectiveDepth)
                    {
                        pending.Push((child, nextDepth));
                    }
                }
                else
                {
                    var fileInfo = (FileInfo)info;
                    string? quickHash8 = null;
                    try
                    {
                        var hash = await _hasher.ComputeContextHashAsync(child, cancellationToken).ConfigureAwait(false);
                        quickHash8 = _hasher.QuickHash8(hash);
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn($"scan hash failed: {ex.Message}");
                    }

                    entries.Add(new ScanItem
                    {
                        Path = relativePath,
                        Kind = "file",
                        Size = fileInfo.Length,
                        MtimeUtc = fileInfo.LastWriteTimeUtc,
                        QuickHash8 = quickHash8
                    });
                }

                if (entries.Count >= effectiveLimit)
                {
                    truncated = true;
                    break;
                }
            }
        }

        var data = new ScanData
        {
            Root = rootPath,
            Entries = entries,
            Truncated = truncated
        };

        return ToolResponse.Success(data, McpJsonSerializerContext.Default.ScanData);
    }
}
