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
            Description = "Return runtime limits, defaults, and available features.",
            InputSchema = ParseSchema("""
            {"type":"object","additionalProperties":false}
            """)
        },
        new McpToolInfo
        {
            Name = "fs.root_detect",
            Description = "Return effective workspace root and detection reason.",
            InputSchema = ParseSchema("""
            {"type":"object","additionalProperties":false}
            """)
        },
        new McpToolInfo
        {
            Name = "fs.health",
            Description = "Return process health, uptime, and active limits.",
            InputSchema = ParseSchema("""
            {"type":"object","additionalProperties":false}
            """)
        },
        new McpToolInfo
        {
            Name = "fs.scan",
            Description = "Recursively list files and directories with ignore rules and caps.",
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
            Name = "fs.readDir",
            Description = "List a single directory level with minimal metadata.",
            InputSchema = ParseSchema("""
            {
              "type":"object",
              "additionalProperties":false,
              "properties":{
                "path":{"type":"string"},
                "includeFiles":{"type":"boolean"},
                "includeDirs":{"type":"boolean"},
                "limit":{"type":"integer","minimum":1}
              }
            }
            """)
        },
        new McpToolInfo
        {
            Name = "fs.stat",
            Description = "Return file or directory metadata and hash data.",
            InputSchema = ParseSchema("""
            {
              "type":"object",
              "required":["path"],
              "additionalProperties":false,
              "properties":{
                "path":{"type":"string"}
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
                "snippetBytes":{"type":"integer","minimum":1},
                "maxFilesScanned":{"type":"integer","minimum":1},
                "maxFileSizeBytes":{"type":"integer","minimum":1},
                "timeoutMs":{"type":"integer","minimum":1}
              }
            }
            """)
        },
        new McpToolInfo
        {
            Name = "fs.open",
            Description = "Open a file by line range with strict caps.",
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
              "required":["path","preHash","edits"],
              "additionalProperties":false,
              "properties":{
                "path":{"type":"string"},
                "preHash":{"type":"string"},
                "mode":{"type":"string","enum":["strict"]},
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
                      "startLine":{"type":"integer","minimum":1},
                      "startCol":{"type":"integer","minimum":1},
                      "endLine":{"type":"integer","minimum":1},
                      "endCol":{"type":"integer","minimum":1},
                      "line":{"type":"integer","minimum":1},
                      "col":{"type":"integer","minimum":1},
                      "text":{"type":"string"}
                    }
                  }
                }
              }
            }
            """)
        },
        new McpToolInfo
        {
            Name = "fs.patchPreview",
            Description = "Validate and preview patch result without writing.",
            InputSchema = ParseSchema("""
            {
              "type":"object",
              "required":["path","preHash","edits"],
              "additionalProperties":false,
              "properties":{
                "path":{"type":"string"},
                "preHash":{"type":"string"},
                "mode":{"type":"string","enum":["strict"]},
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
                      "startLine":{"type":"integer","minimum":1},
                      "startCol":{"type":"integer","minimum":1},
                      "endLine":{"type":"integer","minimum":1},
                      "endCol":{"type":"integer","minimum":1},
                      "line":{"type":"integer","minimum":1},
                      "col":{"type":"integer","minimum":1},
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
    private readonly HealthTool _healthTool;
    private readonly ScanTool _scanTool;
    private readonly SearchTool _searchTool;
    private readonly OpenTool _openTool;
    private readonly PatchTool _patchTool;
    private readonly PatchPreviewTool _patchPreviewTool;
    private readonly StatTool _statTool;
    private readonly ReadDirTool _readDirTool;
    private readonly StderrLogger _logger;

    public Router(
        CapabilitiesTool capabilitiesTool,
        RootDetectTool rootDetectTool,
        HealthTool healthTool,
        ScanTool scanTool,
        SearchTool searchTool,
        OpenTool openTool,
        PatchTool patchTool,
        PatchPreviewTool patchPreviewTool,
        StatTool statTool,
        ReadDirTool readDirTool,
        StderrLogger logger)
    {
        _capabilitiesTool = capabilitiesTool;
        _rootDetectTool = rootDetectTool;
        _healthTool = healthTool;
        _scanTool = scanTool;
        _searchTool = searchTool;
        _openTool = openTool;
        _patchTool = patchTool;
        _patchPreviewTool = patchPreviewTool;
        _statTool = statTool;
        _readDirTool = readDirTool;
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
                            Name = AppMetadata.Name,
                            Version = AppMetadata.Version
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
                return BuildResult(request.Id, _capabilitiesTool.Execute(), McpJsonSerializerContext.Default.ToolResponse);
            case "fs.root_detect":
                return BuildResult(request.Id, _rootDetectTool.Execute(), McpJsonSerializerContext.Default.ToolResponse);
            case "fs.health":
                return BuildResult(request.Id, _healthTool.Execute(), McpJsonSerializerContext.Default.ToolResponse);
            case "fs.scan":
                return await RouteDirectAsync(request, McpJsonSerializerContext.Default.ScanRequest, _scanTool.ExecuteAsync, cancellationToken).ConfigureAwait(false);
            case "fs.search":
                return await RouteDirectAsync(request, McpJsonSerializerContext.Default.SearchRequest, _searchTool.ExecuteAsync, cancellationToken).ConfigureAwait(false);
            case "fs.open":
                return await RouteDirectAsync(request, McpJsonSerializerContext.Default.OpenRequest, _openTool.ExecuteAsync, cancellationToken).ConfigureAwait(false);
            case "fs.patch":
                return await RouteDirectAsync(request, McpJsonSerializerContext.Default.PatchRequest, _patchTool.ExecuteAsync, cancellationToken).ConfigureAwait(false);
            case "fs.patchPreview":
                return await RouteDirectAsync(request, McpJsonSerializerContext.Default.PatchPreviewRequest, _patchPreviewTool.ExecuteAsync, cancellationToken).ConfigureAwait(false);
            case "fs.stat":
                return await RouteDirectAsync(request, McpJsonSerializerContext.Default.StatRequest, _statTool.ExecuteAsync, cancellationToken).ConfigureAwait(false);
            case "fs.readDir":
                return await RouteDirectAsync(request, McpJsonSerializerContext.Default.ReadDirRequest, _readDirTool.ExecuteAsync, cancellationToken).ConfigureAwait(false);
            default:
                {
                    _logger.Warn($"unknown method={request.Method}");
                    return shouldRespond
                        ? BuildError(request.Id, -32601, "Method not found")
                        : null;
                }
        }
    }

    private async Task<JsonRpcResponse> RouteDirectAsync<TRequest>(
        JsonRpcRequest request,
        JsonTypeInfo<TRequest> typeInfo,
        Func<TRequest, CancellationToken, Task<ToolResponse>> executor,
        CancellationToken cancellationToken)
        where TRequest : class, new()
    {
        if (!TryDeserialize(request.Params, typeInfo, out TRequest? parsed, out JsonRpcResponse? invalidParamsResponse, request.Id))
        {
            return invalidParamsResponse!;
        }

        var response = await executor(parsed!, cancellationToken).ConfigureAwait(false);
        return BuildResult(request.Id, response, McpJsonSerializerContext.Default.ToolResponse);
    }

    private async Task<ToolResponse> CallToolAsync(ToolsCallParams call, CancellationToken cancellationToken)
    {
        return call.Name switch
        {
            "fs.capabilities" => _capabilitiesTool.Execute(),
            "fs.root_detect" => _rootDetectTool.Execute(),
            "fs.health" => _healthTool.Execute(),
            "fs.scan" => await RouteToolAsync(call.Arguments, McpJsonSerializerContext.Default.ScanRequest, _scanTool.ExecuteAsync, cancellationToken).ConfigureAwait(false),
            "fs.search" => await RouteToolAsync(call.Arguments, McpJsonSerializerContext.Default.SearchRequest, _searchTool.ExecuteAsync, cancellationToken).ConfigureAwait(false),
            "fs.open" => await RouteToolAsync(call.Arguments, McpJsonSerializerContext.Default.OpenRequest, _openTool.ExecuteAsync, cancellationToken).ConfigureAwait(false),
            "fs.patch" => await RouteToolAsync(call.Arguments, McpJsonSerializerContext.Default.PatchRequest, _patchTool.ExecuteAsync, cancellationToken).ConfigureAwait(false),
            "fs.patchPreview" => await RouteToolAsync(call.Arguments, McpJsonSerializerContext.Default.PatchPreviewRequest, _patchPreviewTool.ExecuteAsync, cancellationToken).ConfigureAwait(false),
            "fs.stat" => await RouteToolAsync(call.Arguments, McpJsonSerializerContext.Default.StatRequest, _statTool.ExecuteAsync, cancellationToken).ConfigureAwait(false),
            "fs.readDir" => await RouteToolAsync(call.Arguments, McpJsonSerializerContext.Default.ReadDirRequest, _readDirTool.ExecuteAsync, cancellationToken).ConfigureAwait(false),
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
