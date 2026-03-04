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
    public async Task Patch_WithOverlappingRanges_ShouldReturnInvalidRange()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var filePath = Path.Combine(root, "file.txt");
            var original = "abcde\n";
            await File.WriteAllTextAsync(filePath, original);

            var workspace = TestHelpers.CreateWorkspace(root);
            var logger = new StderrLogger("error");
            var hasher = new ContentHasher();
            var preHash = hasher.ComputeContextHash(await File.ReadAllBytesAsync(filePath));
            var tool = new PatchTool(workspace, hasher, new AtomicWriter(), logger);

            var response = await tool.ExecuteAsync(new PatchRequest
            {
                Path = "file.txt",
                PreHash = preHash,
                Edits =
                [
                    new PatchEdit
                    {
                        Op = "replace",
                        StartLine = 1,
                        StartCol = 1,
                        EndLine = 1,
                        EndCol = 3,
                        Text = "AB"
                    },
                    new PatchEdit
                    {
                        Op = "delete",
                        StartLine = 1,
                        StartCol = 2,
                        EndLine = 1,
                        EndCol = 4
                    }
                ]
            }, CancellationToken.None);

            response.Ok.Should().BeFalse();
            response.ErrorCode.Should().Be(ErrorCodes.InvalidRange);
            (await File.ReadAllTextAsync(filePath)).Should().Be(original);
        }
        finally
        {
            TestHelpers.DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task Patch_ShouldNormalizeToExistingEolStyle()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var filePath = Path.Combine(root, "file.txt");
            var original = "a\r\nb\r\n";
            await File.WriteAllTextAsync(filePath, original);

            var workspace = TestHelpers.CreateWorkspace(root);
            var logger = new StderrLogger("error");
            var hasher = new ContentHasher();
            var preHash = hasher.ComputeContextHash(await File.ReadAllBytesAsync(filePath));
            var tool = new PatchTool(workspace, hasher, new AtomicWriter(), logger);

            var response = await tool.ExecuteAsync(new PatchRequest
            {
                Path = "file.txt",
                PreHash = preHash,
                Edits =
                [
                    new PatchEdit
                    {
                        Op = "replace",
                        StartLine = 1,
                        StartCol = 1,
                        EndLine = 1,
                        EndCol = 2,
                        Text = "A\nX"
                    }
                ]
            }, CancellationToken.None);

            response.Ok.Should().BeTrue();
            var content = await File.ReadAllTextAsync(filePath);
            content.Should().Be("A\r\nX\r\nb\r\n");
        }
        finally
        {
            TestHelpers.DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task Patch_ShouldPreserveUtf8Bom()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var filePath = Path.Combine(root, "bom.txt");
            var bom = new byte[] { 0xEF, 0xBB, 0xBF };
            var body = System.Text.Encoding.UTF8.GetBytes("hello\n");
            await File.WriteAllBytesAsync(filePath, bom.Concat(body).ToArray());

            var workspace = TestHelpers.CreateWorkspace(root);
            var logger = new StderrLogger("error");
            var hasher = new ContentHasher();
            var preHash = hasher.ComputeContextHash(await File.ReadAllBytesAsync(filePath));
            var tool = new PatchTool(workspace, hasher, new AtomicWriter(), logger);

            var response = await tool.ExecuteAsync(new PatchRequest
            {
                Path = "bom.txt",
                PreHash = preHash,
                Edits =
                [
                    new PatchEdit
                    {
                        Op = "replace",
                        StartLine = 1,
                        StartCol = 1,
                        EndLine = 1,
                        EndCol = 6,
                        Text = "HELLO"
                    }
                ]
            }, CancellationToken.None);

            response.Ok.Should().BeTrue();
            var bytes = await File.ReadAllBytesAsync(filePath);
            bytes[0].Should().Be(0xEF);
            bytes[1].Should().Be(0xBB);
            bytes[2].Should().Be(0xBF);
            System.Text.Encoding.UTF8.GetString(bytes[3..]).Should().Be("HELLO\n");
        }
        finally
        {
            TestHelpers.DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task PatchPreview_ShouldNotModifyFileAndReturnSummary()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var filePath = Path.Combine(root, "file.txt");
            await File.WriteAllTextAsync(filePath, "abc\n");

            var workspace = TestHelpers.CreateWorkspace(root);
            var hasher = new ContentHasher();
            var preHash = hasher.ComputeContextHash(await File.ReadAllBytesAsync(filePath));
            var preview = new PatchPreviewTool(workspace, hasher);

            var response = await preview.ExecuteAsync(new PatchPreviewRequest
            {
                Path = "file.txt",
                PreHash = preHash,
                Edits =
                [
                    new PatchEdit
                    {
                        Op = "insert",
                        Line = 1,
                        Col = 4,
                        Text = "d"
                    }
                ]
            }, CancellationToken.None);

            response.Ok.Should().BeTrue();
            var data = TestHelpers.DeserializeData<PatchPreviewData>(response);
            data.WouldApply.Should().BeTrue();
            data.DiffSummary.EditCount.Should().Be(1);
            (await File.ReadAllTextAsync(filePath)).Should().Be("abc\n");
        }
        finally
        {
            TestHelpers.DeleteDirectory(root);
        }
    }
}
