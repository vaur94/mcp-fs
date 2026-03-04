using FluentAssertions;
using McpFs.Core.Hashing;

namespace McpFs.Tests;

public sealed class HashingTests
{
    [Fact]
    public async Task ContextHash_ShouldBeStableAcrossStreamAndBytes()
    {
        var hasher = new ContentHasher();
        var bytes = System.Text.Encoding.UTF8.GetBytes(new string('a', 200_000));

        await using var stream = new MemoryStream(bytes, writable: false);
        var streamHash = await hasher.ComputeContextHashAsync(stream);
        var byteHash = hasher.ComputeContextHash(bytes);

        streamHash.Should().Be(byteHash);
    }

    [Fact]
    public void QuickHash8_ShouldReturnPrefix()
    {
        var hasher = new ContentHasher();

        hasher.QuickHash8("1234567890").Should().Be("12345678");
    }
}
