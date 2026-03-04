using FluentAssertions;
using McpFs.Rpc;

namespace McpFs.Tests;

public sealed class PathPolicyTests
{
    [Fact]
    public void ResolvePath_WithTraversal_ShouldReturnOutsideRoot()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var workspace = TestHelpers.CreateWorkspace(root);

            var ok = workspace.PathPolicy.TryResolvePath("../escape.txt", out _, out _, out var error);

            ok.Should().BeFalse();
            error.Should().NotBeNull();
            error!.ErrorCode.Should().Be(ErrorCodes.OutsideRoot);
        }
        finally
        {
            TestHelpers.DeleteDirectory(root);
        }
    }

    [Fact]
    public void ResolvePath_WithAbsolutePath_ShouldReturnInvalidPath()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var workspace = TestHelpers.CreateWorkspace(root);
            var absolute = Path.GetFullPath(Path.Combine(root, "a.txt"));

            var ok = workspace.PathPolicy.TryResolvePath(absolute, out _, out _, out var error);

            ok.Should().BeFalse();
            error!.ErrorCode.Should().Be(ErrorCodes.InvalidPath);
        }
        finally
        {
            TestHelpers.DeleteDirectory(root);
        }
    }

    [Theory]
    [InlineData("C:/Windows/System32")]
    [InlineData("C:\\Windows\\System32")]
    [InlineData("\\\\server\\share\\file.txt")]
    [InlineData("//server/share/file.txt")]
    public void ResolvePath_WithDriveOrUnc_ShouldReturnInvalidPath(string input)
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var workspace = TestHelpers.CreateWorkspace(root);

            var ok = workspace.PathPolicy.TryResolvePath(input, out _, out _, out var error);

            ok.Should().BeFalse();
            error.Should().NotBeNull();
            error!.ErrorCode.Should().Be(ErrorCodes.InvalidPath);
        }
        finally
        {
            TestHelpers.DeleteDirectory(root);
        }
    }
}
