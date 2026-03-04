namespace McpFs.Config;

public sealed class McpFsConfig
{
    public string? WorkspaceRoot { get; init; }
    public bool FollowSymlinks { get; init; }
    public int SearchMaxResults { get; init; } = 100;
    public int SearchSnippetBytes { get; init; } = 220;
    public int SearchMaxFilesScanned { get; init; } = 5_000;
    public int SearchMaxFileSizeBytes { get; init; } = 2 * 1024 * 1024;
    public int SearchTimeoutMs { get; init; } = 5_000;
    public int OpenMaxBytes { get; init; } = 65_536;
    public int OpenMaxLines { get; init; } = 200;
    public int PatchMaxBytes { get; init; } = 256 * 1024;
    public int PatchMaxEdits { get; init; } = 50;
    public int PatchMaxFileSizeBytes { get; init; } = 2 * 1024 * 1024;
    public int ScanLimit { get; init; } = 500;
    public int ScanMaxDepth { get; init; } = 16;
    public string LogLevel { get; init; } = "info";
    public string LogFormat { get; init; } = "plain";
    public bool Quiet { get; init; }
}
