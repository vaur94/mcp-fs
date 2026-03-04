# STDIO Smoke Transcript

## Command
```bash
/home/ugur/Projects/local-mcp/mcp-fs/artifacts/linux-x64/McpFs --root /tmp/mcpfs-smoke-hb20c2q7 --config /home/ugur/Projects/local-mcp/mcp-fs/samples/mcp-fs.config.json.sample
```

## Timing
- initialize: 102.88 ms
- tools/list: 7.41 ms

## Calls

### initialize (102.88 ms)
Request:
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "initialize",
  "params": {
    "protocolVersion": "2024-11-05",
    "capabilities": {},
    "clientInfo": {
      "name": "smoke",
      "version": "1.0"
    }
  }
}
```
Response:
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "protocolVersion": "2024-11-05",
    "capabilities": {
      "tools": {
        "listChanged": false
      }
    },
    "serverInfo": {
      "name": "mcp-fs",
      "version": "1.0.0"
    }
  }
}
```

### initialized (notification)
Request:
```json
{
  "jsonrpc": "2.0",
  "method": "initialized",
  "params": {}
}
```

### tools/list (7.41 ms)
Request:
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "tools/list",
  "params": {}
}
```
Response:
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "result": {
    "tools": [
      {
        "name": "fs.capabilities",
        "description": "Return runtime limits, defaults, and available features.",
        "inputSchema": {
          "type": "object",
          "additionalProperties": false
        }
      },
      {
        "name": "fs.root_detect",
        "description": "Return effective workspace root and detection reason.",
        "inputSchema": {
          "type": "object",
          "additionalProperties": false
        }
      },
      {
        "name": "fs.health",
        "description": "Return process health, uptime, and active limits.",
        "inputSchema": {
          "type": "object",
          "additionalProperties": false
        }
      },
      {
        "name": "fs.scan",
        "description": "Recursively list files and directories with ignore rules and caps.",
        "inputSchema": {
          "type": "object",
          "additionalProperties": false,
          "properties": {
            "root": {
              "type": "string"
            },
            "maxDepth": {
              "type": "integer",
              "minimum": 0
            },
            "includeGlobs": {
              "type": "array",
              "items": {
                "type": "string"
              }
            },
            "excludeGlobs": {
              "type": "array",
              "items": {
                "type": "string"
              }
            },
            "limit": {
              "type": "integer",
              "minimum": 1
            }
          }
        }
      },
      {
        "name": "fs.readDir",
        "description": "List a single directory level with minimal metadata.",
        "inputSchema": {
          "type": "object",
          "additionalProperties": false,
          "properties": {
            "path": {
              "type": "string"
            },
            "includeFiles": {
              "type": "boolean"
            },
            "includeDirs": {
              "type": "boolean"
            },
            "limit": {
              "type": "integer",
              "minimum": 1
            }
          }
        }
      },
      {
        "name": "fs.stat",
        "description": "Return file or directory metadata and hash data.",
        "inputSchema": {
          "type": "object",
          "required": [
            "path"
          ],
          "additionalProperties": false,
          "properties": {
            "path": {
              "type": "string"
            }
          }
        }
      },
      {
        "name": "fs.search",
        "description": "Search file contents via ripgrep or fallback streaming engine.",
        "inputSchema": {
          "type": "object",
          "required": [
            "query"
          ],
          "additionalProperties": false,
          "properties": {
            "query": {
              "type": "string"
            },
            "root": {
              "type": "string"
            },
            "regex": {
              "type": "boolean"
            },
            "caseSensitive": {
              "type": "boolean"
            },
            "glob": {
              "type": "array",
              "items": {
                "type": "string"
              }
            },
            "excludeGlob": {
              "type": "array",
              "items": {
                "type": "string"
              }
            },
            "maxResults": {
              "type": "integer",
              "minimum": 1
            },
            "snippetBytes": {
              "type": "integer",
              "minimum": 1
            },
            "maxFilesScanned": {
              "type": "integer",
              "minimum": 1
            },
            "maxFileSizeBytes": {
              "type": "integer",
              "minimum": 1
            },
            "timeoutMs": {
              "type": "integer",
              "minimum": 1
            }
          }
        }
      },
      {
        "name": "fs.open",
        "description": "Open a file by line range with strict caps.",
        "inputSchema": {
          "type": "object",
          "required": [
            "path"
          ],
          "additionalProperties": false,
          "properties": {
            "path": {
              "type": "string"
            },
            "startLine": {
              "type": "integer",
              "minimum": 1
            },
            "endLine": {
              "type": "integer",
              "minimum": 1
            },
            "maxBytes": {
              "type": "integer",
              "minimum": 1
            }
          }
        }
      },
      {
        "name": "fs.patch",
        "description": "Apply strict hash-guarded text edits atomically.",
        "inputSchema": {
          "type": "object",
          "required": [
            "path",
            "preHash",
            "edits"
          ],
          "additionalProperties": false,
          "properties": {
            "path": {
              "type": "string"
            },
            "preHash": {
              "type": "string"
            },
            "mode": {
              "type": "string",
              "enum": [
                "strict"
              ]
            },
            "edits": {
              "type": "array",
              "items": {
                "type": "object",
                "required": [
                  "op"
                ],
                "additionalProperties": false,
                "properties": {
                  "op": {
                    "type": "string",
                    "enum": [
                      "replace",
                      "insert",
                      "delete"
                    ]
                  },
                  "range": {
                    "type": "object",
                    "properties": {
                      "startLine": {
                        "type": "integer",
                        "minimum": 1
                      },
                      "startCol": {
                        "type": "integer",
                        "minimum": 1
                      },
                      "endLine": {
                        "type": "integer",
                        "minimum": 1
                      },
                      "endCol": {
                        "type": "integer",
                        "minimum": 1
                      }
                    },
                    "required": [
                      "startLine",
                      "startCol",
                      "endLine",
                      "endCol"
                    ],
                    "additionalProperties": false
                  },
                  "at": {
                    "type": "object",
                    "properties": {
                      "line": {
                        "type": "integer",
                        "minimum": 1
                      },
                      "col": {
                        "type": "integer",
                        "minimum": 1
                      }
                    },
                    "required": [
                      "line",
                      "col"
                    ],
                    "additionalProperties": false
                  },
                  "startLine": {
                    "type": "integer",
                    "minimum": 1
                  },
                  "startCol": {
                    "type": "integer",
                    "minimum": 1
                  },
                  "endLine": {
                    "type": "integer",
                    "minimum": 1
                  },
                  "endCol": {
                    "type": "integer",
                    "minimum": 1
                  },
                  "line": {
                    "type": "integer",
                    "minimum": 1
                  },
                  "col": {
                    "type": "integer",
                    "minimum": 1
                  },
                  "text": {
                    "type": "string"
                  }
                }
              }
            }
          }
        }
      },
      {
        "name": "fs.patchPreview",
        "description": "Validate and preview patch result without writing.",
        "inputSchema": {
          "type": "object",
          "required": [
            "path",
            "preHash",
            "edits"
          ],
          "additionalProperties": false,
          "properties": {
            "path": {
              "type": "string"
            },
            "preHash": {
              "type": "string"
            },
            "mode": {
              "type": "string",
              "enum": [
                "strict"
              ]
            },
            "edits": {
              "type": "array",
              "items": {
                "type": "object",
                "required": [
                  "op"
                ],
                "additionalProperties": false,
                "properties": {
                  "op": {
                    "type": "string",
                    "enum": [
                      "replace",
                      "insert",
                      "delete"
                    ]
                  },
                  "range": {
                    "type": "object",
                    "properties": {
                      "startLine": {
                        "type": "integer",
                        "minimum": 1
                      },
                      "startCol": {
                        "type": "integer",
                        "minimum": 1
                      },
                      "endLine": {
                        "type": "integer",
                        "minimum": 1
                      },
                      "endCol": {
                        "type": "integer",
                        "minimum": 1
                      }
                    },
                    "required": [
                      "startLine",
                      "startCol",
                      "endLine",
                      "endCol"
                    ],
                    "additionalProperties": false
                  },
                  "at": {
                    "type": "object",
                    "properties": {
                      "line": {
                        "type": "integer",
                        "minimum": 1
                      },
                      "col": {
                        "type": "integer",
                        "minimum": 1
                      }
                    },
                    "required": [
                      "line",
                      "col"
                    ],
                    "additionalProperties": false
                  },
                  "startLine": {
                    "type": "integer",
                    "minimum": 1
                  },
                  "startCol": {
                    "type": "integer",
                    "minimum": 1
                  },
                  "endLine": {
                    "type": "integer",
                    "minimum": 1
                  },
                  "endCol": {
                    "type": "integer",
                    "minimum": 1
                  },
                  "line": {
                    "type": "integer",
                    "minimum": 1
                  },
                  "col": {
                    "type": "integer",
                    "minimum": 1
                  },
                  "text": {
                    "type": "string"
                  }
                }
              }
            }
          }
        }
      }
    ]
  }
}
```

