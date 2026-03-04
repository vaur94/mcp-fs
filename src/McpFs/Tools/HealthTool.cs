using McpFs.Core;
using McpFs.Rpc;

namespace McpFs.Tools;

public sealed class HealthTool
{
    private readonly Workspace _workspace;
    private readonly DateTimeOffset _startedAtUtc;

    public HealthTool(Workspace workspace, DateTimeOffset startedAtUtc)
    {
        _workspace = workspace;
        _startedAtUtc = startedAtUtc;
    }

    public ToolResponse Execute()
    {
        var data = new HealthData
        {
            Status = "ok",
            Version = AppMetadata.Version,
            UptimeMs = Math.Max(0, (long)(DateTimeOffset.UtcNow - _startedAtUtc).TotalMilliseconds),
            Root = _workspace.RootPath,
            FollowSymlinks = _workspace.FollowSymlinks,
            Limits = new DefaultsData
            {
                SearchMaxResults = _workspace.Config.SearchMaxResults,
                SearchSnippetBytes = _workspace.Config.SearchSnippetBytes,
                SearchMaxFilesScanned = _workspace.Config.SearchMaxFilesScanned,
                SearchMaxFileSizeBytes = _workspace.Config.SearchMaxFileSizeBytes,
                SearchTimeoutMs = _workspace.Config.SearchTimeoutMs,
                OpenMaxBytes = _workspace.Config.OpenMaxBytes,
                OpenMaxLines = _workspace.Config.OpenMaxLines,
                PatchMaxBytes = _workspace.Config.PatchMaxBytes,
                PatchMaxEdits = _workspace.Config.PatchMaxEdits,
                PatchMaxFileSizeBytes = _workspace.Config.PatchMaxFileSizeBytes,
                ScanLimit = _workspace.Config.ScanLimit,
                ScanMaxDepth = _workspace.Config.ScanMaxDepth,
                FollowSymlinks = _workspace.Config.FollowSymlinks
            }
        };

        return ToolResponse.Success(data, McpJsonSerializerContext.Default.HealthData);
    }
}
