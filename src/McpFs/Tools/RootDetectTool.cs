using McpFs.Core;
using McpFs.Rpc;

namespace McpFs.Tools;

public sealed class RootDetectTool
{
    private readonly Workspace _workspace;

    public RootDetectTool(Workspace workspace)
    {
        _workspace = workspace;
    }

    public ToolResponse Execute()
    {
        var data = new RootDetectData
        {
            Root = _workspace.RootPath,
            Reason = _workspace.DetectionReason
        };

        return ToolResponse.Success(data, McpJsonSerializerContext.Default.RootDetectData);
    }
}
