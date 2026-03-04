using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using McpFs.Core.Hashing;
using McpFs.Core.Ignore;
using McpFs.Logging;
using McpFs.Rpc;

namespace McpFs.Core.Search;

public sealed class FallbackSearcher
{
    private readonly ContentHasher _hasher;
    private readonly StderrLogger _logger;

    public FallbackSearcher(ContentHasher hasher, StderrLogger logger)
    {
        _hasher = hasher;
        _logger = logger;
    }

    public async Task<SearchEngineResult> SearchAsync(
        Workspace workspace,
        string searchRoot,
        SearchRequest request,
        SearchRuntimeOptions options,
        CancellationToken cancellationToken)
    {
        var matches = new List<SearchMatch>(Math.Min(options.MaxResults, 256));
        var contextHashCache = new Dictionary<string, string>(StringComparer.Ordinal);
        var truncated = false;
        var filesScanned = 0;

        Regex? regex = null;
        if (request.Regex == true)
        {
            var regexOptions = RegexOptions.Compiled | RegexOptions.CultureInvariant;
            if (request.CaseSensitive != true)
            {
                regexOptions |= RegexOptions.IgnoreCase;
            }

            regex = new Regex(request.Query, regexOptions);
        }

        var stopwatch = Stopwatch.StartNew();
        var pending = new Stack<string>();
        pending.Push(searchRoot);

        while (pending.Count > 0 && !truncated)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (stopwatch.ElapsedMilliseconds > options.TimeoutMs)
            {
                truncated = true;
                break;
            }

            var currentDir = pending.Pop();

            IEnumerable<string> entries;
            try
            {
                entries = Directory.EnumerateFileSystemEntries(currentDir);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                _logger.Warn($"search skip directory={currentDir}: {ex.Message}");
                continue;
            }

            foreach (var entry in entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (stopwatch.ElapsedMilliseconds > options.TimeoutMs)
                {
                    truncated = true;
                    break;
                }

                FileSystemInfo info = Directory.Exists(entry)
                    ? new DirectoryInfo(entry)
                    : new FileInfo(entry);

                if (workspace.ShouldSkipSymlink(info))
                {
                    continue;
                }

                var relativePath = workspace.PathPolicy.ToRelativePath(entry);
                if (workspace.IgnoreMatcher.IsIgnored(relativePath, info is DirectoryInfo))
                {
                    continue;
                }

                if (IgnoreMatcher.MatchesExcludeGlobs(request.ExcludeGlob, relativePath))
                {
                    continue;
                }

                if (!IgnoreMatcher.MatchesIncludeGlobs(request.Glob, relativePath))
                {
                    continue;
                }

                if (info is DirectoryInfo)
                {
                    pending.Push(entry);
                    continue;
                }

                filesScanned++;
                if (filesScanned > options.MaxFilesScanned)
                {
                    truncated = true;
                    break;
                }

                var fileInfo = (FileInfo)info;
                if (fileInfo.Length > options.MaxFileSizeBytes)
                {
                    continue;
                }

                if (await IsLikelyBinaryAsync(entry, cancellationToken).ConfigureAwait(false))
                {
                    continue;
                }

                await foreach (var match in SearchFileAsync(
                    entry,
                    relativePath,
                    request,
                    regex,
                    options,
                    stopwatch,
                    contextHashCache,
                    cancellationToken).ConfigureAwait(false))
                {
                    matches.Add(match);
                    if (matches.Count >= options.MaxResults)
                    {
                        truncated = true;
                        break;
                    }
                }

                if (truncated)
                {
                    break;
                }
            }
        }

        return new SearchEngineResult
        {
            Matches = matches,
            Truncated = truncated,
            Engine = "fallback"
        };
    }

    private async IAsyncEnumerable<SearchMatch> SearchFileAsync(
        string filePath,
        string relativePath,
        SearchRequest request,
        Regex? regex,
        SearchRuntimeOptions options,
        Stopwatch stopwatch,
        Dictionary<string, string> contextHashCache,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 16 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        var lineNo = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (stopwatch.ElapsedMilliseconds > options.TimeoutMs)
            {
                yield break;
            }

            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
            {
                break;
            }

            lineNo++;

            if (regex is not null)
            {
                foreach (Match match in regex.Matches(line))
                {
                    if (!match.Success)
                    {
                        continue;
                    }

                    var contextHash = await GetContextHashAsync(filePath, contextHashCache, cancellationToken).ConfigureAwait(false);
                    yield return BuildMatch(relativePath, lineNo, match.Index, match.Length, line, options.SnippetBytes, contextHash);
                }

                continue;
            }

            var comparison = request.CaseSensitive == true
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

            var start = 0;
            while (start <= line.Length)
            {
                var index = line.IndexOf(request.Query, start, comparison);
                if (index < 0)
                {
                    break;
                }

                var contextHash = await GetContextHashAsync(filePath, contextHashCache, cancellationToken).ConfigureAwait(false);
                yield return BuildMatch(relativePath, lineNo, index, request.Query.Length, line, options.SnippetBytes, contextHash);

                start = index + Math.Max(1, request.Query.Length);
            }
        }
    }

    private static SearchMatch BuildMatch(string relativePath, int line, int startIndex, int length, string lineText, int snippetBytes, string contextHash)
    {
        var safeLength = Math.Max(1, length);

        return new SearchMatch
        {
            Path = relativePath,
            Line = line,
            Col = startIndex + 1,
            Snippet = BuildSnippet(lineText, startIndex, safeLength, snippetBytes),
            Range = new MatchRange
            {
                StartLine = line,
                StartCol = startIndex + 1,
                EndLine = line,
                EndCol = startIndex + safeLength + 1
            },
            ContextHash = contextHash
        };
    }

    private async Task<string> GetContextHashAsync(
        string filePath,
        Dictionary<string, string> cache,
        CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(filePath, out var cached))
        {
            return cached;
        }

        var hash = await _hasher.ComputeContextHashAsync(filePath, cancellationToken).ConfigureAwait(false);
        cache[filePath] = hash;
        return hash;
    }

    private static string BuildSnippet(string line, int start, int length, int maxBytes)
    {
        if (string.IsNullOrEmpty(line))
        {
            return string.Empty;
        }

        var halfWindow = Math.Max(16, maxBytes / 4);
        var snippetStart = Math.Max(0, start - halfWindow);
        var snippetEnd = Math.Min(line.Length, start + length + halfWindow);

        var snippet = line[snippetStart..snippetEnd];

        if (snippetStart > 0)
        {
            snippet = "..." + snippet;
        }

        if (snippetEnd < line.Length)
        {
            snippet += "...";
        }

        while (Encoding.UTF8.GetByteCount(snippet) > maxBytes && snippet.Length > 4)
        {
            snippet = snippet[..^1];
        }

        return snippet;
    }

    private static async Task<bool> IsLikelyBinaryAsync(string filePath, CancellationToken cancellationToken)
    {
        var buffer = new byte[1024];
        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
        if (read == 0)
        {
            return false;
        }

        for (var i = 0; i < read; i++)
        {
            if (buffer[i] == 0)
            {
                return true;
            }
        }

        return false;
    }
}
