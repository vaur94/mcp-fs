using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using McpFs.Logging;
using McpFs.Tools;

namespace McpFs.Rpc;

public sealed class Router
{
    private static readonly IReadOnlyList<McpToolInfo> Tools =
    [
        new McpToolInfo
        {
            Name = "fs.capabilities",
            Description = "Return runtime and tool capability information.",
            InputSchema = ParseSchema("""
            {"type":"object","additionalProperties":false}
            """)
        },
        new McpToolInfo
        {
            Name = "fs.root_detect",
            Description = "Detect and return the effective workspace root.",
            InputSchema = ParseSchema("""
            {"type":"object","additionalProperties":false}
            """)
        },
        new McpToolInfo
        {
            Name = "fs.scan",
            Description = "List files/directories with ignore rules and limits.",
            InputSchema = ParseSchema("""
            {
              "type":"object",
              "additionalProperties":false,
              "properties":{
                "root":{"type":"string"},
                "maxDepth":{"type":"integer","minimum":0},
                "includeGlobs":{"type":"array","items":{"type":"string"}},
                "excludeGlobs":{"type":"array","items":{"type":"string"}},
                "limit":{"type":"integer","minimum":1}
              }
            }
            """)
        },
        new McpToolInfo
        {
            Name = "fs.search",
            Description = "Search file contents via ripgrep or fallback streaming engine.",
            InputSchema = ParseSchema("""
            {
              "type":"object",
              "required":["query"],
              "additionalProperties":false,
              "properties":{
                "query":{"type":"string"},
                "root":{"type":"string"},
                "regex":{"type":"boolean"},
                "caseSensitive":{"type":"boolean"},
                "glob":{"type":"array","items":{"type":"string"}},
                "excludeGlob":{"type":"array","items":{"type":"string"}},
                "maxResults":{"type":"integer","minimum":1},
                "snippetBytes":{"type":"integer","minimum":32}
              }
            }
            """)
        },
        new McpToolInfo
        {
            Name = "fs.open",
            Description = "Open a file by line range with byte limits.",
            InputSchema = ParseSchema("""
            {
              "type":"object",
              "required":["path"],
              "additionalProperties":false,
              "properties":{
                "path":{"type":"string"},
                "startLine":{"type":"integer","minimum":1},
                "endLine":{"type":"integer","minimum":1},
                "maxBytes":{"type":"integer","minimum":1}
              }
            }
            """)
        },
        new McpToolInfo
        {
            Name = "fs.patch",
            Description = "Apply strict hash-guarded text edits atomically.",
            InputSchema = ParseSchema("""
            {
              "type":"object",
              "required":["path","preHash","mode","edits"],
              "additionalProperties":false,
              "properties":{
                "path":{"type":"string"},
                "preHash":{"type":"string"},
                "mode":{"type":"string","enum":["strict","best_effort"]},
                "edits":{
                  "type":"array",
                  "items":{
                    "type":"object",
                    "required":["op"],
                    "additionalProperties":false,
                    "properties":{
                      "op":{"type":"string","enum":["replace","insert","delete"]},
                      "range":{
                        "type":"object",
                        "properties":{
                          "startLine":{"type":"integer","minimum":1},
                          "startCol":{"type":"integer","minimum":1},
                          "endLine":{"type":"integer","minimum":1},
                          "endCol":{"type":"integer","minimum":1}
                        },
                        "required":["startLine","startCol","endLine","endCol"],
                        "additionalProperties":false
                      },
                      "at":{
                        "type":"object",
                        "properties":{
                          "line":{"type":"integer","minimum":1},
                          "col":{"type":"integer","minimum":1}
                        },
                        "required":["line","col"],
                        "additionalProperties":false
                      },
                      "text":{"type":"string"}
                    }
                  }
                }
              }
            }
            """)
        }
    ];

    private readonly CapabilitiesTool _capabilitiesTool;
    private readonly RootDetectTool _rootDetectTool;
    private readonly ScanTool _scanTool;
    private readonly SearchTool _searchTool;
    private readonly OpenTool _openTool;
    private readonly PatchTool _patchTool;
    private readonly StderrLogger _logger;

    public Router(
        CapabilitiesTool capabilitiesTool,
        RootDetectTool rootDetectTool,
        ScanTool scanTool,
        SearchTool searchTool,
        OpenTool openTool,
        PatchTool patchTool,
        StderrLogger logger)
    {
        _capabilitiesTool = capabilitiesTool;
        _rootDetectTool = rootDetectTool;
        _scanTool = scanTool;
        _searchTool = searchTool;
        _openTool = openTool;
        _patchTool = patchTool;
        _logger = logger;
    }

