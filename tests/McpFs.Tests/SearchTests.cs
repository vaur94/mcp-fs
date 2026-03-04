using System.Text.Json;
using FluentAssertions;
using McpFs.Core.Hashing;
using McpFs.Core.Search;
using McpFs.Logging;
using McpFs.Rpc;
using McpFs.Tools;

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

            var response = await tool.ExecuteAsync(new SearchRequest
            {
                Query = "needle",
                CaseSensitive = true
            }, CancellationToken.None);

            response.Ok.Should().BeTrue();
            var data = DeserializeData<SearchData>(response);
            data.Engine.Should().Be("fallback");
            data.Matches.Should().HaveCount(1);
            data.Matches[0].Path.Should().Be("sample.txt");
            data.Matches[0].Line.Should().Be(2);
            data.Matches[0].Col.Should().Be(6);
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
            var response = await tool.ExecuteAsync(new SearchRequest { Query = "find" }, CancellationToken.None);

            response.Ok.Should().BeTrue();
            var data = DeserializeData<SearchData>(response);
            data.Engine.Should().Be("rg");
            data.Matches.Should().NotBeEmpty();
            data.Matches[0].Path.Should().Be("sample.txt");
        }
        finally
        {
            TestHelpers.DeleteDirectory(root);
        }
    }

    private static T DeserializeData<T>(ToolResponse response)
    {
        response.Data.Should().NotBeNull();
        return JsonSerializer.Deserialize<T>(
            response.Data!.Value.GetRawText(),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })!;
    }
}
