using System.Text;
using McpFs.Core;
using McpFs.Core.Hashing;
using McpFs.Core.IO;
using McpFs.Logging;
using McpFs.Rpc;

namespace McpFs.Tools;

public sealed class PatchTool
{
    private readonly Workspace _workspace;
    private readonly ContentHasher _hasher;
    private readonly AtomicWriter _atomicWriter;
    private readonly StderrLogger _logger;

    public PatchTool(
        Workspace workspace,
        ContentHasher hasher,
        AtomicWriter atomicWriter,
        StderrLogger logger)
    {
        _workspace = workspace;
        _hasher = hasher;
        _atomicWriter = atomicWriter;
        _logger = logger;
    }

    public async Task<ToolResponse> ExecuteAsync(PatchRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Path))
        {
            return ToolResponse.Failure(ErrorCodes.InvalidPath, "path is required");
        }

        if (string.IsNullOrWhiteSpace(request.PreHash))
        {
            return ToolResponse.Failure(ErrorCodes.InvalidRange, "preHash is required");
        }

        if (!_workspace.PathPolicy.TryResolvePath(request.Path, out var fullPath, out var relativePath, out var pathError))
        {
            return pathError!;
        }

        if (!File.Exists(fullPath))
        {
            return ToolResponse.Failure(ErrorCodes.NotFound, $"File not found: {request.Path}");
        }

        if (request.Edits is null || request.Edits.Count == 0)
        {
            return ToolResponse.Failure(ErrorCodes.InvalidRange, "edits cannot be empty");
        }

        var mode = request.Mode.Trim().ToLowerInvariant();
        if (mode is not ("strict" or "best_effort"))
        {
            return ToolResponse.Failure(ErrorCodes.InvalidRange, "mode must be strict or best_effort");
        }

        string originalText;
        try
        {
            originalText = await File.ReadAllTextAsync(fullPath, cancellationToken).ConfigureAwait(false);
        }
        catch (UnauthorizedAccessException)
        {
            return ToolResponse.Failure(ErrorCodes.PermissionDenied, $"Permission denied: {request.Path}");
        }

        var currentHash = await _hasher.ComputeContextHashAsync(fullPath, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(currentHash, request.PreHash, StringComparison.OrdinalIgnoreCase))
        {
            var message = mode == "best_effort"
                ? "preHash mismatch. best_effort mode is declared in schema but intentionally disabled in v0.1.0."
                : "preHash mismatch.";
            return ToolResponse.Failure(ErrorCodes.HashMismatch, message);
        }

        if (!TryBuildOperations(originalText, request.Edits, out var operations, out var validationError))
        {
            return ToolResponse.Failure(ErrorCodes.InvalidRange, validationError!);
        }

        var patched = ApplyOperations(originalText, operations);

        try
        {
            await _atomicWriter.WriteTextAtomicAsync(fullPath, patched, cancellationToken).ConfigureAwait(false);
        }
        catch (UnauthorizedAccessException)
        {
            return ToolResponse.Failure(ErrorCodes.PermissionDenied, $"Permission denied: {request.Path}");
        }
        catch (Exception ex)
        {
            _logger.Error($"patch write failed path={relativePath}: {ex.Message}");
            return ToolResponse.Failure(ErrorCodes.InternalError, "atomic write failed");
        }

        var postHash = _hasher.ComputeContextHash(patched);
        var data = new PatchData
        {
            PostHash = postHash,
            AppliedEditsCount = operations.Count,
            Summary = $"Applied {operations.Count} edit(s) to {relativePath}."
        };

        return ToolResponse.Success(data, McpJsonSerializerContext.Default.PatchData);
    }

    private static bool TryBuildOperations(
        string source,
        IReadOnlyList<PatchEdit> edits,
        out List<TextEditOperation> operations,
        out string? error)
    {
        operations = new List<TextEditOperation>(edits.Count);
        error = null;

        for (var i = 0; i < edits.Count; i++)
        {
            var edit = edits[i];
            var op = edit.Op.Trim().ToLowerInvariant();

            try
            {
                switch (op)
                {
                    case "replace":
                    {
                        if (edit.Range is null)
                        {
                            error = $"edit[{i}] replace requires range";
                            return false;
                        }

                        if (edit.Text is null)
                        {
                            error = $"edit[{i}] replace requires text";
                            return false;
                        }

                        var start = ToIndex(source, edit.Range.StartLine, edit.Range.StartCol);
                        var end = ToIndex(source, edit.Range.EndLine, edit.Range.EndCol);
                        if (end < start)
                        {
                            error = $"edit[{i}] range end is before start";
                            return false;
                        }

                        operations.Add(new TextEditOperation(i, start, end, edit.Text));
                        break;
                    }
                    case "insert":
                    {
                        if (edit.At is null)
                        {
                            error = $"edit[{i}] insert requires at";
                            return false;
                        }

                        if (edit.Text is null)
                        {
                            error = $"edit[{i}] insert requires text";
                            return false;
                        }

                        var at = ToIndex(source, edit.At.Line, edit.At.Col);
                        operations.Add(new TextEditOperation(i, at, at, edit.Text));
                        break;
                    }
                    case "delete":
                    {
                        if (edit.Range is null)
                        {
                            error = $"edit[{i}] delete requires range";
                            return false;
                        }

                        var start = ToIndex(source, edit.Range.StartLine, edit.Range.StartCol);
                        var end = ToIndex(source, edit.Range.EndLine, edit.Range.EndCol);
                        if (end < start)
                        {
                            error = $"edit[{i}] range end is before start";
                            return false;
                        }

                        operations.Add(new TextEditOperation(i, start, end, string.Empty));
                        break;
                    }
                    default:
                        error = $"edit[{i}] unknown op '{edit.Op}'";
                        return false;
                }
            }
            catch (ArgumentOutOfRangeException ex)
            {
                error = $"edit[{i}] invalid position: {ex.Message}";
                return false;
            }
        }

        var ordered = operations
            .OrderBy(op => op.Start)
            .ThenBy(op => op.End)
            .ThenBy(op => op.Index)
            .ToList();

        for (var i = 1; i < ordered.Count; i++)
        {
            if (ordered[i].Start < ordered[i - 1].End)
            {
                error = "edit ranges overlap";
                return false;
            }
        }

        operations = ordered;
        return true;
    }

    private static string ApplyOperations(string source, List<TextEditOperation> operations)
    {
        if (operations.Count == 0)
        {
            return source;
        }

        var builder = new StringBuilder(source);
        foreach (var op in operations.OrderByDescending(x => x.Start).ThenByDescending(x => x.End).ThenByDescending(x => x.Index))
        {
            builder.Remove(op.Start, op.End - op.Start);
            if (op.Replacement.Length > 0)
            {
                builder.Insert(op.Start, op.Replacement);
            }
        }

        return builder.ToString();
    }

    private static int ToIndex(string text, int line, int col)
    {
        if (line <= 0 || col <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(line), "line and col must be >= 1");
        }

        var currentLine = 1;
        var index = 0;

        while (currentLine < line)
        {
            var nextNewline = text.IndexOf('\n', index);
            if (nextNewline < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(line), "line exceeds document length");
            }

            index = nextNewline + 1;
            currentLine++;
        }

        var lineEnd = text.IndexOf('\n', index);
        if (lineEnd < 0)
        {
            lineEnd = text.Length;
        }

        var lineLength = lineEnd - index;
        var maxCol = lineLength + 1;
        if (col > maxCol)
        {
            throw new ArgumentOutOfRangeException(nameof(col), "column exceeds line length");
        }

        return index + col - 1;
    }

    private sealed record TextEditOperation(int Index, int Start, int End, string Replacement);
}
