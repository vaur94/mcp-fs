using McpFs.Config;
using McpFs.Core;
using McpFs.Logging;
using McpFs.Rpc;

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
            SearchMaxFilesScanned = 5_000,
            SearchMaxFileSizeBytes = 2 * 1024 * 1024,
            SearchTimeoutMs = 5_000,
            OpenMaxBytes = 65_536,
            OpenMaxLines = 200,
            PatchMaxBytes = 256 * 1024,
            PatchMaxEdits = 50,
            PatchMaxFileSizeBytes = 2 * 1024 * 1024,
            ScanLimit = 500,
            ScanMaxDepth = 16,
            LogLevel = "error",
            LogFormat = "plain"
        };

        var logger = new StderrLogger("error");
        return Workspace.Create(config, logger);
    }

    public static T DeserializeData<T>(ToolResponse response)
    {
        if (!response.Data.HasValue)
        {
            throw new InvalidOperationException("Tool response has no data.");
        }

        return System.Text.Json.JsonSerializer.Deserialize<T>(
            response.Data.Value.GetRawText(),
            new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })!;
    }

    public static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}