### tools/call (26.28 ms)
Request:
```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "method": "tools/call",
  "params": {
    "name": "fs.capabilities",
    "arguments": {}
  }
}
```
Response:
```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\"ok\":true,\"data\":{\"os\":\"Fedora Linux 43 (KDE Plasma Desktop Edition)\",\"arch\":\"X64\",\"pathSeparator\":\"/\",\"version\":\"1.0.0\",\"toolAvailability\":{\"ripgrep\":true},\"defaults\":{\"searchMaxResults\":100,\"searchSnippetBytes\":220,\"searchMaxFilesScanned\":5000,\"searchMaxFileSizeBytes\":2097152,\"searchTimeoutMs\":5000,\"openMaxBytes\":65536,\"openMaxLines\":200,\"patchMaxBytes\":262144,\"patchMaxEdits\":50,\"patchMaxFileSizeBytes\":2097152,\"scanLimit\":500,\"scanMaxDepth\":16,\"followSymlinks\":false},\"limits\":{\"openHardCapBytes\":131072,\"openHardCapLines\":1000,\"searchHardCapResults\":500,\"searchHardCapSnippetBytes\":2000,\"searchHardCapFilesScanned\":20000,\"searchHardCapFileSizeBytes\":16777216,\"searchHardCapTimeoutMs\":15000,\"patchHardCapBytes\":1048576,\"patchHardCapEdits\":200,\"patchHardCapFileSizeBytes\":16777216,\"scanHardCapLimit\":5000,\"scanHardCapDepth\":64},\"features\":[\"stdio-jsonrpc\",\"strict-prehash-patch\",\"atomic-write\",\"root-sandbox\",\"ignore-root-gitignore-subset\"]}}"
      }
    ],
    "structuredContent": {
      "ok": true,
      "data": {
        "os": "Fedora Linux 43 (KDE Plasma Desktop Edition)",
        "arch": "X64",
        "pathSeparator": "/",
        "version": "1.0.0",
        "toolAvailability": {
          "ripgrep": true
        },
        "defaults": {
          "searchMaxResults": 100,
          "searchSnippetBytes": 220,
          "searchMaxFilesScanned": 5000,
          "searchMaxFileSizeBytes": 2097152,
          "searchTimeoutMs": 5000,
          "openMaxBytes": 65536,
          "openMaxLines": 200,
          "patchMaxBytes": 262144,
          "patchMaxEdits": 50,
          "patchMaxFileSizeBytes": 2097152,
          "scanLimit": 500,
          "scanMaxDepth": 16,
          "followSymlinks": false
        },
        "limits": {
          "openHardCapBytes": 131072,
          "openHardCapLines": 1000,
          "searchHardCapResults": 500,
          "searchHardCapSnippetBytes": 2000,
          "searchHardCapFilesScanned": 20000,
          "searchHardCapFileSizeBytes": 16777216,
          "searchHardCapTimeoutMs": 15000,
          "patchHardCapBytes": 1048576,
          "patchHardCapEdits": 200,
          "patchHardCapFileSizeBytes": 16777216,
          "scanHardCapLimit": 5000,
          "scanHardCapDepth": 64
        },
        "features": [
          "stdio-jsonrpc",
          "strict-prehash-patch",
          "atomic-write",
          "root-sandbox",
          "ignore-root-gitignore-subset"
        ]
      }
    },
    "isError": false
  }
}
```

