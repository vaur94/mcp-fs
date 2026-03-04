using McpFs.Core;
using McpFs.Core.Hashing;
using McpFs.Core.Search;
using McpFs.Logging;
using McpFs.Rpc;

namespace McpFs.Tools;

public sealed class SearchTool
{
    private readonly Workspace _workspace;
    private readonly RipgrepRunner _ripgrepRunner;
    private readonly FallbackSearcher _fallbackSearcher;
    private readonly ContentHasher _hasher;
    private readonly StderrLogger _logger;

    public SearchTool(
        Workspace workspace,
        RipgrepRunner ripgrepRunner,
        FallbackSearcher fallbackSearcher,
        ContentHasher hasher,
        StderrLogger logger)
    {
        _workspace = workspace;
        _ripgrepRunner = ripgrepRunner;
        _fallbackSearcher = fallbackSearcher;
        _hasher = hasher;
        _logger = logger;
    }

    public async Task<ToolResponse> ExecuteAsync(SearchRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return ToolResponse.Failure(ErrorCodes.InvalidRange, "query is required");
        }

        var maxResults = request.MaxResults ?? _workspace.Config.SearchMaxResults;
        if (maxResults <= 0)
        {
            return ToolResponse.Failure(ErrorCodes.InvalidRange, "maxResults must be > 0");
        }

        var snippetBytes = request.SnippetBytes ?? _workspace.Config.SearchSnippetBytes;
        if (snippetBytes < 32)
        {
            return ToolResponse.Failure(ErrorCodes.InvalidRange, "snippetBytes must be >= 32");
        }

        if (!_workspace.PathPolicy.TryResolveDirectory(request.Root, out var searchRootPath, out var searchRootRelative, out var pathError))
        {
            return pathError!;
        }

        SearchEngineResult? engineResult = null;

        try
        {
            engineResult = await _ripgrepRunner.SearchAsync(
                _workspace.RootPath,
                searchRootRelative,
                request,
                maxResults,
                snippetBytes,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.Warn($"ripgrep search failed and fallback will be used: {ex.Message}");
        }

        engineResult ??= await _fallbackSearcher.SearchAsync(
            _workspace,
            searchRootPath,
            request,
            maxResults,
            snippetBytes,
            cancellationToken).ConfigureAwait(false);

        var hashedMatches = await FillContextHashesAsync(engineResult.Matches, cancellationToken).ConfigureAwait(false);

        var data = new SearchData
        {
            Matches = hashedMatches,
            Truncated = engineResult.Truncated,
            Engine = engineResult.Engine
        };

        return ToolResponse.Success(data, McpJsonSerializerContext.Default.SearchData);
    }

    private async Task<IReadOnlyList<SearchMatch>> FillContextHashesAsync(
        IReadOnlyList<SearchMatch> matches,
        CancellationToken cancellationToken)
    {
        var hashCache = new Dictionary<string, string>(StringComparer.Ordinal);
        var output = new List<SearchMatch>(matches.Count);

        foreach (var match in matches)
        {
            var normalizedPath = NormalizeRelativePath(match.Path);
            var contextHash = match.ContextHash;

            if (string.IsNullOrEmpty(contextHash))
            {
                if (hashCache.TryGetValue(normalizedPath, out var cached))
                {
                    contextHash = cached;
                }
                else if (_workspace.PathPolicy.TryResolvePath(normalizedPath, out var fullPath, out _, out _))
                {
                    contextHash = await _hasher.ComputeContextHashAsync(fullPath, cancellationToken).ConfigureAwait(false);
                    hashCache[normalizedPath] = contextHash;
                }
            }

            output.Add(new SearchMatch
            {
                Path = normalizedPath,
                Line = match.Line,
                Col = match.Col,
                Snippet = match.Snippet,
                Range = match.Range,
                ContextHash = contextHash ?? string.Empty
            });
        }

        return output;
    }

    private string NormalizeRelativePath(string path)
    {
        var normalized = path.Replace('\\', '/');

        if (Path.IsPathRooted(normalized))
        {
            try
            {
                normalized = _workspace.PathPolicy.ToRelativePath(path);
            }
            catch
            {
                // Keep original on failure.
            }
        }

        return normalized.TrimStart('.', '/');
    }
}
