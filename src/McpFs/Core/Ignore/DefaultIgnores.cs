namespace McpFs.Core.Ignore;

public static class DefaultIgnores
{
    private static readonly HashSet<string> IgnoredDirectoryNames = new(StringComparer.Ordinal)
    {
        ".git",
        "bin",
        "obj",
        "node_modules",
        "dist",
        ".idea",
        ".vs"
    };

    public static bool IsIgnoredByDefault(string relativePath)
    {
        var normalized = Normalize(relativePath);
        var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            if (IgnoredDirectoryNames.Contains(part))
            {
                return true;
            }
        }

        return false;
    }

    public static IReadOnlyList<string> ToRipgrepGlobs()
    {
        return
        [
            "**/.git/**",
            "**/bin/**",
            "**/obj/**",
            "**/node_modules/**",
            "**/dist/**",
            "**/.idea/**",
            "**/.vs/**"
        ];
    }

    private static string Normalize(string path)
    {
        var normalized = path.Replace('\\', '/');
        return normalized.TrimStart('.', '/');
    }
}
