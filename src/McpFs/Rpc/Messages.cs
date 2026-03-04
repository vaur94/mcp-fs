using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace McpFs.Rpc;

public static class ErrorCodes
{
    public const string NotFound = "NOT_FOUND";
    public const string PermissionDenied = "PERMISSION_DENIED";
    public const string InvalidPath = "INVALID_PATH";
    public const string OutsideRoot = "OUTSIDE_ROOT";
    public const string InvalidRange = "INVALID_RANGE";
    public const string HashMismatch = "HASH_MISMATCH";
    public const string TooLarge = "TOO_LARGE";
    public const string RateLimited = "RATE_LIMITED";
    public const string InternalError = "INTERNAL_ERROR";
}

public sealed class JsonRpcRequest
{
    public string Jsonrpc { get; init; } = "2.0";
    public JsonElement? Id { get; init; }
    public string Method { get; init; } = string.Empty;

    [JsonPropertyName("params")]
    public JsonElement? Params { get; init; }
}

public sealed class JsonRpcResponse
{
    public string Jsonrpc { get; init; } = "2.0";
    public JsonElement? Id { get; init; }
    public JsonElement? Result { get; init; }
    public JsonRpcError? Error { get; init; }
}

public sealed class JsonRpcError
{
    public int Code { get; init; }
    public string Message { get; init; } = string.Empty;
    public JsonElement? Data { get; init; }
}

public sealed class ToolResponse
{
    public bool Ok { get; init; }
    public string? ErrorCode { get; init; }
    public string? Message { get; init; }
    public JsonElement? Data { get; init; }

    public static ToolResponse Success<T>(T data, JsonTypeInfo<T> typeInfo)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(data, typeInfo);
        using var doc = JsonDocument.Parse(bytes);

        return new ToolResponse
        {
            Ok = true,
            Data = doc.RootElement.Clone()
        };
    }

    public static ToolResponse Failure(string errorCode, string message, JsonElement? data = null)
    {
        return new ToolResponse
        {
            Ok = false,
            ErrorCode = errorCode,
            Message = message,
            Data = data
        };
    }

    public static ToolResponse Failure<T>(string errorCode, string message, T data, JsonTypeInfo<T> typeInfo)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(data, typeInfo);
        using var doc = JsonDocument.Parse(bytes);

        return new ToolResponse
        {
            Ok = false,
            ErrorCode = errorCode,
            Message = message,
            Data = doc.RootElement.Clone()
        };
    }
}

public sealed class InitializeParams
{
    public string? ProtocolVersion { get; init; }
    public JsonElement? Capabilities { get; init; }
    public ClientInfo? ClientInfo { get; init; }
}

public sealed class ClientInfo
{
    public string? Name { get; init; }
    public string? Version { get; init; }
}

public sealed class InitializeResult
{
    public string ProtocolVersion { get; init; } = "2024-11-05";
    public ServerCapabilities Capabilities { get; init; } = new();
    public ServerInfo ServerInfo { get; init; } = new();
}

public sealed class ServerCapabilities
{
    public ToolCapability Tools { get; init; } = new();
}

public sealed class ToolCapability
{
    public bool ListChanged { get; init; }
}

public sealed class ServerInfo
{
    public string Name { get; init; } = AppMetadata.Name;
    public string Version { get; init; } = AppMetadata.Version;
}

public sealed class ToolsListResult
{
    public IReadOnlyList<McpToolInfo> Tools { get; init; } = Array.Empty<McpToolInfo>();
}

public sealed class McpToolInfo
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public JsonElement InputSchema { get; init; }
}

public sealed class ToolsCallParams
{
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("arguments")]
    public JsonElement? Arguments { get; init; }
}

public sealed class ToolsCallResult
{
    public IReadOnlyList<McpContentItem> Content { get; init; } = Array.Empty<McpContentItem>();
    public ToolResponse StructuredContent { get; init; } = ToolResponse.Failure(ErrorCodes.InternalError, "uninitialized");
    public bool IsError { get; init; }
}

public sealed class McpContentItem
{
    public string Type { get; init; } = "text";
    public string Text { get; init; } = string.Empty;
}

public sealed class CapabilitiesRequest
{
}

public sealed class RootDetectRequest
{
}

public sealed class HealthRequest
{
}

public sealed class ScanRequest
{
    public string? Root { get; init; }
    public int? MaxDepth { get; init; }
    public IReadOnlyList<string>? IncludeGlobs { get; init; }
    public IReadOnlyList<string>? ExcludeGlobs { get; init; }
    public int? Limit { get; init; }
}

