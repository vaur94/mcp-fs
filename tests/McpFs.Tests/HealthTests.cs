using FluentAssertions;
using McpFs.Tools;
using Rpc = McpFs.Rpc;

namespace McpFs.Tests;

public sealed class HealthTests
{
    [Fact]
    public void Health_ShouldReturnOkAndLimits()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var workspace = TestHelpers.CreateWorkspace(root);
            var tool = new HealthTool(workspace, DateTimeOffset.UtcNow.AddSeconds(-1));

            var response = tool.Execute();

            response.Ok.Should().BeTrue();
            var data = TestHelpers.DeserializeData<Rpc.HealthData>(response);
            data.Status.Should().Be("ok");
            data.UptimeMs.Should().BeGreaterThan(0);
            data.Root.Should().Be(workspace.RootPath);
            data.Limits.OpenMaxBytes.Should().Be(workspace.Config.OpenMaxBytes);
        }
        finally
        {
            TestHelpers.DeleteDirectory(root);
        }
    }
}
