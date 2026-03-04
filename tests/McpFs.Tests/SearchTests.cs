using FluentAssertions;
using McpFs.Core.Hashing;
using McpFs.Core.Search;
using McpFs.Logging;
using McpFs.Tools;
using Rpc = McpFs.Rpc;

namespace McpFs.Tests;

public sealed class SearchTests
{
    [Fact]
    public async Task FallbackSearch_ShouldReturnCorrectLineAndColumn()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(root, "sample.txt"), "alpha\nbeta needle gamma\n");

            var workspace = TestHelpers.CreateWorkspace(root);
            var logger = new StderrLogger("error");
            var hasher = new ContentHasher();
            var fallback = new FallbackSearcher(hasher, logger);
            var ripgrep = new RipgrepRunner(logger, enabled: false);
            var tool = new SearchTool(workspace, ripgrep, fallback, hasher, logger);

            var response = await tool.ExecuteAsync(new Rpc.SearchRequest
            {
                Query = "needle",
                CaseSensitive = true
            }, CancellationToken.None);

            response.Ok.Should().BeTrue();
            var data = TestHelpers.DeserializeData<Rpc.SearchData>(response);
            data.Engine.Should().Be("fallback");
            data.Results.Should().HaveCount(1);
            data.Results[0].Path.Should().Be("sample.txt");
            data.Results[0].Line.Should().Be(2);
            data.Results[0].Col.Should().Be(6);
        }
        finally
        {
            TestHelpers.DeleteDirectory(root);
        }
    }

    [Fact]
    public void RipgrepParser_ShouldParseMatchEvents()
    {
        const string line = """
        {"type":"match","data":{"path":{"text":"src/app.txt"},"lines":{"text":"hello needle world\n"},"line_number":4,"submatches":[{"match":{"text":"needle"},"start":6,"end":12}]}}
        """;

        var ok = RipgrepRunner.TryParseMatchJsonLine(line, 220, out var matches);

        ok.Should().BeTrue();
        matches.Should().HaveCount(1);
        matches[0].Path.Should().Be("src/app.txt");
        matches[0].Line.Should().Be(4);
        matches[0].Col.Should().Be(7);
        matches[0].Range.StartCol.Should().Be(7);
    }

    [Fact]
    public async Task Search_ShouldRespectMaxResults()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(root, "sample.txt"), "needle\nneedle\nneedle\n");

            var workspace = TestHelpers.CreateWorkspace(root);
            var logger = new StderrLogger("error");
            var hasher = new ContentHasher();
            var fallback = new FallbackSearcher(hasher, logger);
            var tool = new SearchTool(workspace, new RipgrepRunner(logger, enabled: false), fallback, hasher, logger);

            var response = await tool.ExecuteAsync(new Rpc.SearchRequest
            {
                Query = "needle",
                MaxResults = 2
            }, CancellationToken.None);

            response.Ok.Should().BeTrue();
            var data = TestHelpers.DeserializeData<Rpc.SearchData>(response);
            data.Results.Should().HaveCount(2);
            data.Truncated.Should().BeTrue();
        }
        finally
        {
            TestHelpers.DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task Search_ShouldRespectMaxFilesScannedInFallback()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(root, "a.txt"), "x\n");
            File.WriteAllText(Path.Combine(root, "b.txt"), "x\n");

            var workspace = TestHelpers.CreateWorkspace(root);
            var logger = new StderrLogger("error");
            var hasher = new ContentHasher();
            var fallback = new FallbackSearcher(hasher, logger);
            var tool = new SearchTool(workspace, new RipgrepRunner(logger, enabled: false), fallback, hasher, logger);

            var response = await tool.ExecuteAsync(new Rpc.SearchRequest
            {
                Query = "x",
                MaxFilesScanned = 1,
                MaxResults = 10
            }, CancellationToken.None);

            response.Ok.Should().BeTrue();
            var data = TestHelpers.DeserializeData<Rpc.SearchData>(response);
            data.Truncated.Should().BeTrue();
            data.Engine.Should().Be("fallback");
        }
        finally
        {
            TestHelpers.DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task RipgrepSearch_WhenAvailable_ShouldRunWithRgEngine()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(root, "sample.txt"), "one two three\nfind me\n");

            var workspace = TestHelpers.CreateWorkspace(root);
            var logger = new StderrLogger("error");
            var hasher = new ContentHasher();
            var fallback = new FallbackSearcher(hasher, logger);
            var ripgrep = new RipgrepRunner(logger, enabled: true);

            if (!await ripgrep.IsAvailableAsync(CancellationToken.None))
            {
                return;
            }

            var tool = new SearchTool(workspace, ripgrep, fallback, hasher, logger);
            var response = await tool.ExecuteAsync(new Rpc.SearchRequest { Query = "find" }, CancellationToken.None);

            response.Ok.Should().BeTrue();
            var data = TestHelpers.DeserializeData<Rpc.SearchData>(response);
            data.Engine.Should().Be("rg");
            data.Results.Should().NotBeEmpty();
            data.Results[0].Path.Should().Be("sample.txt");
        }
        finally
        {
            TestHelpers.DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task RipgrepSearch_WithTinyTimeout_ShouldTruncate()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var large = string.Join('\n', Enumerable.Repeat("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", 300_000)) + "\n";
            File.WriteAllText(Path.Combine(root, "big.txt"), large);

            var workspace = TestHelpers.CreateWorkspace(root);
            var logger = new StderrLogger("error");
            var hasher = new ContentHasher();
            var fallback = new FallbackSearcher(hasher, logger);
            var ripgrep = new RipgrepRunner(logger, enabled: true);

            if (!await ripgrep.IsAvailableAsync(CancellationToken.None))
            {
                return;
            }

            var tool = new SearchTool(workspace, ripgrep, fallback, hasher, logger);
            var response = await tool.ExecuteAsync(new Rpc.SearchRequest
            {
                Query = "needle-not-present",
                TimeoutMs = 1,
                MaxResults = 10
            }, CancellationToken.None);

            response.Ok.Should().BeTrue();
            var data = TestHelpers.DeserializeData<Rpc.SearchData>(response);
            data.Engine.Should().Be("rg");
            data.Truncated.Should().BeTrue();
        }
        finally
        {
            TestHelpers.DeleteDirectory(root);
        }
    }
}