    public async Task<JsonRpcResponse?> RouteAsync(JsonRpcRequest request, CancellationToken cancellationToken)
    {
        var shouldRespond = request.Id.HasValue;

        if (!string.Equals(request.Jsonrpc, "2.0", StringComparison.Ordinal))
        {
            return shouldRespond
                ? BuildError(request.Id, -32600, "Invalid Request")
                : null;
        }

        switch (request.Method)
        {
            case "initialize":
            {
                var result = new InitializeResult
                {
                    Capabilities = new ServerCapabilities
                    {
                        Tools = new ToolCapability
                        {
                            ListChanged = false
                        }
                    },
                    ServerInfo = new ServerInfo
                    {
                        Name = "mcp-fs",
                        Version = "0.1.0"
                    }
                };

                return BuildResult(request.Id, result, McpJsonSerializerContext.Default.InitializeResult);
            }
            case "initialized":
                return null;
            case "tools/list":
            {
                var result = new ToolsListResult
                {
                    Tools = Tools
                };

                return BuildResult(request.Id, result, McpJsonSerializerContext.Default.ToolsListResult);
            }
            case "tools/call":
            {
                if (!TryDeserialize(request.Params, McpJsonSerializerContext.Default.ToolsCallParams, out ToolsCallParams? callParams, out JsonRpcResponse? invalidParamsResponse, request.Id))
                {
                    return invalidParamsResponse;
                }

                var toolResponse = await CallToolAsync(callParams!, cancellationToken).ConfigureAwait(false);
                var text = JsonSerializer.Serialize(toolResponse, McpJsonSerializerContext.Default.ToolResponse);
                var result = new ToolsCallResult
                {
                    Content =
                    [
                        new McpContentItem
                        {
                            Type = "text",
                            Text = text
                        }
                    ],
                    StructuredContent = toolResponse,
                    IsError = !toolResponse.Ok
                };

                return BuildResult(request.Id, result, McpJsonSerializerContext.Default.ToolsCallResult);
            }
            case "fs.capabilities":
            {
                var toolResponse = _capabilitiesTool.Execute();
                return BuildResult(request.Id, toolResponse, McpJsonSerializerContext.Default.ToolResponse);
            }
            case "fs.root_detect":
            {
                var toolResponse = _rootDetectTool.Execute();
                return BuildResult(request.Id, toolResponse, McpJsonSerializerContext.Default.ToolResponse);
            }
            case "fs.scan":
            {
                if (!TryDeserialize(request.Params, McpJsonSerializerContext.Default.ScanRequest, out ScanRequest? scanRequest, out JsonRpcResponse? invalidParamsResponse, request.Id))
                {
                    return invalidParamsResponse;
                }

                var toolResponse = await _scanTool.ExecuteAsync(scanRequest!, cancellationToken).ConfigureAwait(false);
                return BuildResult(request.Id, toolResponse, McpJsonSerializerContext.Default.ToolResponse);
            }
            case "fs.search":
            {
                if (!TryDeserialize(request.Params, McpJsonSerializerContext.Default.SearchRequest, out SearchRequest? searchRequest, out JsonRpcResponse? invalidParamsResponse, request.Id))
                {
                    return invalidParamsResponse;
                }

                var toolResponse = await _searchTool.ExecuteAsync(searchRequest!, cancellationToken).ConfigureAwait(false);
                return BuildResult(request.Id, toolResponse, McpJsonSerializerContext.Default.ToolResponse);
            }
            case "fs.open":
            {
                if (!TryDeserialize(request.Params, McpJsonSerializerContext.Default.OpenRequest, out OpenRequest? openRequest, out JsonRpcResponse? invalidParamsResponse, request.Id))
                {
                    return invalidParamsResponse;
                }

                var toolResponse = await _openTool.ExecuteAsync(openRequest!, cancellationToken).ConfigureAwait(false);
                return BuildResult(request.Id, toolResponse, McpJsonSerializerContext.Default.ToolResponse);
            }
            case "fs.patch":
            {
                if (!TryDeserialize(request.Params, McpJsonSerializerContext.Default.PatchRequest, out PatchRequest? patchRequest, out JsonRpcResponse? invalidParamsResponse, request.Id))
                {
                    return invalidParamsResponse;
                }

                var toolResponse = await _patchTool.ExecuteAsync(patchRequest!, cancellationToken).ConfigureAwait(false);
                return BuildResult(request.Id, toolResponse, McpJsonSerializerContext.Default.ToolResponse);
            }
            default:
            {
                _logger.Warn($"unknown method={request.Method}");
                return shouldRespond
                    ? BuildError(request.Id, -32601, "Method not found")
                    : null;
            }
        }
    }