### tools/call (1.76 ms)
Request:
```json
{
  "jsonrpc": "2.0",
  "id": 4,
  "method": "tools/call",
  "params": {
    "name": "fs.root_detect",
    "arguments": {}
  }
}
```
Response:
```json
{
  "jsonrpc": "2.0",
  "id": 4,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\"ok\":true,\"data\":{\"root\":\"/tmp/mcpfs-smoke-hb20c2q7\",\"reason\":\"config\"}}"
      }
    ],
    "structuredContent": {
      "ok": true,
      "data": {
        "root": "/tmp/mcpfs-smoke-hb20c2q7",
        "reason": "config"
      }
    },
    "isError": false
  }
}
```

### tools/call (5.77 ms)
Request:
```json
{
  "jsonrpc": "2.0",
  "id": 5,
  "method": "tools/call",
  "params": {
    "name": "fs.health",
    "arguments": {}
  }
}
```
Response:
```json
{
  "jsonrpc": "2.0",
  "id": 5,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\"ok\":true,\"data\":{\"status\":\"ok\",\"version\":\"1.0.0\",\"uptimeMs\":124,\"root\":\"/tmp/mcpfs-smoke-hb20c2q7\",\"followSymlinks\":false,\"limits\":{\"searchMaxResults\":100,\"searchSnippetBytes\":220,\"searchMaxFilesScanned\":5000,\"searchMaxFileSizeBytes\":2097152,\"searchTimeoutMs\":5000,\"openMaxBytes\":65536,\"openMaxLines\":200,\"patchMaxBytes\":262144,\"patchMaxEdits\":50,\"patchMaxFileSizeBytes\":2097152,\"scanLimit\":500,\"scanMaxDepth\":16,\"followSymlinks\":false}}}"
      }
    ],
    "structuredContent": {
      "ok": true,
      "data": {
        "status": "ok",
        "version": "1.0.0",
        "uptimeMs": 124,
        "root": "/tmp/mcpfs-smoke-hb20c2q7",
        "followSymlinks": false,
        "limits": {
          "searchMaxResults": 100,
          "searchSnippetBytes": 220,
          "searchMaxFilesScanned": 5000,
          "searchMaxFileSizeBytes": 2097152,
          "searchTimeoutMs": 5000,
          "openMaxBytes": 65536,
          "openMaxLines": 200,
          "patchMaxBytes": 262144,
          "patchMaxEdits": 50,
          "patchMaxFileSizeBytes": 2097152,
          "scanLimit": 500,
          "scanMaxDepth": 16,
          "followSymlinks": false
        }
      }
    },
    "isError": false
  }
}
```

