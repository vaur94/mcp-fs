using System.Text;

namespace McpFs.Core.IO;

public sealed class AtomicWriter
{
    public async Task WriteTextAtomicAsync(string targetPath, string content, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(targetPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException("Target path directory could not be resolved.");
        }

        Directory.CreateDirectory(directory);

        var tempFile = Path.Combine(directory, $".mcpfs-{Guid.NewGuid():N}.tmp");

        try
        {
            await using (var stream = new FileStream(
                tempFile,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 16 * 1024,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            await using (var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
            {
                await writer.WriteAsync(content.AsMemory(), cancellationToken).ConfigureAwait(false);
                await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
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
