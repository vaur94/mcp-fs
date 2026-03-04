using FluentAssertions;
using McpFs.Core.Hashing;
using McpFs.Tools;
using Rpc = McpFs.Rpc;

namespace McpFs.Tests;

public sealed class StatTests
{
    [Fact]
    public async Task Stat_File_ShouldReturnHashAndQuickHash()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(root, "a.txt"), "hello\n");
            var tool = new StatTool(TestHelpers.CreateWorkspace(root), new ContentHasher());

            var response = await tool.ExecuteAsync(new Rpc.StatRequest { Path = "a.txt" }, CancellationToken.None);

            response.Ok.Should().BeTrue();
            var data = TestHelpers.DeserializeData<Rpc.StatData>(response);
            data.Kind.Should().Be("file");
            data.ContextHash.Should().NotBeNullOrWhiteSpace();
            data.QuickHash8.Should().NotBeNullOrWhiteSpace();
            data.QuickHash8!.Length.Should().Be(8);
        }
        finally
        {
            TestHelpers.DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task Stat_Directory_ShouldReturnDirectoryKind()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "src"));
            var tool = new StatTool(TestHelpers.CreateWorkspace(root), new ContentHasher());

            var response = await tool.ExecuteAsync(new Rpc.StatRequest { Path = "src" }, CancellationToken.None);

            response.Ok.Should().BeTrue();
            var data = TestHelpers.DeserializeData<Rpc.StatData>(response);
            data.Kind.Should().Be("dir");
            data.ContextHash.Should().BeNull();
            data.Size.Should().BeNull();
        }
        finally
        {
            TestHelpers.DeleteDirectory(root);
        }
    }
}