### tools/call (23.12 ms)
Request:
```json
{
  "jsonrpc": "2.0",
  "id": 6,
  "method": "tools/call",
  "params": {
    "name": "fs.readDir",
    "arguments": {}
  }
}
```
Response:
```json
{
  "jsonrpc": "2.0",
  "id": 6,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\"ok\":true,\"data\":{\"entries\":[{\"name\":\"smoke.txt\",\"path\":\"smoke.txt\",\"kind\":\"file\",\"size\":24,\"mtimeUtc\":\"2026-03-04T21:21:03.4864771\\u002B00:00\",\"isSymlink\":false}],\"truncated\":false}}"
      }
    ],
    "structuredContent": {
      "ok": true,
      "data": {
        "entries": [
          {
            "name": "smoke.txt",
            "path": "smoke.txt",
            "kind": "file",
            "size": 24,
            "mtimeUtc": "2026-03-04T21:21:03.4864771+00:00",
            "isSymlink": false
          }
        ],
        "truncated": false
      }
    },
    "isError": false
  }
}
```

### tools/call (12.78 ms)
Request:
```json
{
  "jsonrpc": "2.0",
  "id": 7,
  "method": "tools/call",
  "params": {
    "name": "fs.stat",
    "arguments": {
      "path": "smoke.txt"
    }
  }
}
```
Response:
```json
{
  "jsonrpc": "2.0",
  "id": 7,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\"ok\":true,\"data\":{\"path\":\"smoke.txt\",\"kind\":\"file\",\"size\":24,\"mtimeUtc\":\"2026-03-04T21:21:03.4864771\\u002B00:00\",\"contextHash\":\"87c187833ac555cd84c829f4ba8c7f85916fd182fb1519d26c27dd9b7f842b7c\",\"quickHash8\":\"87c18783\",\"isSymlink\":false}}"
      }
    ],
    "structuredContent": {
      "ok": true,
      "data": {
        "path": "smoke.txt",
        "kind": "file",
        "size": 24,
        "mtimeUtc": "2026-03-04T21:21:03.4864771+00:00",
        "contextHash": "87c187833ac555cd84c829f4ba8c7f85916fd182fb1519d26c27dd9b7f842b7c",
        "quickHash8": "87c18783",
        "isSymlink": false
      }
    },
    "isError": false
  }
}
```

