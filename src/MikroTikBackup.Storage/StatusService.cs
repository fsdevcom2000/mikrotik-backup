// MikroTik Backup Manager
//
// Reads local backup metadata and builds the current backup status
// without connecting to the MikroTik routers.

using System.Globalization;
using System.Text.Json;
using MikroTikBackup.Core.Interfaces;
using MikroTikBackup.Core.Models;

namespace MikroTikBackup.Storage;

public sealed class StatusService : IStatusService
{
    private const string DateFormat = "yyyy-MM-dd";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<IReadOnlyList<BackupStatusInfo>> GetStatusAsync(
        IReadOnlyList<RouterConfig> routers,
        string rootPath,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(routers);

        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException(
                "Storage root path cannot be empty.",
                nameof(rootPath));
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (routers.Count == 0)
        {
            return [];
        }

        if (!Directory.Exists(rootPath))
        {
            return routers
                .Select(CreateNoBackupStatus)
                .ToList();
        }

        var dateDirectories =
            GetDateDirectories(rootPath);

        var results =
            new List<BackupStatusInfo>(routers.Count);

        foreach (var router in routers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var status =
                await GetRouterStatusAsync(
                    router,
                    dateDirectories,
                    cancellationToken);

            results.Add(status);
        }

        return results;
    }

    private static List<DateDirectory> GetDateDirectories(
        string rootPath)
    {
        var result = new List<DateDirectory>();

        foreach (var directory in Directory.EnumerateDirectories(rootPath))
        {
            var name =
                Path.GetFileName(directory);

            if (!DateTime.TryParseExact(
                    name,
                    DateFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var date))
            {
                continue;
            }

            result.Add(
                new DateDirectory(
                    date.Date,
                    directory));
        }

        result.Sort(
            static (left, right) =>
                right.Date.CompareTo(left.Date));

        return result;
    }

    private static async Task<BackupStatusInfo> GetRouterStatusAsync(
        RouterConfig router,
        IReadOnlyList<DateDirectory> dateDirectories,
        CancellationToken cancellationToken)
    {
        foreach (var dateDirectory in dateDirectories)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var routerDirectory =
                Path.Combine(
                    dateDirectory.Path,
                    router.Name);

            var metadataPath =
                Path.Combine(
                    routerDirectory,
                    "metadata.json");

            if (!File.Exists(metadataPath))
            {
                continue;
            }

            BackupMetadata metadata;

            try
            {
                await using var stream =
                    new FileStream(
                        metadataPath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read,
                        bufferSize: 16 * 1024,
                        useAsync: true);

                metadata =
                    await JsonSerializer.DeserializeAsync<BackupMetadata>(
                        stream,
                        JsonOptions,
                        cancellationToken)
                    ?? throw new InvalidDataException(
                        "Metadata file is empty.");
            }
            catch (JsonException ex)
            {
                return CreateFailedStatus(
                    router,
                    $"Invalid metadata.json: {ex.Message}");
            }
            catch (InvalidDataException ex)
            {
                return CreateFailedStatus(
                    router,
                    ex.Message);
            }

            return CreateStatusFromMetadata(
                router,
                metadata);
        }

        return CreateNoBackupStatus(router);
    }

    private static BackupStatusInfo CreateStatusFromMetadata(
        RouterConfig router,
        BackupMetadata metadata)
    {
        if (!TryParseStatus(
                metadata.Status,
                out var status))
        {
            return new BackupStatusInfo
            {
                Router = router.Name,
                Address = router.Address,
                Status = BackupStatus.Failed,
                RouterOsVersion = metadata.RouterOsVersion,
                CompletedAt = metadata.CompletedAt,
                BackupSize = metadata.BackupSize,
                ExportSize = metadata.ExportSize,
                Warning = metadata.Warning,
                Error =
                    $"Unknown backup status in metadata: '{metadata.Status}'."
            };
        }

        return new BackupStatusInfo
        {
            Router = router.Name,
            Address = router.Address,
            Status = status,
            RouterOsVersion = metadata.RouterOsVersion,
            CompletedAt = metadata.CompletedAt,
            BackupSize = metadata.BackupSize,
            ExportSize = metadata.ExportSize,
            Warning = metadata.Warning
        };
    }

    private static BackupStatusInfo CreateNoBackupStatus(
        RouterConfig router)
    {
        return new BackupStatusInfo
        {
            Router = router.Name,
            Address = router.Address,
            Status = null
        };
    }

    private static BackupStatusInfo CreateFailedStatus(
        RouterConfig router,
        string error)
    {
        return new BackupStatusInfo
        {
            Router = router.Name,
            Address = router.Address,
            Status = BackupStatus.Failed,
            Error = error
        };
    }

    private static bool TryParseStatus(
        string? value,
        out BackupStatus status)
    {
        status = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized =
            value.Trim()
                .Replace("_", string.Empty)
                .Replace("-", string.Empty)
                .Replace(" ", string.Empty);

        if (normalized.Equals(
                "success",
                StringComparison.OrdinalIgnoreCase))
        {
            status = BackupStatus.Success;
            return true;
        }

        if (normalized.Equals(
                "successwithwarning",
                StringComparison.OrdinalIgnoreCase))
        {
            status = BackupStatus.SuccessWithWarning;
            return true;
        }

        if (normalized.Equals(
                "failed",
                StringComparison.OrdinalIgnoreCase))
        {
            status = BackupStatus.Failed;
            return true;
        }

        return false;
    }

    private sealed record DateDirectory(
        DateTime Date,
        string Path);
}