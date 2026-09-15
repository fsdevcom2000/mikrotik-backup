// MikroTik Backup Manager
//
// Creates and reads backup metadata files.
// Metadata contains backup results, file sizes and SHA-256 hashes,
// but never stores credentials or other secrets.

using System.Text.Json;
using MikroTikBackup.Core.Interfaces;
using MikroTikBackup.Core.Models;

namespace MikroTikBackup.Storage;

public sealed class MetadataService : IMetadataService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public async Task SaveAsync(
        string directory,
        BackupMetadata metadata,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(directory);

        var filePath =
            Path.Combine(directory, "metadata.json");

        await using var stream =
            new FileStream(
                filePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 16 * 1024,
                useAsync: true);

        await JsonSerializer.SerializeAsync(
            stream,
            metadata,
            JsonOptions,
            cancellationToken);
    }
}