### tools/call (8.80 ms)
Request:
```json
{
  "jsonrpc": "2.0",
  "id": 8,
  "method": "tools/call",
  "params": {
    "name": "fs.open",
    "arguments": {
      "path": "smoke.txt",
      "startLine": 1,
      "endLine": 5000,
      "maxBytes": 999999
    }
  }
}
```
Response:
```json
{
  "jsonrpc": "2.0",
  "id": 8,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\"ok\":false,\"errorCode\":\"INVALID_RANGE\",\"message\":\"endLine is outside file bounds.\"}"
      }
    ],
    "structuredContent": {
      "ok": false,
      "errorCode": "INVALID_RANGE",
      "message": "endLine is outside file bounds."
    },
    "isError": true
  }
}
```

### tools/call (23.60 ms)
Request:
```json
{
  "jsonrpc": "2.0",
  "id": 9,
  "method": "tools/call",
  "params": {
    "name": "fs.search",
    "arguments": {
      "query": "marker",
      "maxResults": 1000,
      "snippetBytes": 5000,
      "timeoutMs": 20000
    }
  }
}
```
Response:
```json
{
  "jsonrpc": "2.0",
  "id": 9,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\"ok\":true,\"data\":{\"results\":[{\"path\":\"smoke.txt\",\"line\":2,\"col\":6,\"snippet\":\"beta marker\",\"range\":{\"startLine\":2,\"startCol\":6,\"endLine\":2,\"endCol\":12},\"contextHash\":\"87c187833ac555cd84c829f4ba8c7f85916fd182fb1519d26c27dd9b7f842b7c\"}],\"truncated\":false,\"engine\":\"rg\"}}"
      }
    ],
    "structuredContent": {
      "ok": true,
      "data": {
        "results": [
          {
            "path": "smoke.txt",
            "line": 2,
            "col": 6,
            "snippet": "beta marker",
            "range": {
              "startLine": 2,
              "startCol": 6,
              "endLine": 2,
              "endCol": 12
            },
            "contextHash": "87c187833ac555cd84c829f4ba8c7f85916fd182fb1519d26c27dd9b7f842b7c"
          }
        ],
        "truncated": false,
        "engine": "rg"
      }
    },
    "isError": false
  }
}
```

### tools/call (20.78 ms)
Request:
```json
{
  "jsonrpc": "2.0",
  "id": 10,
  "method": "tools/call",
  "params": {
    "name": "fs.patchPreview",
    "arguments": {
      "path": "smoke.txt",
      "preHash": "87c187833ac555cd84c829f4ba8c7f85916fd182fb1519d26c27dd9b7f842b7c",
      "mode": "strict",
      "edits": [
        {
          "op": "replace",
          "startLine": 2,
          "startCol": 1,
          "endLine": 2,
          "endCol": 12,
          "text": "BETA marker"
        }
      ]
    }
  }
}
```
Response:
```json
{
  "jsonrpc": "2.0",
  "id": 10,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\"ok\":true,\"data\":{\"wouldApply\":true,\"postHash\":\"078fc4855f12ceee2a8650e530db38bd719964ff8a8f82fac3e3a0e860be5bee\",\"diffSummary\":{\"path\":\"smoke.txt\",\"editCount\":1,\"bytesChanged\":4,\"lineDelta\":0,\"editSummaries\":[\"replace:6-17\"]},\"bytesChanged\":4,\"lineDelta\":0}}"
      }
    ],
    "structuredContent": {
      "ok": true,
      "data": {
        "wouldApply": true,
        "postHash": "078fc4855f12ceee2a8650e530db38bd719964ff8a8f82fac3e3a0e860be5bee",
        "diffSummary": {
          "path": "smoke.txt",
          "editCount": 1,
          "bytesChanged": 4,
          "lineDelta": 0,
          "editSummaries": [
            "replace:6-17"
          ]
        },
        "bytesChanged": 4,
        "lineDelta": 0
      }
    },
    "isError": false
  }
}
```

