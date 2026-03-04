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

    public static ToolResponse Failure(string errorCode, string message)
    {
        return new ToolResponse
        {
            Ok = false,
            ErrorCode = errorCode,
            Message = message
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
    public string Name { get; init; } = "mcp-fs";
    public string Version { get; init; } = "0.1.0";
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
}

public sealed class OpenRequest
{
    public string Path { get; init; } = string.Empty;
    public int? StartLine { get; init; }
    public int? EndLine { get; init; }
    public int? MaxBytes { get; init; }
}

public sealed class PatchRequest
{
    public string Path { get; init; } = string.Empty;
    public string PreHash { get; init; } = string.Empty;
    public string Mode { get; init; } = "strict";
    public IReadOnlyList<PatchEdit> Edits { get; init; } = Array.Empty<PatchEdit>();
}

public sealed class PatchEdit
{
    public string Op { get; init; } = string.Empty;
    public PatchRange? Range { get; init; }
    public PatchPosition? At { get; init; }
    public string? Text { get; init; }
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
    public int OpenMaxBytes { get; init; }
    public int ScanLimit { get; init; }
    public bool FollowSymlinks { get; init; }
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
    public string Version { get; init; } = "0.1.0";
    public ToolAvailabilityData ToolAvailability { get; init; } = new();
    public DefaultsData Defaults { get; init; } = new();
}

public sealed class RootDetectData
{
    public string RootPath { get; init; } = string.Empty;
    public string DetectionReason { get; init; } = string.Empty;
}

public sealed class ScanData
{
    public string Root { get; init; } = string.Empty;
    public IReadOnlyList<ScanItem> Items { get; init; } = Array.Empty<ScanItem>();
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
    public IReadOnlyList<SearchMatch> Matches { get; init; } = Array.Empty<SearchMatch>();
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
    public IReadOnlyList<RejectedEdit>? RejectedEdits { get; init; }
    public string? Summary { get; init; }
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
[JsonSerializable(typeof(ScanRequest))]
[JsonSerializable(typeof(SearchRequest))]
[JsonSerializable(typeof(OpenRequest))]
[JsonSerializable(typeof(PatchRequest))]
[JsonSerializable(typeof(CapabilitiesData))]
[JsonSerializable(typeof(RootDetectData))]
[JsonSerializable(typeof(ScanData))]
[JsonSerializable(typeof(SearchData))]
[JsonSerializable(typeof(OpenData))]
[JsonSerializable(typeof(PatchData))]
[JsonSerializable(typeof(McpToolInfo))]
internal partial class McpJsonSerializerContext : JsonSerializerContext
{
}
