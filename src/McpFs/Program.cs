using McpFs.Config;
using McpFs.Core;
using McpFs.Core.Hashing;
using McpFs.Core.IO;
using McpFs.Core.Search;
using McpFs.Logging;
using McpFs.Rpc;
using McpFs.Tools;

var cancellation = new CancellationTokenSource();
var processStartedAt = DateTimeOffset.UtcNow;
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cancellation.Cancel();
};

var config = ConfigLoader.Load(args);
var logger = new StderrLogger(config.LogLevel);
var workspace = Workspace.Create(config, logger);

logger.Info("workspace initialized");
logger.Info($"detectionReason={workspace.DetectionReason}");

var hasher = new ContentHasher();
var fileReader = new FileReader();
var atomicWriter = new AtomicWriter();
var ripgrepRunner = new RipgrepRunner(logger);
var fallbackSearcher = new FallbackSearcher(hasher, logger);

var capabilitiesTool = new CapabilitiesTool(workspace, config, ripgrepRunner);
var rootDetectTool = new RootDetectTool(workspace);
var healthTool = new HealthTool(workspace, processStartedAt);
var scanTool = new ScanTool(workspace, hasher, logger);
var searchTool = new SearchTool(workspace, ripgrepRunner, fallbackSearcher, hasher, logger);
var openTool = new OpenTool(workspace, fileReader, hasher);
var patchTool = new PatchTool(workspace, hasher, atomicWriter, logger);
var patchPreviewTool = new PatchPreviewTool(workspace, hasher);
var statTool = new StatTool(workspace, hasher);
var readDirTool = new ReadDirTool(workspace);

var router = new Router(
    capabilitiesTool,
    rootDetectTool,
    healthTool,
    scanTool,
    searchTool,
    openTool,
    patchTool,
    patchPreviewTool,
    statTool,
    readDirTool,
    logger);

var host = new JsonRpcHost(router, logger);
await host.RunAsync(cancellation.Token).ConfigureAwait(false);
