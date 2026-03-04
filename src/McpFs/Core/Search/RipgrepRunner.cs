using System.Diagnostics;
using System.Text;
using System.Text.Json;
using McpFs.Core.Ignore;
using McpFs.Logging;
using McpFs.Rpc;

namespace McpFs.Core.Search;

public sealed class RipgrepRunner
{
    private readonly StderrLogger _logger;
    private readonly bool _enabled;
    private Task<bool>? _availabilityTask;

    public RipgrepRunner(StderrLogger logger, bool enabled = true)
    {
        _logger = logger;
        _enabled = enabled;
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
    {
        if (!_enabled)
        {
            return false;
        }

        _availabilityTask ??= ProbeAvailabilityAsync(cancellationToken);
        return await _availabilityTask.ConfigureAwait(false);
    }

    public async Task<SearchEngineResult?> SearchAsync(
        string workspaceRoot,
        string searchRoot,
        SearchRequest request,
        SearchRuntimeOptions options,
        CancellationToken cancellationToken)
    {
        if (!await IsAvailableAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        var args = new List<string>
        {
            "--json",
            "--line-number",
            "--column",
            "--color",
            "never",
            "--no-messages",
            "--no-ignore"
        };

        if (request.Regex != true)
        {
            args.Add("--fixed-strings");
        }

        if (request.CaseSensitive != true)
        {
            args.Add("--ignore-case");
        }

        args.Add("--max-filesize");
        args.Add(options.MaxFileSizeBytes.ToString());

        foreach (var glob in request.Glob ?? Array.Empty<string>())
        {
            args.Add("--glob");
            args.Add(glob);
        }

        foreach (var glob in request.ExcludeGlob ?? Array.Empty<string>())
        {
            args.Add("--glob");
            args.Add($"!{glob}");
        }

        foreach (var glob in DefaultIgnores.ToRipgrepGlobs())
        {
            args.Add("--glob");
            args.Add($"!{glob}");
        }

        var rootIgnoreFile = Path.Combine(workspaceRoot, ".gitignore");
        if (File.Exists(rootIgnoreFile))
        {
            args.Add("--ignore-file");
            args.Add(rootIgnoreFile);
        }

        args.Add(request.Query);
        args.Add(searchRoot);

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "rg",
                WorkingDirectory = workspaceRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        foreach (var arg in args)
        {
            process.StartInfo.ArgumentList.Add(arg);
        }

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            _logger.Warn($"ripgrep unavailable: {ex.Message}");
            return null;
        }

        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        var stopwatch = Stopwatch.StartNew();

        var matches = new List<SearchMatch>(Math.Min(options.MaxResults, 256));
        var matchedFiles = new HashSet<string>(StringComparer.Ordinal);
        var truncated = false;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (stopwatch.ElapsedMilliseconds > options.TimeoutMs)
            {
                truncated = true;
                KillSafely(process);
                break;
            }

            var line = await process.StandardOutput.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
            {
                break;
            }

            if (!TryParseMatchJsonLine(line, options.SnippetBytes, out var parsed))
            {
                continue;
            }

            foreach (var match in parsed)
            {
                matchedFiles.Add(match.Path);
                if (matchedFiles.Count > options.MaxFilesScanned)
                {
                    truncated = true;
                    KillSafely(process);
                    break;
                }

                matches.Add(match);
                if (matches.Count >= options.MaxResults)
                {
                    truncated = true;
                    KillSafely(process);
                    break;
                }
            }

            if (truncated)
            {
                break;
            }
        }

        try
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // ignored
        }

        var stderr = await stderrTask.ConfigureAwait(false);
        if (!truncated && process.ExitCode > 1)
        {
            _logger.Warn($"ripgrep exitCode={process.ExitCode}, stderr={stderr.Trim()}");
            return null;
        }

        return new SearchEngineResult
        {
            Matches = matches,
            Truncated = truncated,
            Engine = "rg"
        };
    }

    internal static bool TryParseMatchJsonLine(string jsonLine, int snippetBytes, out IReadOnlyList<SearchMatch> matches)
    {
        matches = Array.Empty<SearchMatch>();

        using var doc = JsonDocument.Parse(jsonLine);
        var root = doc.RootElement;

        if (!root.TryGetProperty("type", out var typeElement) ||
            !string.Equals(typeElement.GetString(), "match", StringComparison.Ordinal))
        {
            return false;
        }

        if (!root.TryGetProperty("data", out var dataElement))
        {
            return false;
        }

        var path = dataElement
            .GetProperty("path")
            .GetProperty("text")
            .GetString();
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var lineNumber = dataElement.GetProperty("line_number").GetInt32();
        var lineText = dataElement.GetProperty("lines").GetProperty("text").GetString() ?? string.Empty;
        lineText = lineText.TrimEnd('\r', '\n');

        if (!dataElement.TryGetProperty("submatches", out var submatches))
        {
            return false;
        }

        var lineBytes = Encoding.UTF8.GetBytes(lineText);
        var parsed = new List<SearchMatch>();

        foreach (var submatch in submatches.EnumerateArray())
        {
            var startByte = submatch.GetProperty("start").GetInt32();
            var endByte = submatch.GetProperty("end").GetInt32();

            var startChar = ByteOffsetToCharIndex(lineBytes, startByte);
            var endChar = ByteOffsetToCharIndex(lineBytes, endByte);
            var length = Math.Max(0, endChar - startChar);

            parsed.Add(new SearchMatch
            {
                Path = path.Replace('\\', '/'),
                Line = lineNumber,
                Col = startChar + 1,
                Snippet = BuildSnippet(lineText, startChar, length, snippetBytes),
                Range = new MatchRange
                {
                    StartLine = lineNumber,
                    StartCol = startChar + 1,
                    EndLine = lineNumber,
                    EndCol = Math.Max(startChar + 1, endChar + 1)
                },
                ContextHash = string.Empty
            });
        }

        matches = parsed;
        return parsed.Count > 0;
    }

    private static int ByteOffsetToCharIndex(byte[] utf8, int byteOffset)
    {
        if (byteOffset <= 0)
        {
            return 0;
        }

        if (byteOffset >= utf8.Length)
        {
            return Encoding.UTF8.GetCharCount(utf8);
        }

        return Encoding.UTF8.GetCharCount(utf8, 0, byteOffset);
    }

    private static string BuildSnippet(string line, int start, int length, int maxBytes)
    {
        if (string.IsNullOrEmpty(line))
        {
            return string.Empty;
        }

        var safeLength = Math.Max(length, 1);
        var halfWindow = Math.Max(16, maxBytes / 4);
        var snippetStart = Math.Max(0, start - halfWindow);
        var snippetEnd = Math.Min(line.Length, start + safeLength + halfWindow);

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

    private static void KillSafely(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // ignored
        }
    }

    private static async Task<bool> ProbeAvailabilityAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "rg",
                    ArgumentList = { "--version" },
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