public sealed class SearchRequest
{
    public string Query { get; init; } = string.Empty;
    public string? Root { get; init; }
    public bool? Regex { get; init; }
    public bool? CaseSensitive { get; init; }
    public IReadOnlyList<string>? Glob { get; init; }
    public IReadOnlyList<string>? ExcludeGlob { get; init; }
    public int? MaxResults { get; init; }
    public int? SnippetBytes { get; init; }
    public int? MaxFilesScanned { get; init; }
    public int? MaxFileSizeBytes { get; init; }
    public int? TimeoutMs { get; init; }
}

public sealed class OpenRequest
{
    public string Path { get; init; } = string.Empty;
    public int? StartLine { get; init; }
    public int? EndLine { get; init; }
    public int? MaxBytes { get; init; }
}

public sealed class StatRequest
{
    public string Path { get; init; } = string.Empty;
}

public sealed class ReadDirRequest
{
    public string? Path { get; init; }
    public bool? IncludeFiles { get; init; }
    public bool? IncludeDirs { get; init; }
    public int? Limit { get; init; }
}

public sealed class PatchRequest
{
    public string Path { get; init; } = string.Empty;
    public string PreHash { get; init; } = string.Empty;
    public string? Mode { get; init; }
    public IReadOnlyList<PatchEdit> Edits { get; init; } = Array.Empty<PatchEdit>();
}

public sealed class PatchPreviewRequest
{
    public string Path { get; init; } = string.Empty;
    public string PreHash { get; init; } = string.Empty;
    public string? Mode { get; init; }
    public IReadOnlyList<PatchEdit> Edits { get; init; } = Array.Empty<PatchEdit>();
}

public sealed class PatchEdit
{
    public string Op { get; init; } = string.Empty;
    public PatchRange? Range { get; init; }
    public PatchPosition? At { get; init; }
    public string? Text { get; init; }

    public int? StartLine { get; init; }
    public int? StartCol { get; init; }
    public int? EndLine { get; init; }
    public int? EndCol { get; init; }
    public int? Line { get; init; }
    public int? Col { get; init; }
}

public sealed class PatchRange
{
    public int StartLine { get; init; }
    public int StartCol { get; init; }
    public int EndLine { get; init; }
    public int EndCol { get; init; }
}

public sealed class PatchPosition
{
    public int Line { get; init; }
    public int Col { get; init; }
}

public sealed class DefaultsData
{
    public int SearchMaxResults { get; init; }
    public int SearchSnippetBytes { get; init; }
    public int SearchMaxFilesScanned { get; init; }
    public int SearchMaxFileSizeBytes { get; init; }
    public int SearchTimeoutMs { get; init; }
    public int OpenMaxBytes { get; init; }
    public int OpenMaxLines { get; init; }
    public int PatchMaxBytes { get; init; }
    public int PatchMaxEdits { get; init; }
    public int PatchMaxFileSizeBytes { get; init; }
    public int ScanLimit { get; init; }
    public int ScanMaxDepth { get; init; }
    public bool FollowSymlinks { get; init; }
}

public sealed class LimitsData
{
    public int OpenHardCapBytes { get; init; }
    public int OpenHardCapLines { get; init; }
    public int SearchHardCapResults { get; init; }
    public int SearchHardCapSnippetBytes { get; init; }
    public int SearchHardCapFilesScanned { get; init; }
    public int SearchHardCapFileSizeBytes { get; init; }
    public int SearchHardCapTimeoutMs { get; init; }
    public int PatchHardCapBytes { get; init; }
    public int PatchHardCapEdits { get; init; }
    public int PatchHardCapFileSizeBytes { get; init; }
    public int ScanHardCapLimit { get; init; }
    public int ScanHardCapDepth { get; init; }
}

public sealed class ToolAvailabilityData
{
    public bool Ripgrep { get; init; }
}

public sealed class CapabilitiesData
{
    public string Os { get; init; } = string.Empty;
    public string Arch { get; init; } = string.Empty;
    public string PathSeparator { get; init; } = string.Empty;
    public string Version { get; init; } = AppMetadata.Version;
    public ToolAvailabilityData ToolAvailability { get; init; } = new();
    public DefaultsData Defaults { get; init; } = new();
    public LimitsData Limits { get; init; } = new();
    public IReadOnlyList<string> Features { get; init; } = Array.Empty<string>();
}

