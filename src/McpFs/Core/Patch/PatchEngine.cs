using System.Text;
using McpFs.Core.Hashing;
using McpFs.Rpc;

namespace McpFs.Core.Patch;

public sealed class PatchEngine
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];

    private readonly ContentHasher _hasher;

    public PatchEngine(ContentHasher hasher)
    {
        _hasher = hasher;
    }

    public async Task<PatchComputationResult> ComputeAsync(
        string fullPath,
        string relativePath,
        string preHash,
        IReadOnlyList<PatchEdit> edits,
        PatchRuntimeOptions options,
        CancellationToken cancellationToken)
    {
        if (edits.Count == 0)
        {
            return PatchComputationResult.Failure(ErrorCodes.InvalidRange, "edits cannot be empty");
        }

        if (edits.Count > options.MaxEdits)
        {
            return PatchComputationResult.Failure(ErrorCodes.TooLarge, "edit count exceeds limit");
        }

        byte[] originalBytes;
        try
        {
            originalBytes = await File.ReadAllBytesAsync(fullPath, cancellationToken).ConfigureAwait(false);
        }
        catch (UnauthorizedAccessException)
        {
            return PatchComputationResult.Failure(ErrorCodes.PermissionDenied, "Permission denied.");
        }

        if (originalBytes.LongLength > options.MaxFileSizeBytes)
        {
            return PatchComputationResult.Failure(ErrorCodes.TooLarge, "file size exceeds patch limit");
        }

        var currentHash = _hasher.ComputeContextHash(originalBytes);
        if (!string.Equals(currentHash, preHash, StringComparison.OrdinalIgnoreCase))
        {
            return PatchComputationResult.Failure(ErrorCodes.HashMismatch, "preHash mismatch.");
        }

        var totalPatchBytes = EstimatePatchBytes(edits);
        if (totalPatchBytes > options.MaxPatchBytes)
        {
            return PatchComputationResult.Failure(ErrorCodes.TooLarge, "patch payload exceeds limit");
        }

        var hasBom = HasUtf8Bom(originalBytes);
        var textBytes = hasBom ? originalBytes.AsSpan(Utf8Bom.Length).ToArray() : originalBytes;

        string sourceText;
        try
        {
            sourceText = Utf8NoBom.GetString(textBytes);
        }
        catch (DecoderFallbackException)
        {
            return PatchComputationResult.Failure(ErrorCodes.InvalidRange, "file must be UTF-8 text");
        }

        var eol = DetectEol(sourceText);
        var sourceLf = NormalizeToLf(sourceText);

        if (!TryBuildOperations(sourceLf, edits, out var operations, out var validationError))
        {
            return PatchComputationResult.Failure(ErrorCodes.InvalidRange, validationError ?? "invalid edit range");
        }

        var patchedLf = ApplyOperations(sourceLf, operations);
        var patchedText = eol == "\r\n" ? patchedLf.Replace("\n", "\r\n", StringComparison.Ordinal) : patchedLf;

        var patchedTextBytes = Utf8NoBom.GetBytes(patchedText);
        var finalBytes = hasBom ? [.. Utf8Bom, .. patchedTextBytes] : patchedTextBytes;

        var postHash = _hasher.ComputeContextHash(finalBytes);
        var bytesChanged = ComputeBytesChanged(originalBytes, finalBytes);
        var lineDelta = CountLines(patchedLf) - CountLines(sourceLf);

        var diffSummary = new DiffSummaryData
        {
            Path = relativePath,
            EditCount = operations.Count,
            BytesChanged = bytesChanged,
            LineDelta = lineDelta,
            EditSummaries = BuildEditSummaries(operations)
        };

        return PatchComputationResult.Success(
            finalBytes,
            postHash,
            operations.Count,
            bytesChanged,
            lineDelta,
            diffSummary);
    }

    private static int EstimatePatchBytes(IReadOnlyList<PatchEdit> edits)
    {
        var total = 0;
        foreach (var edit in edits)
        {
            total += 64;
            if (!string.IsNullOrEmpty(edit.Text))
            {
                total += Utf8NoBom.GetByteCount(edit.Text);
            }
        }

        return total;
    }

    private static bool HasUtf8Bom(ReadOnlySpan<byte> bytes)
    {
        return bytes.Length >= Utf8Bom.Length &&
               bytes[0] == Utf8Bom[0] &&
               bytes[1] == Utf8Bom[1] &&
               bytes[2] == Utf8Bom[2];
    }

    private static string DetectEol(string text)
    {
        return text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
    }

    private static string NormalizeToLf(string text)
    {
        return text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
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
                            if (!TryGetRange(edit, out var range, out error))
                            {
                                error = $"edit[{i}] {error}";
                                return false;
                            }

                            if (edit.Text is null)
                            {
                                error = $"edit[{i}] replace requires text";
                                return false;
                            }

                            var start = ToIndex(source, range!.StartLine, range.StartCol);
                            var end = ToIndex(source, range.EndLine, range.EndCol);
                            if (end < start)
                            {
                                error = $"edit[{i}] range end is before start";
                                return false;
                            }

                            operations.Add(new TextEditOperation(i, op, start, end, NormalizeToLf(edit.Text)));
                            break;
                        }
                    case "insert":
                        {
                            if (!TryGetPosition(edit, out var position, out error))
                            {
                                error = $"edit[{i}] {error}";
                                return false;
                            }

                            if (edit.Text is null)
                            {
                                error = $"edit[{i}] insert requires text";
                                return false;
                            }

                            var at = ToIndex(source, position!.Line, position.Col);
                            operations.Add(new TextEditOperation(i, op, at, at, NormalizeToLf(edit.Text)));
                            break;
                        }
                    case "delete":
                        {
                            if (!TryGetRange(edit, out var range, out error))
                            {
                                error = $"edit[{i}] {error}";
                                return false;
                            }

                            var start = ToIndex(source, range!.StartLine, range.StartCol);
                            var end = ToIndex(source, range.EndLine, range.EndCol);
                            if (end < start)
                            {
                                error = $"edit[{i}] range end is before start";
                                return false;
                            }

                            operations.Add(new TextEditOperation(i, op, start, end, string.Empty));
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

    private static bool TryGetRange(PatchEdit edit, out PatchRange? range, out string? error)
    {
        range = null;
        error = null;

        if (edit.Range is not null)
        {
            range = edit.Range;
            return true;
        }

        if (edit.StartLine.HasValue && edit.StartCol.HasValue && edit.EndLine.HasValue && edit.EndCol.HasValue)
        {
            range = new PatchRange
            {
                StartLine = edit.StartLine.Value,
                StartCol = edit.StartCol.Value,
                EndLine = edit.EndLine.Value,
                EndCol = edit.EndCol.Value
            };
            return true;
        }

        error = "range is required";
        return false;
    }

    private static bool TryGetPosition(PatchEdit edit, out PatchPosition? at, out string? error)
    {
        at = null;
        error = null;

        if (edit.At is not null)
        {
            at = edit.At;
            return true;
        }

        if (edit.Line.HasValue && edit.Col.HasValue)
        {
            at = new PatchPosition
            {
                Line = edit.Line.Value,
                Col = edit.Col.Value
            };
            return true;
        }

        error = "position is required";
        return false;
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

    private static int ComputeBytesChanged(ReadOnlySpan<byte> before, ReadOnlySpan<byte> after)
    {
        var min = Math.Min(before.Length, after.Length);
        var changed = Math.Abs(before.Length - after.Length);

        for (var i = 0; i < min; i++)
        {
            if (before[i] != after[i])
            {
                changed++;
            }
        }

        return changed;
    }

    private static int CountLines(string text)
    {
        if (text.Length == 0)
        {
            return 0;
        }

        var lines = 1;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                lines++;
            }
        }

        return lines;
    }

    private static IReadOnlyList<string> BuildEditSummaries(IReadOnlyList<TextEditOperation> operations)
    {
        var summaries = new List<string>(Math.Min(operations.Count, 5));

        foreach (var operation in operations.Take(5))
        {
            summaries.Add($"{operation.Operation}:{operation.Start}-{operation.End}");
        }

        return summaries;
    }

    private sealed record TextEditOperation(int Index, string Operation, int Start, int End, string Replacement);
}

