using McpFs.Core;
using McpFs.Core.Hashing;
using McpFs.Core.Limits;
using McpFs.Core.Patch;
using McpFs.Rpc;

namespace McpFs.Tools;

public sealed class PatchPreviewTool
{
    private readonly Workspace _workspace;
    private readonly PatchEngine _engine;

    public PatchPreviewTool(Workspace workspace, ContentHasher hasher)
    {
        _workspace = workspace;
        _engine = new PatchEngine(hasher);
    }

    public async Task<ToolResponse> ExecuteAsync(PatchPreviewRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Path))
        {
            return ToolResponse.Failure(ErrorCodes.InvalidPath, "path is required");
        }

        if (string.IsNullOrWhiteSpace(request.PreHash))
        {
            return ToolResponse.Failure(ErrorCodes.InvalidRange, "preHash is required");
        }

        var mode = string.IsNullOrWhiteSpace(request.Mode) ? "strict" : request.Mode.Trim().ToLowerInvariant();
        if (!string.Equals(mode, "strict", StringComparison.Ordinal))
        {
            return ToolResponse.Failure(ErrorCodes.InvalidRange, "mode must be strict");
        }

        if (!_workspace.PathPolicy.TryResolveExistingFile(request.Path, out var fullPath, out var relativePath, out _, out var pathError))
        {
            return pathError!;
        }

        var options = new PatchRuntimeOptions
        {
            MaxPatchBytes = Math.Min(_workspace.Config.PatchMaxBytes, FsLimits.PatchHardCapBytes),
            MaxEdits = Math.Min(_workspace.Config.PatchMaxEdits, FsLimits.PatchHardCapEdits),
            MaxFileSizeBytes = Math.Min(_workspace.Config.PatchMaxFileSizeBytes, FsLimits.PatchHardCapFileSizeBytes)
        };

        var compute = await _engine.ComputeAsync(
            fullPath,
            relativePath,
            request.PreHash,
            request.Edits,
            options,
            cancellationToken).ConfigureAwait(false);

        if (compute.Error is not null)
        {
            return compute.Error;
        }

        var data = new PatchPreviewData
        {
            WouldApply = true,
            PostHash = compute.PostHash,
            DiffSummary = compute.DiffSummary,
            BytesChanged = compute.BytesChanged,
            LineDelta = compute.LineDelta
        };

        return ToolResponse.Success(data, McpJsonSerializerContext.Default.PatchPreviewData);
    }
}
