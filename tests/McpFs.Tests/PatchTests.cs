using System.Text.Json;
using FluentAssertions;
using McpFs.Core.Hashing;
using McpFs.Core.IO;
using McpFs.Logging;
using McpFs.Rpc;
using McpFs.Tools;

namespace McpFs.Tests;

public sealed class PatchTests
{
    [Fact]
    public async Task Patch_WithHashMismatch_ShouldNotModifyFile()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var filePath = Path.Combine(root, "file.txt");
            await File.WriteAllTextAsync(filePath, "hello\nworld\n");

            var workspace = TestHelpers.CreateWorkspace(root);
            var logger = new StderrLogger("error");
            var hasher = new ContentHasher();
            var tool = new PatchTool(workspace, hasher, new AtomicWriter(), logger);

            var response = await tool.ExecuteAsync(new PatchRequest
            {
                Path = "file.txt",
                PreHash = "deadbeef",
                Mode = "strict",
                Edits =
                [
                    new PatchEdit
                    {
                        Op = "replace",
                        Range = new PatchRange
                        {
                            StartLine = 1,
                            StartCol = 1,
                            EndLine = 1,
                            EndCol = 6
                        },
                        Text = "HELLO"
                    }
                ]
            }, CancellationToken.None);

            response.Ok.Should().BeFalse();
            response.ErrorCode.Should().Be(ErrorCodes.HashMismatch);

            var content = await File.ReadAllTextAsync(filePath);
            content.Should().Be("hello\nworld\n");
        }
        finally
        {
            TestHelpers.DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task Patch_WithMultipleEdits_ShouldApplyWithoutOffsetIssues()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var filePath = Path.Combine(root, "file.txt");
            var original = "abcde\n12345\n";
            await File.WriteAllTextAsync(filePath, original);

            var workspace = TestHelpers.CreateWorkspace(root);
            var logger = new StderrLogger("error");
            var hasher = new ContentHasher();
            var preHash = hasher.ComputeContextHash(original);
            var tool = new PatchTool(workspace, hasher, new AtomicWriter(), logger);

            var response = await tool.ExecuteAsync(new PatchRequest
            {
                Path = "file.txt",
                PreHash = preHash,
                Mode = "strict",
                Edits =
                [
                    new PatchEdit
                    {
                        Op = "replace",
                        Range = new PatchRange
                        {
                            StartLine = 1,
                            StartCol = 2,
                            EndLine = 1,
                            EndCol = 4
                        },
                        Text = "BCX"
                    },
                    new PatchEdit
                    {
                        Op = "insert",
                        At = new PatchPosition
                        {
                            Line = 2,
                            Col = 6
                        },
                        Text = "!"
                    },
                    new PatchEdit
                    {
                        Op = "delete",
                        Range = new PatchRange
                        {
                            StartLine = 1,
                            StartCol = 1,
                            EndLine = 1,
                            EndCol = 2
                        }
                    }
                ]
            }, CancellationToken.None);

            response.Ok.Should().BeTrue();
            var data = JsonSerializer.Deserialize<PatchData>(
                response.Data!.Value.GetRawText(),
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                })!;
            data.AppliedEditsCount.Should().Be(3);

            var content = await File.ReadAllTextAsync(filePath);
            content.Should().Be("BCXde\n12345!\n");
        }
        finally
        {
            TestHelpers.DeleteDirectory(root);
        }
    }
}
