using FluentAssertions;

namespace McpFs.Tests;

public sealed class IgnoreTests
{
    [Fact]
    public void DefaultIgnore_ShouldIgnoreNodeModules()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var workspace = TestHelpers.CreateWorkspace(root);

            var ignored = workspace.IgnoreMatcher.IsIgnored("src/node_modules/pkg/index.js", isDirectory: false);

            ignored.Should().BeTrue();
        }
        finally
        {
            TestHelpers.DeleteDirectory(root);
        }
    }

    [Fact]
    public void GitIgnoreRules_ShouldSupportNegation()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(root, ".gitignore"), "generated/\n!generated/keep.txt\n");
            var workspace = TestHelpers.CreateWorkspace(root);

            var ignoredFile = workspace.IgnoreMatcher.IsIgnored("generated/a.txt", isDirectory: false);
            var includedFile = workspace.IgnoreMatcher.IsIgnored("generated/keep.txt", isDirectory: false);

            ignoredFile.Should().BeTrue();
            includedFile.Should().BeFalse();
        }
        finally
        {
            TestHelpers.DeleteDirectory(root);
        }
    }
}
