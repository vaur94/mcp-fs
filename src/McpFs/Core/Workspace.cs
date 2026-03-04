using McpFs.Config;
using McpFs.Core.Ignore;
using McpFs.Logging;

namespace McpFs.Core;

public sealed class Workspace
{
    private Workspace(
        string rootPath,
        string detectionReason,
        McpFsConfig config,
        PathPolicy pathPolicy,
        IgnoreMatcher ignoreMatcher,
        StderrLogger logger)
    {
        RootPath = rootPath;
        DetectionReason = detectionReason;
        Config = config;
        PathPolicy = pathPolicy;
        IgnoreMatcher = ignoreMatcher;
        Logger = logger;
    }

    public string RootPath { get; }
    public string DetectionReason { get; }
    public McpFsConfig Config { get; }
    public PathPolicy PathPolicy { get; }
    public IgnoreMatcher IgnoreMatcher { get; }
    public StderrLogger Logger { get; }

    public bool FollowSymlinks => Config.FollowSymlinks;

    public static Workspace Create(McpFsConfig config, StderrLogger logger)
    {
        var detection = DetectRoot(config.WorkspaceRoot);
        var pathPolicy = new PathPolicy(detection.RootPath);
        var matcher = IgnoreMatcher.Load(detection.RootPath);

        return new Workspace(
            detection.RootPath,
            detection.Reason,
            config,
            pathPolicy,
            matcher,
            logger);
    }

    public static RootDetectionResult DetectRoot(string? configRootOverride)
    {
        static RootDetectionResult Build(string path, string reason)
            => new(Path.GetFullPath(path), reason);

        var envRoot = Environment.GetEnvironmentVariable("MCP_FS_ROOT");
        if (!string.IsNullOrWhiteSpace(envRoot) && Directory.Exists(envRoot))
        {
            return Build(envRoot, "env:MCP_FS_ROOT");
        }

        if (!string.IsNullOrWhiteSpace(configRootOverride) && Directory.Exists(configRootOverride))
        {
            return Build(configRootOverride, "config:workspaceRoot");
        }

        var cwd = Directory.GetCurrentDirectory();

        var gitRoot = FindUpward(cwd, directory => Directory.Exists(Path.Combine(directory, ".git")));
        if (gitRoot is not null)
        {
            return Build(gitRoot, "upward:.git");
        }

        var slnRoot = FindUpward(cwd, directory =>
            File.Exists(Path.Combine(directory, "global.json")) ||
            Directory.EnumerateFiles(directory, "*.sln", SearchOption.TopDirectoryOnly).Any());
        if (slnRoot is not null)
        {
            return Build(slnRoot, "upward:sln-or-global-json");
        }

        var jsRoot = FindUpward(cwd, directory =>
            File.Exists(Path.Combine(directory, "package.json")) ||
            File.Exists(Path.Combine(directory, "pnpm-workspace.yaml")));
        if (jsRoot is not null)
        {
            return Build(jsRoot, "upward:package-json-or-pnpm-workspace");
        }

        var pyRoot = FindUpward(cwd, directory => File.Exists(Path.Combine(directory, "pyproject.toml")));
        if (pyRoot is not null)
        {
            return Build(pyRoot, "upward:pyproject-toml");
        }

        return Build(cwd, "fallback:cwd");
    }

    public bool ShouldSkipSymlink(FileSystemInfo info)
    {
        if (FollowSymlinks)
        {
            return false;
        }

        return info.LinkTarget is not null || info.Attributes.HasFlag(FileAttributes.ReparsePoint);
    }

    private static string? FindUpward(string startDirectory, Func<string, bool> predicate)
    {
        var current = new DirectoryInfo(Path.GetFullPath(startDirectory));

        while (current is not null)
        {
            if (predicate(current.FullName))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }
}

public sealed record RootDetectionResult(string RootPath, string Reason);