public sealed class PatchRuntimeOptions
{
    public int MaxPatchBytes { get; init; }
    public int MaxEdits { get; init; }
    public int MaxFileSizeBytes { get; init; }
}

public sealed class PatchComputationResult
{
    private PatchComputationResult()
    {
    }

    public ToolResponse? Error { get; private init; }
    public byte[] NewBytes { get; private init; } = Array.Empty<byte>();
    public string PostHash { get; private init; } = string.Empty;
    public int AppliedEditsCount { get; private init; }
    public int BytesChanged { get; private init; }
    public int LineDelta { get; private init; }
    public DiffSummaryData DiffSummary { get; private init; } = new();

    public static PatchComputationResult Failure(string errorCode, string message)
    {
        return new PatchComputationResult
        {
            Error = ToolResponse.Failure(errorCode, message)
        };
    }

    public static PatchComputationResult Success(
        byte[] newBytes,
        string postHash,
        int appliedEditsCount,
        int bytesChanged,
        int lineDelta,
        DiffSummaryData diffSummary)
    {
        return new PatchComputationResult
        {
            NewBytes = newBytes,
            PostHash = postHash,
            AppliedEditsCount = appliedEditsCount,
            BytesChanged = bytesChanged,
            LineDelta = lineDelta,
            DiffSummary = diffSummary
        };
    }
}