### tools/call (9.36 ms)
Request:
```json
{
  "jsonrpc": "2.0",
  "id": 11,
  "method": "tools/call",
  "params": {
    "name": "fs.patch",
    "arguments": {
      "path": "smoke.txt",
      "preHash": "87c187833ac555cd84c829f4ba8c7f85916fd182fb1519d26c27dd9b7f842b7c",
      "mode": "strict",
      "edits": [
        {
          "op": "replace",
          "startLine": 2,
          "startCol": 1,
          "endLine": 2,
          "endCol": 12,
          "text": "BETA marker"
        }
      ]
    }
  }
}
```
Response:
```json
{
  "jsonrpc": "2.0",
  "id": 11,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\"ok\":true,\"data\":{\"postHash\":\"078fc4855f12ceee2a8650e530db38bd719964ff8a8f82fac3e3a0e860be5bee\",\"appliedEditsCount\":1,\"bytesChanged\":4,\"lineDelta\":0,\"summary\":\"Applied 1 edit(s) to smoke.txt.\"}}"
      }
    ],
    "structuredContent": {
      "ok": true,
      "data": {
        "postHash": "078fc4855f12ceee2a8650e530db38bd719964ff8a8f82fac3e3a0e860be5bee",
        "appliedEditsCount": 1,
        "bytesChanged": 4,
        "lineDelta": 0,
        "summary": "Applied 1 edit(s) to smoke.txt."
      }
    },
    "isError": false
  }
}
```

### tools/call (0.71 ms)
Request:
```json
{
  "jsonrpc": "2.0",
  "id": 12,
  "method": "tools/call",
  "params": {
    "name": "fs.patch",
    "arguments": {
      "path": "smoke.txt",
      "preHash": "87c187833ac555cd84c829f4ba8c7f85916fd182fb1519d26c27dd9b7f842b7c",
      "mode": "strict",
      "edits": [
        {
          "op": "replace",
          "startLine": 2,
          "startCol": 1,
          "endLine": 2,
          "endCol": 12,
          "text": "BETA marker"
        }
      ]
    }
  }
}
```
Response:
```json
{
  "jsonrpc": "2.0",
  "id": 12,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\"ok\":false,\"errorCode\":\"HASH_MISMATCH\",\"message\":\"preHash mismatch.\"}"
      }
    ],
    "structuredContent": {
      "ok": false,
      "errorCode": "HASH_MISMATCH",
      "message": "preHash mismatch."
    },
    "isError": true
  }
}
```

### tools/call (0.38 ms)
Request:
```json
{
  "jsonrpc": "2.0",
  "id": 100,
  "method": "tools/call",
  "params": {
    "name": "fs.stat",
    "arguments": {
      "path": "../outside.txt"
    }
  }
}
```
Response:
```json
{
  "jsonrpc": "2.0",
  "id": 100,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\"ok\":false,\"errorCode\":\"OUTSIDE_ROOT\",\"message\":\"Path escapes workspace root.\"}"
      }
    ],
    "structuredContent": {
      "ok": false,
      "errorCode": "OUTSIDE_ROOT",
      "message": "Path escapes workspace root."
    },
    "isError": true
  }
}
```

### tools/call (0.36 ms)
Request:
```json
{
  "jsonrpc": "2.0",
  "id": 101,
  "method": "tools/call",
  "params": {
    "name": "fs.stat",
    "arguments": {
      "path": "/etc/passwd"
    }
  }
}
```
Response:
```json
{
  "jsonrpc": "2.0",
  "id": 101,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\"ok\":false,\"errorCode\":\"INVALID_PATH\",\"message\":\"Absolute paths are not allowed.\"}"
      }
    ],
    "structuredContent": {
      "ok": false,
      "errorCode": "INVALID_PATH",
      "message": "Absolute paths are not allowed."
    },
    "isError": true
  }
}
```

