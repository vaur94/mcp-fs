namespace McpFs.Core.Limits;

public static class FsLimits
{
    public const int OpenHardCapBytes = 131_072;
    public const int OpenHardCapLines = 1_000;

    public const int SearchHardCapResults = 500;
    public const int SearchHardCapSnippetBytes = 2_000;
    public const int SearchHardCapFilesScanned = 20_000;
    public const int SearchHardCapFileSizeBytes = 16 * 1024 * 1024;
    public const int SearchHardCapTimeoutMs = 15_000;

    public const int PatchHardCapBytes = 1_048_576;
    public const int PatchHardCapEdits = 200;
    public const int PatchHardCapFileSizeBytes = 16 * 1024 * 1024;

    public const int ScanHardCapLimit = 5_000;
    public const int ScanHardCapDepth = 64;

    public static int ClampPositive(int value, int fallback, int hardCap)
    {
        var normalized = value > 0 ? value : fallback;
        return Math.Min(normalized, hardCap);
    }
}
