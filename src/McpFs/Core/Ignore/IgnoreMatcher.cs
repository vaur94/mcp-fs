using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace McpFs.Core.Ignore;

public sealed class IgnoreMatcher
{
    private readonly IReadOnlyList<IgnoreRule> _gitIgnoreRules;

    private IgnoreMatcher(IReadOnlyList<IgnoreRule> gitIgnoreRules)
    {
        _gitIgnoreRules = gitIgnoreRules;
    }

    public static IgnoreMatcher Load(string rootPath)
    {
        var rules = GitIgnoreParser.ParseFile(Path.Combine(rootPath, ".gitignore"));
        return new IgnoreMatcher(rules);
    }

    public bool IsIgnored(string relativePath, bool isDirectory)
    {
        var normalized = Normalize(relativePath);

        if (DefaultIgnores.IsIgnoredByDefault(normalized))
        {
            return true;
        }

        var ignored = false;
        foreach (var rule in _gitIgnoreRules)
        {
            if (!RuleMatches(rule, normalized, isDirectory))
            {
                continue;
            }

            ignored = !rule.Negate;
        }

        return ignored;
    }

    public static bool MatchesIncludeGlobs(IReadOnlyList<string>? globs, string relativePath)
    {
        if (globs is null || globs.Count == 0)
        {
            return true;
        }

        var normalized = Normalize(relativePath);
        foreach (var glob in globs)
        {
            if (GlobMatcher.IsMatch(glob, normalized))
            {
                return true;
            }
        }

        return false;
    }

    public static bool MatchesExcludeGlobs(IReadOnlyList<string>? globs, string relativePath)
    {
        if (globs is null || globs.Count == 0)
        {
            return false;
        }

        var normalized = Normalize(relativePath);
        foreach (var glob in globs)
        {
            if (GlobMatcher.IsMatch(glob, normalized))
            {
                return true;
            }
        }

        return false;
    }

    private static bool RuleMatches(IgnoreRule rule, string path, bool isDirectory)
    {
        if (rule.DirectoryRule)
        {
            var dirPattern = rule.Pattern;
            if (rule.Anchored)
            {
                if (path.Equals(dirPattern, StringComparison.Ordinal) || path.StartsWith(dirPattern + "/", StringComparison.Ordinal))
                {
                    return true;
                }
            }
            else
            {
                var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Any(part => part.Equals(dirPattern, StringComparison.Ordinal)))
                {
                    return true;
                }

                if (path.Contains('/'))
                {
                    if (path.Contains($"/{dirPattern}/", StringComparison.Ordinal) ||
                        path.EndsWith($"/{dirPattern}", StringComparison.Ordinal) ||
                        path.StartsWith(dirPattern + "/", StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            if (isDirectory && path.Equals(dirPattern, StringComparison.Ordinal))
            {
                return true;
            }
        }

        if (rule.ContainsSlash || rule.Anchored)
        {
            if (GlobMatcher.IsMatch(rule.Pattern, path))
            {
                return true;
            }

            return !rule.Anchored && GlobMatcher.IsMatch($"**/{rule.Pattern}", path);
        }

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in segments)
        {
            if (GlobMatcher.IsMatch(rule.Pattern, segment))
            {
                return true;
            }
        }

        return false;
    }

    private static string Normalize(string path)
    {
        var normalized = path.Replace('\\', '/');
        return normalized.TrimStart('.', '/');
    }
}

internal static class GlobMatcher
{
    private static readonly ConcurrentDictionary<string, Regex> Cache = new(StringComparer.Ordinal);

    public static bool IsMatch(string pattern, string path)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return false;
        }

        var normalizedPattern = NormalizePattern(pattern);
        var normalizedPath = path.Replace('\\', '/');

        if (!normalizedPattern.Contains('/'))
        {
            var fileName = Path.GetFileName(normalizedPath);
            return IsRegexMatch(normalizedPattern, fileName) || IsRegexMatch($"**/{normalizedPattern}", normalizedPath);
        }

        return IsRegexMatch(normalizedPattern, normalizedPath);
    }

    private static bool IsRegexMatch(string pattern, string path)
    {
        var regex = Cache.GetOrAdd(pattern, static p => BuildRegex(p));
        return regex.IsMatch(path);
    }

    private static Regex BuildRegex(string pattern)
    {
        var escaped = Regex.Escape(pattern)
            .Replace("\\*\\*", "__DOUBLE_STAR__")
            .Replace("\\*", "[^/]*")
            .Replace("\\?", "[^/]")
            .Replace("__DOUBLE_STAR__", ".*");

        return new Regex($"^{escaped}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    }

    private static string NormalizePattern(string pattern)
    {
        var normalized = pattern.Trim();
        if (normalized.StartsWith('!'))
        {
            normalized = normalized[1..];
        }

        normalized = normalized.Replace('\\', '/');
        normalized = normalized.TrimStart('/');
        return normalized;
    }
}
