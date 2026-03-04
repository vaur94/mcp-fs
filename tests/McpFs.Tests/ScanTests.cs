using FluentAssertions;
using McpFs.Core.Hashing;
using McpFs.Logging;
using McpFs.Tools;
using Rpc = McpFs.Rpc;

namespace McpFs.Tests;

public sealed class ScanTests
{
    [Fact]
    public async Task Scan_ShouldRespectDepthAndLimit()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "a", "b", "c"));
            await File.WriteAllTextAsync(Path.Combine(root, "a", "x.txt"), "x");
            await File.WriteAllTextAsync(Path.Combine(root, "a", "b", "y.txt"), "y");
            await File.WriteAllTextAsync(Path.Combine(root, "a", "b", "c", "z.txt"), "z");

            var tool = new ScanTool(TestHelpers.CreateWorkspace(root), new ContentHasher(), new StderrLogger("error"));
            var response = await tool.ExecuteAsync(new Rpc.ScanRequest
            {
                MaxDepth = 2,
                Limit = 2
            }, CancellationToken.None);

            response.Ok.Should().BeTrue();
            var data = TestHelpers.DeserializeData<Rpc.ScanData>(response);
            data.Entries.Should().HaveCount(2);
            data.Truncated.Should().BeTrue();
        }
        finally
        {
            TestHelpers.DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task Scan_ShouldHonorDefaultIgnores()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "node_modules", "pkg"));
            await File.WriteAllTextAsync(Path.Combine(root, "node_modules", "pkg", "a.js"), "x");
            await File.WriteAllTextAsync(Path.Combine(root, "keep.txt"), "k");

            var tool = new ScanTool(TestHelpers.CreateWorkspace(root), new ContentHasher(), new StderrLogger("error"));
            var response = await tool.ExecuteAsync(new Rpc.ScanRequest(), CancellationToken.None);

            response.Ok.Should().BeTrue();
            var data = TestHelpers.DeserializeData<Rpc.ScanData>(response);
            data.Entries.Should().Contain(x => x.Path == "keep.txt");
            data.Entries.Should().NotContain(x => x.Path.Contains("node_modules", StringComparison.Ordinal));
        }
        finally
        {
            TestHelpers.DeleteDirectory(root);
        }
    }
}