### tools/call (0.39 ms)
Request:
```json
{
  "jsonrpc": "2.0",
  "id": 102,
  "method": "tools/call",
  "params": {
    "name": "fs.stat",
    "arguments": {
      "path": "C:\\Windows\\system.ini"
    }
  }
}
```
Response:
```json
{
  "jsonrpc": "2.0",
  "id": 102,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\"ok\":false,\"errorCode\":\"INVALID_PATH\",\"message\":\"Absolute paths are not allowed.\"}"
      }
    ],
    "structuredContent": {
      "ok": false,
      "errorCode": "INVALID_PATH",
      "message": "Absolute paths are not allowed."
    },
    "isError": true
  }
}
```

### tools/call (0.34 ms)
Request:
```json
{
  "jsonrpc": "2.0",
  "id": 103,
  "method": "tools/call",
  "params": {
    "name": "fs.stat",
    "arguments": {
      "path": "\\\\server\\share\\a"
    }
  }
}
```
Response:
```json
{
  "jsonrpc": "2.0",
  "id": 103,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\"ok\":false,\"errorCode\":\"INVALID_PATH\",\"message\":\"Absolute paths are not allowed.\"}"
      }
    ],
    "structuredContent": {
      "ok": false,
      "errorCode": "INVALID_PATH",
      "message": "Absolute paths are not allowed."
    },
    "isError": true
  }
}
```

### tools/call (0.34 ms)
Request:
```json
{
  "jsonrpc": "2.0",
  "id": 104,
  "method": "tools/call",
  "params": {
    "name": "fs.stat",
    "arguments": {
      "path": "a\\..\\..\\b"
    }
  }
}
```
Response:
```json
{
  "jsonrpc": "2.0",
  "id": 104,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\"ok\":false,\"errorCode\":\"OUTSIDE_ROOT\",\"message\":\"Path escapes workspace root.\"}"
      }
    ],
    "structuredContent": {
      "ok": false,
      "errorCode": "OUTSIDE_ROOT",
      "message": "Path escapes workspace root."
    },
    "isError": true
  }
}
```

### tools/call (0.31 ms)
Request:
```json
{
  "jsonrpc": "2.0",
  "id": 105,
  "method": "tools/call",
  "params": {
    "name": "fs.stat",
    "arguments": {
      "path": "a\u0000b"
    }
  }
}
```
Response:
```json
{
  "jsonrpc": "2.0",
  "id": 105,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\"ok\":false,\"errorCode\":\"INVALID_PATH\",\"message\":\"Path contains null character.\"}"
      }
    ],
    "structuredContent": {
      "ok": false,
      "errorCode": "INVALID_PATH",
      "message": "Path contains null character."
    },
    "isError": true
  }
}
```

### tools/call (0.37 ms)
Request:
```json
{
  "jsonrpc": "2.0",
  "id": 106,
  "method": "tools/call",
  "params": {
    "name": "fs.stat",
    "arguments": {
      "path": "link-outside"
    }
  }
}
```
Response:
```json
{
  "jsonrpc": "2.0",
  "id": 106,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\"ok\":false,\"errorCode\":\"PERMISSION_DENIED\",\"message\":\"Symlink access is disabled.\"}"
      }
    ],
    "structuredContent": {
      "ok": false,
      "errorCode": "PERMISSION_DENIED",
      "message": "Symlink access is disabled."
    },
    "isError": true
  }
}
```

## Security Matrix
| Path | Expected | Actual | Pass |
|---|---|---|---|
| `../outside.txt` | `OUTSIDE_ROOT` | `OUTSIDE_ROOT` | PASS |
| `/etc/passwd` | `INVALID_PATH` | `INVALID_PATH` | PASS |
| `C:\Windows\system.ini` | `INVALID_PATH` | `INVALID_PATH` | PASS |
| `\\server\share\a` | `INVALID_PATH` | `INVALID_PATH` | PASS |
| `a\..\..\b` | `OUTSIDE_ROOT` | `OUTSIDE_ROOT` | PASS |
| `a\u0000b` | `INVALID_PATH` | `INVALID_PATH` | PASS |
| `link-outside` | `PERMISSION_DENIED` | `PERMISSION_DENIED` | PASS |

## Stdout/Stderr Check
- stdout contamination: yok (framed parser ile tüm yanıtlar başarıyla parse edildi)
- stderr sample:
```text
[2026-03-04T21:21:03.551Z] [Info] workspace initialized
[2026-03-04T21:21:03.554Z] [Info] detectionReason=config
```
