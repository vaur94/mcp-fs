using McpFs.Config;
using McpFs.Core;
using McpFs.Logging;

namespace McpFs.Tests;

internal static class TestHelpers
{
    public static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "mcpfs-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    public static Workspace CreateWorkspace(string rootPath, bool followSymlinks = false)
    {
        var config = new McpFsConfig
        {
            WorkspaceRoot = rootPath,
            FollowSymlinks = followSymlinks,
            SearchMaxResults = 100,
            SearchSnippetBytes = 220,
            OpenMaxBytes = 65_536,
            OpenMaxLines = 200,
            ScanLimit = 500,
            ScanMaxDepth = 16,
            LogLevel = "error"
        };

        var logger = new StderrLogger("error");
        return Workspace.Create(config, logger);
    }

    public static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}
