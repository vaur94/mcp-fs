namespace McpFs.Core.IO;

public sealed class AtomicWriter
{
    public async Task WriteBytesAtomicAsync(string targetPath, ReadOnlyMemory<byte> content, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(targetPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException("Target path directory could not be resolved.");
        }

        Directory.CreateDirectory(directory);

        var fileName = Path.GetFileName(targetPath);
        var tempFile = Path.Combine(directory, $"{fileName}.mcpfs.tmp.{Guid.NewGuid():N}");

        try
        {
            await using (var stream = new FileStream(
                tempFile,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 16 * 1024,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await stream.WriteAsync(content, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                stream.Flush(flushToDisk: true);
            }

            if (OperatingSystem.IsWindows() && File.Exists(targetPath))
            {
                File.Replace(tempFile, targetPath, destinationBackupFileName: null, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(tempFile, targetPath, overwrite: true);
            }
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