public sealed class RootDetectData
{
    public string Root { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
}

public sealed class ScanData
{
    public string Root { get; init; } = string.Empty;
    public IReadOnlyList<ScanItem> Entries { get; init; } = Array.Empty<ScanItem>();
    public bool Truncated { get; init; }
}

public sealed class ScanItem
{
    public string Path { get; init; } = string.Empty;
    public string Kind { get; init; } = string.Empty;
    public long? Size { get; init; }
    public DateTimeOffset? MtimeUtc { get; init; }
    public string? QuickHash8 { get; init; }
}

public sealed class SearchData
{
    public IReadOnlyList<SearchMatch> Results { get; init; } = Array.Empty<SearchMatch>();
    public bool Truncated { get; init; }
    public string Engine { get; init; } = string.Empty;
}

public sealed class SearchMatch
{
    public string Path { get; init; } = string.Empty;
    public int Line { get; init; }
    public int Col { get; init; }
    public string Snippet { get; init; } = string.Empty;
    public MatchRange Range { get; init; } = new();
    public string ContextHash { get; init; } = string.Empty;
}

public sealed class MatchRange
{
    public int StartLine { get; init; }
    public int StartCol { get; init; }
    public int EndLine { get; init; }
    public int EndCol { get; init; }
}

public sealed class OpenData
{
    public string Path { get; init; } = string.Empty;
    public int StartLine { get; init; }
    public int EndLine { get; init; }
    public string Text { get; init; } = string.Empty;
    public string ContextHash { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public bool Truncated { get; init; }
}

public sealed class RejectedEdit
{
    public int Index { get; init; }
    public string Reason { get; init; } = string.Empty;
}

public sealed class PatchData
{
    public string PostHash { get; init; } = string.Empty;
    public int AppliedEditsCount { get; init; }
    public int BytesChanged { get; init; }
    public int LineDelta { get; init; }
    public IReadOnlyList<RejectedEdit>? RejectedEdits { get; init; }
    public string? Summary { get; init; }
}

public sealed class PatchPreviewData
{
    public bool WouldApply { get; init; }
    public string PostHash { get; init; } = string.Empty;
    public DiffSummaryData DiffSummary { get; init; } = new();
    public int BytesChanged { get; init; }
    public int LineDelta { get; init; }
}

public sealed class DiffSummaryData
{
    public string Path { get; init; } = string.Empty;
    public int EditCount { get; init; }
    public int BytesChanged { get; init; }
    public int LineDelta { get; init; }
    public IReadOnlyList<string> EditSummaries { get; init; } = Array.Empty<string>();
}

public sealed class StatData
{
    public string Path { get; init; } = string.Empty;
    public string Kind { get; init; } = string.Empty;
    public long? Size { get; init; }
    public DateTimeOffset? MtimeUtc { get; init; }
    public string? ContextHash { get; init; }
    public string? QuickHash8 { get; init; }
    public bool IsSymlink { get; init; }
}

public sealed class ReadDirData
{
    public IReadOnlyList<ReadDirEntry> Entries { get; init; } = Array.Empty<ReadDirEntry>();
    public bool Truncated { get; init; }
}

public sealed class ReadDirEntry
{
    public string Name { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public string Kind { get; init; } = string.Empty;
    public long? Size { get; init; }
    public DateTimeOffset? MtimeUtc { get; init; }
    public bool IsSymlink { get; init; }
}

public sealed class HealthData
{
    public string Status { get; init; } = "ok";
    public string Version { get; init; } = AppMetadata.Version;
    public long UptimeMs { get; init; }
    public string Root { get; init; } = string.Empty;
    public bool FollowSymlinks { get; init; }
    public DefaultsData Limits { get; init; } = new();
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false)]
[JsonSerializable(typeof(JsonRpcRequest))]
[JsonSerializable(typeof(JsonRpcResponse))]
[JsonSerializable(typeof(JsonRpcError))]
[JsonSerializable(typeof(ToolResponse))]
[JsonSerializable(typeof(InitializeParams))]
[JsonSerializable(typeof(InitializeResult))]
[JsonSerializable(typeof(ToolsListResult))]
[JsonSerializable(typeof(ToolsCallParams))]
[JsonSerializable(typeof(ToolsCallResult))]
[JsonSerializable(typeof(CapabilitiesRequest))]
[JsonSerializable(typeof(RootDetectRequest))]
[JsonSerializable(typeof(HealthRequest))]
[JsonSerializable(typeof(ScanRequest))]
[JsonSerializable(typeof(SearchRequest))]
[JsonSerializable(typeof(OpenRequest))]
[JsonSerializable(typeof(PatchRequest))]
[JsonSerializable(typeof(PatchPreviewRequest))]
[JsonSerializable(typeof(StatRequest))]
[JsonSerializable(typeof(ReadDirRequest))]
[JsonSerializable(typeof(CapabilitiesData))]
[JsonSerializable(typeof(RootDetectData))]
[JsonSerializable(typeof(HealthData))]
[JsonSerializable(typeof(ScanData))]
[JsonSerializable(typeof(SearchData))]
[JsonSerializable(typeof(OpenData))]
[JsonSerializable(typeof(PatchData))]
[JsonSerializable(typeof(PatchPreviewData))]
[JsonSerializable(typeof(StatData))]
[JsonSerializable(typeof(ReadDirData))]
[JsonSerializable(typeof(McpToolInfo))]
internal partial class McpJsonSerializerContext : JsonSerializerContext
{
}
