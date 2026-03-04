namespace McpFs.Config;

public sealed class McpFsConfig
{
    public string? WorkspaceRoot { get; init; }
    public bool FollowSymlinks { get; init; }
    public int SearchMaxResults { get; init; } = 100;
    public int SearchSnippetBytes { get; init; } = 220;
    public int OpenMaxBytes { get; init; } = 65_536;
    public int OpenMaxLines { get; init; } = 200;
    public int ScanLimit { get; init; } = 500;
    public int ScanMaxDepth { get; init; } = 16;
    public string LogLevel { get; init; } = "info";
}
