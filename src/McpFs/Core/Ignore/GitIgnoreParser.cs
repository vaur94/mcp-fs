namespace McpFs.Core.Ignore;

public static class GitIgnoreParser
{
    public static IReadOnlyList<IgnoreRule> ParseFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return Array.Empty<IgnoreRule>();
        }

        var rules = new List<IgnoreRule>();
        var lines = File.ReadAllLines(filePath);

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var negate = false;
            if (line.StartsWith('!'))
            {
                negate = true;
                line = line[1..].Trim();
            }

            if (line.Length == 0)
            {
                continue;
            }

            var directoryRule = line.EndsWith('/');
            line = line.Trim('/');
            if (line.Length == 0)
            {
                continue;
            }

            line = line.Replace('\\', '/');
            var anchored = line.StartsWith('/');
            line = line.TrimStart('/');

            rules.Add(new IgnoreRule(
                line,
                negate,
                anchored,
                directoryRule,
                line.Contains('/')));
        }

        return rules;
    }
}

public sealed class IgnoreRule
{
    public IgnoreRule(string pattern, bool negate, bool anchored, bool directoryRule, bool containsSlash)
    {
        Pattern = pattern;
        Negate = negate;
        Anchored = anchored;
        DirectoryRule = directoryRule;
        ContainsSlash = containsSlash;
    }

    public string Pattern { get; }
    public bool Negate { get; }
    public bool Anchored { get; }
    public bool DirectoryRule { get; }
    public bool ContainsSlash { get; }
}
