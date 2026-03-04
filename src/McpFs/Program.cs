using McpFs.Config;
using McpFs.Core;
using McpFs.Core.Hashing;
using McpFs.Core.IO;
using McpFs.Core.Search;
using McpFs.Logging;
using McpFs.Rpc;
using McpFs.Tools;

var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cancellation.Cancel();
};

var config = ConfigLoader.Load();
var logger = new StderrLogger(config.LogLevel);
var workspace = Workspace.Create(config, logger);

logger.Info($"workspaceRoot={workspace.RootPath}");
logger.Info($"detectionReason={workspace.DetectionReason}");

var hasher = new ContentHasher();
var fileReader = new FileReader();
var atomicWriter = new AtomicWriter();
var ripgrepRunner = new RipgrepRunner(logger);
var fallbackSearcher = new FallbackSearcher(hasher, logger);

var capabilitiesTool = new CapabilitiesTool(workspace, config, ripgrepRunner);
var rootDetectTool = new RootDetectTool(workspace);
var scanTool = new ScanTool(workspace, hasher, logger);
var searchTool = new SearchTool(workspace, ripgrepRunner, fallbackSearcher, hasher, logger);
var openTool = new OpenTool(workspace, fileReader, hasher);
var patchTool = new PatchTool(workspace, hasher, atomicWriter, logger);

var router = new Router(
    capabilitiesTool,
    rootDetectTool,
    scanTool,
    searchTool,
    openTool,
    patchTool,
    logger);

var host = new JsonRpcHost(router, logger);
await host.RunAsync(cancellation.Token).ConfigureAwait(false);
