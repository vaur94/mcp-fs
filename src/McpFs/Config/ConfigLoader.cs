using System.Text.Json;
using McpFs.Core.Limits;

namespace McpFs.Config;

public static class ConfigLoader
{
    public static McpFsConfig Load(string[] args)
    {
        var cli = ParseArgs(args);
        var configPath = ResolveConfigPath(cli.ConfigPath);

        McpFsConfig config;
        if (string.IsNullOrWhiteSpace(configPath) || !File.Exists(configPath))
        {
            config = new McpFsConfig();
        }
        else
        {
            config = TryLoadFromFile(configPath) ?? new McpFsConfig();
        }

        if (!string.IsNullOrWhiteSpace(cli.RootPath))
        {
            config = new McpFsConfig
            {
                WorkspaceRoot = cli.RootPath,
                FollowSymlinks = config.FollowSymlinks,
                SearchMaxResults = config.SearchMaxResults,
                SearchSnippetBytes = config.SearchSnippetBytes,
                SearchMaxFilesScanned = config.SearchMaxFilesScanned,
                SearchMaxFileSizeBytes = config.SearchMaxFileSizeBytes,
                SearchTimeoutMs = config.SearchTimeoutMs,
                OpenMaxBytes = config.OpenMaxBytes,
                OpenMaxLines = config.OpenMaxLines,
                PatchMaxBytes = config.PatchMaxBytes,
                PatchMaxEdits = config.PatchMaxEdits,
                PatchMaxFileSizeBytes = config.PatchMaxFileSizeBytes,
                ScanLimit = config.ScanLimit,
                ScanMaxDepth = config.ScanMaxDepth,
                LogLevel = config.LogLevel,
                LogFormat = config.LogFormat,
                Quiet = config.Quiet
            };
        }

        return Normalize(config);
    }

    private static McpFsConfig? TryLoadFromFile(string configPath)
    {
        try
        {
            var raw = File.ReadAllText(configPath);
            return JsonSerializer.Deserialize<McpFsConfig>(raw, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }

    private static string? ResolveConfigPath(string? cliConfigPath)
    {
        if (!string.IsNullOrWhiteSpace(cliConfigPath))
        {
            return cliConfigPath;
        }

        var env = Environment.GetEnvironmentVariable("MCP_FS_CONFIG");
        if (!string.IsNullOrWhiteSpace(env))
        {
            return env;
        }

        var preferred = Path.Combine(Directory.GetCurrentDirectory(), "mcp-fs.config.json");
        if (File.Exists(preferred))
        {
            return preferred;
        }

        var legacy = Path.Combine(Directory.GetCurrentDirectory(), "mcp-fs.json");
        return File.Exists(legacy) ? legacy : null;
    }

    private static McpFsConfig Normalize(McpFsConfig config)
    {
        var logLevel = string.IsNullOrWhiteSpace(config.LogLevel) ? "info" : config.LogLevel.Trim().ToLowerInvariant();
        if (config.Quiet)
        {
            logLevel = "error";
        }

        return new McpFsConfig
        {
            WorkspaceRoot = string.IsNullOrWhiteSpace(config.WorkspaceRoot) ? null : config.WorkspaceRoot,
            FollowSymlinks = config.FollowSymlinks,
            SearchMaxResults = FsLimits.ClampPositive(config.SearchMaxResults, 100, FsLimits.SearchHardCapResults),
            SearchSnippetBytes = FsLimits.ClampPositive(config.SearchSnippetBytes, 220, FsLimits.SearchHardCapSnippetBytes),
            SearchMaxFilesScanned = FsLimits.ClampPositive(config.SearchMaxFilesScanned, 5_000, FsLimits.SearchHardCapFilesScanned),
            SearchMaxFileSizeBytes = FsLimits.ClampPositive(config.SearchMaxFileSizeBytes, 2 * 1024 * 1024, FsLimits.SearchHardCapFileSizeBytes),
            SearchTimeoutMs = FsLimits.ClampPositive(config.SearchTimeoutMs, 5_000, FsLimits.SearchHardCapTimeoutMs),
            OpenMaxBytes = FsLimits.ClampPositive(config.OpenMaxBytes, 65_536, FsLimits.OpenHardCapBytes),
            OpenMaxLines = FsLimits.ClampPositive(config.OpenMaxLines, 200, FsLimits.OpenHardCapLines),
            PatchMaxBytes = FsLimits.ClampPositive(config.PatchMaxBytes, 256 * 1024, FsLimits.PatchHardCapBytes),
            PatchMaxEdits = FsLimits.ClampPositive(config.PatchMaxEdits, 50, FsLimits.PatchHardCapEdits),
            PatchMaxFileSizeBytes = FsLimits.ClampPositive(config.PatchMaxFileSizeBytes, 2 * 1024 * 1024, FsLimits.PatchHardCapFileSizeBytes),
            ScanLimit = FsLimits.ClampPositive(config.ScanLimit, 500, FsLimits.ScanHardCapLimit),
            ScanMaxDepth = FsLimits.ClampPositive(config.ScanMaxDepth, 16, FsLimits.ScanHardCapDepth),
            LogLevel = logLevel,
            LogFormat = string.IsNullOrWhiteSpace(config.LogFormat) ? "plain" : config.LogFormat.Trim().ToLowerInvariant(),
            Quiet = config.Quiet
        };
    }

    private static CliArguments ParseArgs(string[] args)
    {
        var cli = new CliArguments();

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (TrySplitArg(arg, "--root", out var root))
            {
                cli.RootPath = root;
                continue;
            }

            if (TrySplitArg(arg, "--config", out var config))
            {
                cli.ConfigPath = config;
                continue;
            }

            if (arg == "--root" && i + 1 < args.Length)
            {
                cli.RootPath = args[++i];
                continue;
            }

            if (arg == "--config" && i + 1 < args.Length)
            {
                cli.ConfigPath = args[++i];
            }
        }

        return cli;
    }

    private static bool TrySplitArg(string arg, string key, out string? value)
    {
        value = null;
        if (!arg.StartsWith(key + "=", StringComparison.Ordinal))
        {
            return false;
        }

        value = arg[(key.Length + 1)..];
        return true;
    }

    private sealed class CliArguments
    {
        public string? RootPath { get; set; }
        public string? ConfigPath { get; set; }
    }
}
