using System.Runtime.InteropServices;
using McpFs.Config;
using McpFs.Core;
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
            Version = "0.1.0",
            ToolAvailability = new ToolAvailabilityData
            {
                Ripgrep = hasRipgrep
            },
            Defaults = new DefaultsData
            {
                SearchMaxResults = _config.SearchMaxResults,
                SearchSnippetBytes = _config.SearchSnippetBytes,
                OpenMaxBytes = _config.OpenMaxBytes,
                ScanLimit = _config.ScanLimit,
                FollowSymlinks = _workspace.FollowSymlinks
            }
        };

        return ToolResponse.Success(data, McpJsonSerializerContext.Default.CapabilitiesData);
    }
}
