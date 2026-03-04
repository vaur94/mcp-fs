using McpFs.Rpc;

namespace McpFs.Core.Search;

public sealed class SearchEngineResult
{
    public IReadOnlyList<SearchMatch> Matches { get; init; } = Array.Empty<SearchMatch>();
    public bool Truncated { get; init; }
    public string Engine { get; init; } = string.Empty;
}
