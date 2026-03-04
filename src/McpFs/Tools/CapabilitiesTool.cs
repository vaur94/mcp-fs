using System.Runtime.InteropServices;
using McpFs.Config;
using McpFs.Core;
using McpFs.Core.Limits;
using McpFs.Core.Search;
using McpFs.Rpc;

namespace McpFs.Tools;

public sealed class CapabilitiesTool
{
    private readonly Workspace _workspace;
    private readonly McpFsConfig _config;
    private readonly RipgrepRunner _ripgrepRunner;

    public CapabilitiesTool(Workspace workspace, McpFsConfig config, RipgrepRunner ripgrepRunner)
    {
        _workspace = workspace;
        _config = config;
        _ripgrepRunner = ripgrepRunner;
    }

    public ToolResponse Execute()
    {
        var hasRipgrep = _ripgrepRunner.IsAvailableAsync(CancellationToken.None).GetAwaiter().GetResult();

        var data = new CapabilitiesData
        {
            Os = RuntimeInformation.OSDescription,
            Arch = RuntimeInformation.OSArchitecture.ToString(),
            PathSeparator = Path.DirectorySeparatorChar.ToString(),
            Version = AppMetadata.Version,
            ToolAvailability = new ToolAvailabilityData
            {
                Ripgrep = hasRipgrep
            },
            Defaults = new DefaultsData
            {
                SearchMaxResults = _config.SearchMaxResults,
                SearchSnippetBytes = _config.SearchSnippetBytes,
                SearchMaxFilesScanned = _config.SearchMaxFilesScanned,
                SearchMaxFileSizeBytes = _config.SearchMaxFileSizeBytes,
                SearchTimeoutMs = _config.SearchTimeoutMs,
                OpenMaxBytes = _config.OpenMaxBytes,
                OpenMaxLines = _config.OpenMaxLines,
                PatchMaxBytes = _config.PatchMaxBytes,
                PatchMaxEdits = _config.PatchMaxEdits,
                PatchMaxFileSizeBytes = _config.PatchMaxFileSizeBytes,
                ScanLimit = _config.ScanLimit,
                ScanMaxDepth = _config.ScanMaxDepth,
                FollowSymlinks = _workspace.FollowSymlinks
            },
            Limits = new LimitsData
            {
                OpenHardCapBytes = FsLimits.OpenHardCapBytes,
                OpenHardCapLines = FsLimits.OpenHardCapLines,
                SearchHardCapResults = FsLimits.SearchHardCapResults,
                SearchHardCapSnippetBytes = FsLimits.SearchHardCapSnippetBytes,
                SearchHardCapFilesScanned = FsLimits.SearchHardCapFilesScanned,
                SearchHardCapFileSizeBytes = FsLimits.SearchHardCapFileSizeBytes,
                SearchHardCapTimeoutMs = FsLimits.SearchHardCapTimeoutMs,
                PatchHardCapBytes = FsLimits.PatchHardCapBytes,
                PatchHardCapEdits = FsLimits.PatchHardCapEdits,
                PatchHardCapFileSizeBytes = FsLimits.PatchHardCapFileSizeBytes,
                ScanHardCapLimit = FsLimits.ScanHardCapLimit,
                ScanHardCapDepth = FsLimits.ScanHardCapDepth
            },
            Features =
            [
                "stdio-jsonrpc",
                "strict-prehash-patch",
                "atomic-write",
                "root-sandbox",
                "ignore-root-gitignore-subset"
            ]
        };

        return ToolResponse.Success(data, McpJsonSerializerContext.Default.CapabilitiesData);
    }
}
