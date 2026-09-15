// MikroTik Backup Manager
//
// Calculates SHA-256 hashes for locally stored backup files
// to verify and identify backup contents.

using System.Security.Cryptography;
using MikroTikBackup.Core.Interfaces;

namespace MikroTikBackup.Storage;

public sealed class FileHasher : IFileHasher
{
    public async Task<string> ComputeSha256Async(
        string filePath,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                $"File was not found: {filePath}",
                filePath);
        }

        await using var stream =
            new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 64 * 1024,
                useAsync: true);

        using var sha256 =
            SHA256.Create();

        var hash =
            await sha256.ComputeHashAsync(
                stream,
                cancellationToken);

        return Convert.ToHexString(hash)
            .ToLowerInvariant();
    }
}