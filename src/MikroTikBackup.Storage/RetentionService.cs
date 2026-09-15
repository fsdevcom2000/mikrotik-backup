// MikroTik Backup Manager
//
// Removes local backups older than the configured retention period.
// Remote router files are not affected by retention cleanup.

using System.Globalization;
using MikroTikBackup.Core.Interfaces;

namespace MikroTikBackup.Storage;

public sealed class RetentionService : IRetentionService
{
    private const string DateFormat = "yyyy-MM-dd";

    public Task ExecuteAsync(
        string rootPath,
        int retentionDays,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException(
                "Storage root path cannot be empty.",
                nameof(rootPath));
        }

        if (retentionDays < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(retentionDays),
                retentionDays,
                "Retention days must be at least 1.");
        }

        if (!Directory.Exists(rootPath))
        {
            return Task.CompletedTask;
        }

        var cutoffDate =
            DateTime.Today.AddDays(-retentionDays);

        foreach (var directory in Directory.EnumerateDirectories(rootPath))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var directoryName =
                Path.GetFileName(directory);

            if (!DateTime.TryParseExact(
                    directoryName,
                    DateFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var backupDate))
            {
                continue;
            }

            if (backupDate >= cutoffDate)
            {
                continue;
            }

            Directory.Delete(
                directory,
                recursive: true);
        }

        return Task.CompletedTask;
    }
}