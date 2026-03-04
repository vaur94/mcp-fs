using System.Text.Json;

namespace McpFs.Config;

public static class ConfigLoader
{
    public static McpFsConfig Load()
    {
        var configPath = Environment.GetEnvironmentVariable("MCP_FS_CONFIG");
        if (string.IsNullOrWhiteSpace(configPath))
        {
            var local = Path.Combine(Directory.GetCurrentDirectory(), "mcp-fs.json");
            if (File.Exists(local))
            {
                configPath = local;
            }
        }

        if (string.IsNullOrWhiteSpace(configPath) || !File.Exists(configPath))
        {
            return new McpFsConfig();
        }

        try
        {
            var raw = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<McpFsConfig>(raw, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (config is null)
            {
                return new McpFsConfig();
            }

            return Normalize(config);
        }
        catch
        {
            return new McpFsConfig();
        }
    }

    private static McpFsConfig Normalize(McpFsConfig config)
    {
        return new McpFsConfig
        {
            WorkspaceRoot = string.IsNullOrWhiteSpace(config.WorkspaceRoot) ? null : config.WorkspaceRoot,
            FollowSymlinks = config.FollowSymlinks,
            SearchMaxResults = config.SearchMaxResults <= 0 ? 100 : Math.Min(config.SearchMaxResults, 10_000),
            SearchSnippetBytes = config.SearchSnippetBytes <= 0 ? 220 : Math.Min(config.SearchSnippetBytes, 2_048),
            OpenMaxBytes = config.OpenMaxBytes <= 0 ? 65_536 : Math.Min(config.OpenMaxBytes, 4 * 1024 * 1024),
            OpenMaxLines = config.OpenMaxLines <= 0 ? 200 : Math.Min(config.OpenMaxLines, 50_000),
            ScanLimit = config.ScanLimit <= 0 ? 500 : Math.Min(config.ScanLimit, 50_000),
            ScanMaxDepth = config.ScanMaxDepth <= 0 ? 16 : Math.Min(config.ScanMaxDepth, 64),
            LogLevel = string.IsNullOrWhiteSpace(config.LogLevel) ? "info" : config.LogLevel
        };
    }
}
