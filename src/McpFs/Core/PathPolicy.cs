using McpFs.Rpc;

namespace McpFs.Core;

public sealed class PathPolicy
{
    private readonly string _rootPath;
    private readonly string _rootWithSeparator;
    private readonly StringComparison _pathComparison;
    private readonly bool _followSymlinks;

    public PathPolicy(string rootPath, bool followSymlinks)
    {
        _rootPath = Path.GetFullPath(rootPath);
        _rootWithSeparator = _rootPath.EndsWith(Path.DirectorySeparatorChar)
            ? _rootPath
            : _rootPath + Path.DirectorySeparatorChar;
        _pathComparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        _followSymlinks = followSymlinks;
    }

    public string RootPath => _rootPath;
    public bool FollowSymlinks => _followSymlinks;

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

        if (IsAbsoluteLike(trimmed))
        {
            error = ToolResponse.Failure(ErrorCodes.InvalidPath, "Absolute paths are not allowed.");
            return false;
        }

        try
        {
            var normalizedInput = trimmed
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);
            var combined = Path.Combine(_rootPath, normalizedInput);
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
            error = ToolResponse.Failure(ErrorCodes.NotFound, "Directory not found.");
            return false;
        }

        if (!TryValidateSymlinkPolicy(fullPath, out error))
        {
            return false;
        }

        return true;
    }

    public bool TryResolveExistingPath(string inputPath, out string fullPath, out string relativePath, out bool isSymlink, out ToolResponse? error)
    {
        isSymlink = false;

        if (!TryResolvePath(inputPath, out fullPath, out relativePath, out error))
        {
            return false;
        }

        if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
        {
            error = ToolResponse.Failure(ErrorCodes.NotFound, "Path not found.");
            return false;
        }

        isSymlink = IsSymlink(fullPath);
        if (!TryValidateSymlinkPolicy(fullPath, out error))
        {
            return false;
        }

        return true;
    }

    public bool TryResolveExistingFile(string inputPath, out string fullPath, out string relativePath, out bool isSymlink, out ToolResponse? error)
    {
        if (!TryResolveExistingPath(inputPath, out fullPath, out relativePath, out isSymlink, out error))
        {
            return false;
        }

        if (!File.Exists(fullPath))
        {
            error = ToolResponse.Failure(ErrorCodes.NotFound, "File not found.");
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

    public bool IsSymlink(string path)
    {
        try
        {
            var attributes = File.GetAttributes(path);
            if (attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                return true;
            }

            FileSystemInfo info = Directory.Exists(path)
                ? new DirectoryInfo(path)
                : new FileInfo(path);

            return info.LinkTarget is not null;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsAbsoluteLike(string path)
    {
        if (Path.IsPathRooted(path))
        {
            return true;
        }

        if (path.StartsWith(@"\\", StringComparison.Ordinal) || path.StartsWith("//", StringComparison.Ordinal))
        {
            return true;
        }

        return path.Length >= 2 && char.IsLetter(path[0]) && path[1] == ':';
    }

    private bool TryValidateSymlinkPolicy(string fullPath, out ToolResponse? error)
    {
        error = null;

        var target = Path.GetFullPath(fullPath);
        if (!target.StartsWith(_rootWithSeparator, _pathComparison) && !string.Equals(target, _rootPath, _pathComparison))
        {
            error = ToolResponse.Failure(ErrorCodes.OutsideRoot, "Path escapes workspace root.");
            return false;
        }

        var current = _rootPath;
        var relative = Path.GetRelativePath(_rootPath, target);
        if (relative == ".")
        {
            return true;
        }

        var segments = relative.Split(
            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
            StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in segments)
        {
            current = Path.Combine(current, segment);
            if (!File.Exists(current) && !Directory.Exists(current))
            {
                continue;
            }

            if (!IsSymlink(current))
            {
                continue;
            }

            if (!_followSymlinks)
            {
                error = ToolResponse.Failure(ErrorCodes.PermissionDenied, "Symlink access is disabled.");
                return false;
            }

            var resolved = ResolveFully(current);
            if (resolved is null)
            {
                error = ToolResponse.Failure(ErrorCodes.PermissionDenied, "Symlink target could not be resolved.");
                return false;
            }

            if (!IsWithinRoot(resolved))
            {
                error = ToolResponse.Failure(ErrorCodes.OutsideRoot, "Symlink target escapes workspace root.");
                return false;
            }
        }

        return true;
    }

    private static string? ResolveFully(string path)
    {
        try
        {
            FileSystemInfo info = Directory.Exists(path)
                ? new DirectoryInfo(path)
                : new FileInfo(path);

            var resolved = info.ResolveLinkTarget(returnFinalTarget: true);
            if (resolved is null)
            {
                return path;
            }

            return Path.GetFullPath(resolved.FullName);
        }
        catch
        {
            return null;
        }
    }

    private static string NormalizeRelative(string path)
    {
        var normalized = path.Replace(Path.DirectorySeparatorChar, '/');
        if (Path.DirectorySeparatorChar != Path.AltDirectorySeparatorChar)
        {
            normalized = normalized.Replace(Path.AltDirectorySeparatorChar, '/');
        }

        if (normalized == ".")
        {
            return ".";
        }

        if (normalized.StartsWith("./", StringComparison.Ordinal))
        {
            normalized = normalized[2..];
        }

        if (normalized.StartsWith("/", StringComparison.Ordinal))
        {
            normalized = normalized[1..];
        }

        return normalized;
    }
}