    private async Task<ToolResponse> CallToolAsync(ToolsCallParams call, CancellationToken cancellationToken)
    {
        return call.Name switch
        {
            "fs.capabilities" => _capabilitiesTool.Execute(),
            "fs.root_detect" => _rootDetectTool.Execute(),
            "fs.scan" => await RouteToolAsync(call.Arguments, McpJsonSerializerContext.Default.ScanRequest, _scanTool.ExecuteAsync, cancellationToken).ConfigureAwait(false),
            "fs.search" => await RouteToolAsync(call.Arguments, McpJsonSerializerContext.Default.SearchRequest, _searchTool.ExecuteAsync, cancellationToken).ConfigureAwait(false),
            "fs.open" => await RouteToolAsync(call.Arguments, McpJsonSerializerContext.Default.OpenRequest, _openTool.ExecuteAsync, cancellationToken).ConfigureAwait(false),
            "fs.patch" => await RouteToolAsync(call.Arguments, McpJsonSerializerContext.Default.PatchRequest, _patchTool.ExecuteAsync, cancellationToken).ConfigureAwait(false),
            _ => ToolResponse.Failure(ErrorCodes.NotFound, $"Unknown tool '{call.Name}'")
        };
    }

    private static async Task<ToolResponse> RouteToolAsync<TRequest>(
        JsonElement? args,
        JsonTypeInfo<TRequest> typeInfo,
        Func<TRequest, CancellationToken, Task<ToolResponse>> executor,
        CancellationToken cancellationToken)
        where TRequest : class, new()
    {
        if (!TryDeserializeTool(args, typeInfo, out var request, out var error))
        {
            return error!;
        }

        return await executor(request!, cancellationToken).ConfigureAwait(false);
    }

    private static bool TryDeserialize<T>(
        JsonElement? source,
        JsonTypeInfo<T> typeInfo,
        out T? value,
        out JsonRpcResponse? invalidResponse,
        JsonElement? requestId)
        where T : class, new()
    {
        value = default;
        invalidResponse = null;

        try
        {
            if (!source.HasValue || source.Value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            {
                value = new T();
                return true;
            }

            value = source.Value.Deserialize(typeInfo);
            if (value is null)
            {
                invalidResponse = BuildError(requestId, -32602, "Invalid params");
                return false;
            }

            return true;
        }
        catch
        {
            invalidResponse = BuildError(requestId, -32602, "Invalid params");
            return false;
        }
    }

    private static bool TryDeserializeTool<T>(
        JsonElement? source,
        JsonTypeInfo<T> typeInfo,
        out T? value,
        out ToolResponse? error)
        where T : class, new()
    {
        value = default;
        error = null;

        try
        {
            if (!source.HasValue || source.Value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            {
                value = new T();
                return true;
            }

            value = source.Value.Deserialize(typeInfo);
            if (value is null)
            {
                error = ToolResponse.Failure(ErrorCodes.InternalError, "Invalid tool arguments");
                return false;
            }

            return true;
        }
        catch
        {
            error = ToolResponse.Failure(ErrorCodes.InternalError, "Invalid tool arguments");
            return false;
        }
    }

    private static JsonRpcResponse BuildError(JsonElement? id, int code, string message)
    {
        return new JsonRpcResponse
        {
            Id = id,
            Error = new JsonRpcError
            {
                Code = code,
                Message = message
            }
        };
    }

    private static JsonRpcResponse BuildResult<T>(JsonElement? id, T payload, JsonTypeInfo<T> typeInfo)
    {
        var result = ToElement(payload, typeInfo);

        return new JsonRpcResponse
        {
            Id = id,
            Result = result
        };
    }

    private static JsonElement ParseSchema(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static JsonElement ToElement<T>(T payload, JsonTypeInfo<T> typeInfo)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload, typeInfo);
        using var doc = JsonDocument.Parse(bytes);
        return doc.RootElement.Clone();
    }
}
