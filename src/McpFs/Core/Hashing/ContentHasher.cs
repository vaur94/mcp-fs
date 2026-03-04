using System.Buffers.Binary;
using System.Security.Cryptography;

namespace McpFs.Core.Hashing;

public sealed class ContentHasher
{
    public const int HashChunkSize = 64 * 1024;

    public async Task<string> ComputeContextHashAsync(string filePath, CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        return await ComputeContextHashAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> ComputeContextHashAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        if (!stream.CanRead)
        {
            throw new InvalidOperationException("Stream is not readable.");
        }

        if (!stream.CanSeek)
        {
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
            return ComputeContextHash(ms.ToArray());
        }

        var size = stream.Length;
        using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        Span<byte> sizeBytes = stackalloc byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(sizeBytes, size);
        hasher.AppendData(sizeBytes);

        if (size <= HashChunkSize * 2L)
        {
            stream.Position = 0;
            var all = new byte[(int)size];
            await ReadExactlyAsync(stream, all, cancellationToken).ConfigureAwait(false);
            hasher.AppendData(all);
        }
        else
        {
            var first = new byte[HashChunkSize];
            stream.Position = 0;
            await ReadExactlyAsync(stream, first, cancellationToken).ConfigureAwait(false);
            hasher.AppendData(first);

            var last = new byte[HashChunkSize];
            stream.Position = size - HashChunkSize;
            await ReadExactlyAsync(stream, last, cancellationToken).ConfigureAwait(false);
            hasher.AppendData(last);
        }

        var hash = hasher.GetHashAndReset();
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public string ComputeContextHash(byte[] content)
    {
        using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        Span<byte> sizeBytes = stackalloc byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(sizeBytes, content.LongLength);
        hasher.AppendData(sizeBytes);

        if (content.LongLength <= HashChunkSize * 2L)
        {
            hasher.AppendData(content);
        }
        else
        {
            hasher.AppendData(content.AsSpan(0, HashChunkSize));
            hasher.AppendData(content.AsSpan(content.Length - HashChunkSize, HashChunkSize));
        }

        return Convert.ToHexString(hasher.GetHashAndReset()).ToLowerInvariant();
    }

    public string ComputeContextHash(string text)
    {
        return ComputeContextHash(System.Text.Encoding.UTF8.GetBytes(text));
    }

    public string QuickHash8(string contextHash)
    {
        if (string.IsNullOrEmpty(contextHash))
        {
            return string.Empty;
        }

        return contextHash.Length <= 8 ? contextHash : contextHash[..8];
    }

    private static async Task ReadExactlyAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                throw new EndOfStreamException("Unexpected EOF while hashing content.");
            }

            offset += read;
        }
    }
}
