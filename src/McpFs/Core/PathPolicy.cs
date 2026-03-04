using McpFs.Rpc;

namespace McpFs.Core;

public sealed class PathPolicy
{
    private readonly string _rootPath;
    private readonly string _rootWithSeparator;
    private readonly StringComparison _pathComparison;

    public PathPolicy(string rootPath)
    {
        _rootPath = Path.GetFullPath(rootPath);
        _rootWithSeparator = _rootPath.EndsWith(Path.DirectorySeparatorChar)
            ? _rootPath
            : _rootPath + Path.DirectorySeparatorChar;
        _pathComparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
    }

    public string RootPath => _rootPath;

    public bool TryResolvePath(string inputPath, out string fullPath, out string relativePath, out ToolResponse? error)
    {
        fullPath = string.Empty;
        relativePath = string.Empty;
        error = null;

        if (string.IsNullOrWhiteSpace(inputPath))
        {
            error = ToolResponse.Failure(ErrorCodes.InvalidPath, "Path is required.");
            return false;
        }

        var trimmed = inputPath.Trim();
        if (trimmed.Contains('\0'))
        {
            error = ToolResponse.Failure(ErrorCodes.InvalidPath, "Path contains null character.");
            return false;
        }

        if (Path.IsPathRooted(trimmed))
        {
            error = ToolResponse.Failure(ErrorCodes.InvalidPath, "Absolute paths are not allowed.");
            return false;
        }

        try
        {
            var combined = Path.Combine(_rootPath, trimmed.Replace('/', Path.DirectorySeparatorChar));
            var normalized = Path.GetFullPath(combined);
            if (!IsWithinRoot(normalized))
            {
                error = ToolResponse.Failure(ErrorCodes.OutsideRoot, "Path escapes workspace root.");
                return false;
            }

            fullPath = normalized;
            relativePath = NormalizeRelative(Path.GetRelativePath(_rootPath, normalized));
            return true;
        }
        catch
        {
            error = ToolResponse.Failure(ErrorCodes.InvalidPath, "Path is invalid.");
            return false;
        }
    }

    public bool TryResolveDirectory(string? inputPath, out string fullPath, out string relativePath, out ToolResponse? error)
    {
        if (string.IsNullOrWhiteSpace(inputPath))
        {
            fullPath = _rootPath;
            relativePath = ".";
            error = null;
            return true;
        }

        if (!TryResolvePath(inputPath, out fullPath, out relativePath, out error))
        {
            return false;
        }

        if (!Directory.Exists(fullPath))
        {
            error = ToolResponse.Failure(ErrorCodes.NotFound, $"Directory not found: {inputPath}");
            return false;
        }

        return true;
    }

    public bool IsWithinRoot(string absolutePath)
    {
        var normalized = Path.GetFullPath(absolutePath);
        if (string.Equals(normalized, _rootPath, _pathComparison))
        {
            return true;
        }

        return normalized.StartsWith(_rootWithSeparator, _pathComparison);
    }

    public string ToRelativePath(string absolutePath)
    {
        var relative = Path.GetRelativePath(_rootPath, absolutePath);
        return NormalizeRelative(relative);
    }

    private static string NormalizeRelative(string path)
    {
        var normalized = path.Replace(Path.DirectorySeparatorChar, '/');
        if (Path.DirectorySeparatorChar != Path.AltDirectorySeparatorChar)
        {
            normalized = normalized.Replace(Path.AltDirectorySeparatorChar, '/');
        }

        return normalized;
    }
}
