using FluentAssertions;
using McpFs.Tools;
using Rpc = McpFs.Rpc;

namespace McpFs.Tests;

public sealed class ReadDirTests
{
    [Fact]
    public async Task ReadDir_ShouldListSingleLevelAndRespectLimit()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "dir-a"));
            Directory.CreateDirectory(Path.Combine(root, "dir-b"));
            await File.WriteAllTextAsync(Path.Combine(root, "a.txt"), "a");
            await File.WriteAllTextAsync(Path.Combine(root, "b.txt"), "b");

            var tool = new ReadDirTool(TestHelpers.CreateWorkspace(root));
            var response = await tool.ExecuteAsync(new Rpc.ReadDirRequest { Limit = 2 }, CancellationToken.None);

            response.Ok.Should().BeTrue();
            var data = TestHelpers.DeserializeData<Rpc.ReadDirData>(response);
            data.Entries.Should().HaveCount(2);
            data.Truncated.Should().BeTrue();
        }
        finally
        {
            TestHelpers.DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task ReadDir_ShouldFilterByKind()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "dir-a"));
            await File.WriteAllTextAsync(Path.Combine(root, "a.txt"), "a");

            var tool = new ReadDirTool(TestHelpers.CreateWorkspace(root));
            var response = await tool.ExecuteAsync(new Rpc.ReadDirRequest
            {
                IncludeDirs = true,
                IncludeFiles = false
            }, CancellationToken.None);

            response.Ok.Should().BeTrue();
            var data = TestHelpers.DeserializeData<Rpc.ReadDirData>(response);
            data.Entries.Should().OnlyContain(x => x.Kind == "dir");
        }
        finally
        {
            TestHelpers.DeleteDirectory(root);
        }
    }
}
