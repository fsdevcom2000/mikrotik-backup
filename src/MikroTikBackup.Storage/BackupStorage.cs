// MikroTik Backup Manager
//
// Manages the local backup directory structure and stores verified
// backup files together with their metadata.

using MikroTikBackup.Core.Interfaces;
using MikroTikBackup.Core.Models;

namespace MikroTikBackup.Storage;

public sealed class BackupStorage : IBackupStorage
{
    public string GetBackupDirectory(
        string rootPath,
        RouterConfig router,
        DateTime date)
    {
        var dateDirectory =
            date.ToString("yyyy-MM-dd");

        return Path.Combine(
            rootPath,
            dateDirectory,
            router.Name);
    }

    public string GetBackupFilePath(
        string rootPath,
        RouterConfig router,
        DateTime date)
    {
        return Path.Combine(
            GetBackupDirectory(rootPath, router, date),
            $"{router.Name}.backup");
    }

    public string GetExportFilePath(
        string rootPath,
        RouterConfig router,
        DateTime date)
    {
        return Path.Combine(
            GetBackupDirectory(rootPath, router, date),
            $"{router.Name}.rsc");
    }

    public Task VerifyFileAsync(
        string filePath,
        long expectedSize,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                $"Backup file was not found: {filePath}",
                filePath);
        }

        var fileInfo = new FileInfo(filePath);

        if (fileInfo.Length <= 0)
        {
            throw new InvalidDataException(
                $"Backup file is empty: {filePath}");
        }

        if (fileInfo.Length != expectedSize)
        {
            throw new InvalidDataException(
                $"File size mismatch for '{filePath}'. " +
                $"Expected: {expectedSize} bytes, " +
                $"actual: {fileInfo.Length} bytes.");
        }

        return Task.CompletedTask;
    }
}