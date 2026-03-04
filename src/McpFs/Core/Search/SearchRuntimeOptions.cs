namespace McpFs.Core.Search;

public sealed class SearchRuntimeOptions
{
    public int MaxResults { get; init; }
    public int SnippetBytes { get; init; }
    public int MaxFilesScanned { get; init; }
    public int MaxFileSizeBytes { get; init; }
    public int TimeoutMs { get; init; }
}
