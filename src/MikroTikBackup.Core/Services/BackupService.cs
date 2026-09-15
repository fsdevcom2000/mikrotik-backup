// MikroTik Backup Manager
//
// Executes the complete backup workflow for a single MikroTik router:
// connection, backup and export creation, download, verification,
// hashing, metadata storage and remote file cleanup.

using System.Diagnostics;
using MikroTikBackup.Core.Interfaces;
using MikroTikBackup.Core.Logging;
using MikroTikBackup.Core.Models;

namespace MikroTikBackup.Core.Services;

public sealed class BackupService : IBackupService
{
    private readonly IRouterOsClient _routerOsClient;
    private readonly IFileTransfer _fileTransfer;
    private readonly IBackupStorage _storage;
    private readonly IFileHasher _fileHasher;
    private readonly IMetadataService _metadataService;
    private readonly IRetryService _retryService;
    private readonly ITransientErrorClassifier _transientErrorClassifier;
    private readonly ILogger _logger;

    public BackupService(
        IRouterOsClient routerOsClient,
        IFileTransfer fileTransfer,
        IBackupStorage storage,
        IFileHasher fileHasher,
        IMetadataService metadataService,
        IRetryService retryService,
        ITransientErrorClassifier transientErrorClassifier,
        ILogger? logger = null)
    {
        _routerOsClient = routerOsClient;
        _fileTransfer = fileTransfer;
        _storage = storage;
        _fileHasher = fileHasher;
        _metadataService = metadataService;
        _retryService = retryService;
        _transientErrorClassifier = transientErrorClassifier;
        _logger = logger ?? new NullLogger();
    }

    public async Task<RouterBackupResult> BackupRouterAsync(
        RouterConfig router,
        Credential credential,
        AppConfig config,
        CancellationToken cancellationToken)
    {
        var startedAt = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        string? remoteBackupName = null;
        string? remoteExportName = null;

        long? backupSize = null;
        long? exportSize = null;

        string? backupSha256 = null;
        string? exportSha256 = null;

        string? backupDirectory = null;
        string? routerOsVersion = null;

        _logger.Info(
            $"Starting backup for router '{router.Name}'.");

        try
        {
            _logger.Info(
                $"Connecting to router '{router.Name}'.");

            await _retryService.ExecuteAsync(
                ct => _routerOsClient.ConnectAsync(
                    router,
                    credential,
                    TimeSpan.FromSeconds(
                        config.Timeouts.ConnectionSeconds),
                    ct),
                config.Retry.Attempts,
                TimeSpan.FromSeconds(
                    config.Retry.DelaySeconds),
                _transientErrorClassifier.IsTransient,
                cancellationToken);

            _logger.Info(
                $"Connected to router '{router.Name}'.");

            routerOsVersion =
                await _routerOsClient.GetRouterOsVersionAsync(
                    cancellationToken);

            _logger.Info(
                $"RouterOS version for '{router.Name}': " +
                $"{routerOsVersion}.");

            backupDirectory =
                _storage.GetBackupDirectory(
                    config.Storage.Path,
                    router,
                    startedAt);

            var localBackupPath =
                _storage.GetBackupFilePath(
                    config.Storage.Path,
                    router,
                    startedAt);

            var localExportPath =
                _storage.GetExportFilePath(
                    config.Storage.Path,
                    router,
                    startedAt);

            Directory.CreateDirectory(
                backupDirectory);

            if (config.Backup.Binary)
            {
                remoteBackupName =
                    $"mbm-{Guid.NewGuid():N}.backup";

                _logger.Info(
                    $"Creating binary backup for " +
                    $"'{router.Name}'.");

                await _routerOsClient.CreateBackupAsync(
                    remoteBackupName,
                    cancellationToken);

                _logger.Info(
                    $"Waiting for remote backup file " +
                    $"'{remoteBackupName}'.");

                backupSize =
                    await WaitForRemoteFileAsync(
                        remoteBackupName,
                        TimeSpan.FromSeconds(
                            config.Timeouts.OperationSeconds),
                        cancellationToken);

                _logger.Info(
                    $"Remote backup file created: " +
                    $"{backupSize.Value} bytes.");

                _logger.Info(
                    $"Downloading backup file for " +
                    $"'{router.Name}' via SFTP.");

                await _retryService.ExecuteAsync(
                    ct => _fileTransfer.DownloadAsync(
                        router,
                        credential,
                        remoteBackupName,
                        localBackupPath,
                        TimeSpan.FromSeconds(
                            config.Timeouts.TransferSeconds),
                        ct),
                    config.Retry.Attempts,
                    TimeSpan.FromSeconds(
                        config.Retry.DelaySeconds),
                    _transientErrorClassifier.IsTransient,
                    cancellationToken);

                _logger.Info(
                    $"Backup downloaded successfully: " +
                    $"{backupSize.Value} bytes.");

                await _storage.VerifyFileAsync(
                    localBackupPath,
                    backupSize.Value,
                    cancellationToken);

                _logger.Info(
                    $"Backup file verification succeeded.");

                backupSha256 =
                    await _fileHasher.ComputeSha256Async(
                        localBackupPath,
                        cancellationToken);

                _logger.Info(
                    $"Backup SHA-256: {backupSha256}");
            }

            if (config.Backup.Export)
            {
                remoteExportName =
                    $"mbm-{Guid.NewGuid():N}.rsc";

                _logger.Info(
                    $"Creating text export for " +
                    $"'{router.Name}'.");

                await _routerOsClient.CreateExportAsync(
                    remoteExportName,
                    cancellationToken);

                _logger.Info(
                    $"Waiting for remote export file " +
                    $"'{remoteExportName}'.");

                exportSize =
                    await WaitForRemoteFileAsync(
                        remoteExportName,
                        TimeSpan.FromSeconds(
                            config.Timeouts.OperationSeconds),
                        cancellationToken);

                _logger.Info(
                    $"Remote export file created: " +
                    $"{exportSize.Value} bytes.");

                _logger.Info(
                    $"Downloading export file for " +
                    $"'{router.Name}' via SFTP.");

                await _retryService.ExecuteAsync(
                    ct => _fileTransfer.DownloadAsync(
                        router,
                        credential,
                        remoteExportName,
                        localExportPath,
                        TimeSpan.FromSeconds(
                            config.Timeouts.TransferSeconds),
                        ct),
                    config.Retry.Attempts,
                    TimeSpan.FromSeconds(
                        config.Retry.DelaySeconds),
                    _transientErrorClassifier.IsTransient,
                    cancellationToken);

                _logger.Info(
                    $"Export downloaded successfully: " +
                    $"{exportSize.Value} bytes.");

                await _storage.VerifyFileAsync(
                    localExportPath,
                    exportSize.Value,
                    cancellationToken);

                _logger.Info(
                    $"Export file verification succeeded.");

                exportSha256 =
                    await _fileHasher.ComputeSha256Async(
                        localExportPath,
                        cancellationToken);

                _logger.Info(
                    $"Export SHA-256: {exportSha256}");
            }

            /*
              At this point all requested backup files are:
              1. downloaded;
              2. verified;
              3. hashed.

              Save metadata before remote cleanup.
            */

            var localStorageCompletedAt =
                DateTime.UtcNow;

            var metadata =
                new BackupMetadata
                {
                    Router = router.Name,
                    Address = router.Address,
                    RouterOsVersion = routerOsVersion,
                    StartedAt = startedAt,
                    CompletedAt = localStorageCompletedAt,
                    Status = "success",
                    Warning = null,
                    BackupSize = backupSize,
                    ExportSize = exportSize,
                    BackupSha256 = backupSha256,
                    ExportSha256 = exportSha256
                };

            _logger.Info(
                $"Saving backup metadata for " +
                $"'{router.Name}'.");

            await _metadataService.SaveAsync(
                backupDirectory,
                metadata,
                cancellationToken);

            _logger.Info(
                $"Backup metadata saved successfully.");

            /*
              Local backup and metadata are now safely stored.
              Remote cleanup is allowed from this point onward.
            */

            var cleanupWarnings =
                new List<string>();

            if (remoteBackupName != null)
            {
                _logger.Info(
                    $"Removing remote backup file " +
                    $"'{remoteBackupName}'.");

                var warning =
                    await TryDeleteRemoteFileAsync(
                        remoteBackupName,
                        cancellationToken);

                if (warning != null)
                {
                    cleanupWarnings.Add(warning);

                    _logger.Warning(warning);
                }
                else
                {
                    _logger.Info(
                        $"Remote backup file " +
                        $"'{remoteBackupName}' removed successfully.");

                    remoteBackupName = null;
                }
            }

            if (remoteExportName != null)
            {
                _logger.Info(
                    $"Removing remote export file " +
                    $"'{remoteExportName}'.");

                var warning =
                    await TryDeleteRemoteFileAsync(
                        remoteExportName,
                        cancellationToken);

                if (warning != null)
                {
                    cleanupWarnings.Add(warning);

                    _logger.Warning(warning);
                }
                else
                {
                    _logger.Info(
                        $"Remote export file " +
                        $"'{remoteExportName}' removed successfully.");

                    remoteExportName = null;
                }
            }

            var completedAt =
                DateTime.UtcNow;

            var finalStatus =
                cleanupWarnings.Count == 0
                    ? BackupStatus.Success
                    : BackupStatus.SuccessWithWarning;

            var finalWarning =
                cleanupWarnings.Count == 0
                    ? null
                    : string.Join(
                        Environment.NewLine,
                        cleanupWarnings);

            if (finalStatus == BackupStatus.SuccessWithWarning)
            {
                metadata =
                    new BackupMetadata
                    {
                        Router = router.Name,
                        Address = router.Address,
                        RouterOsVersion = routerOsVersion,
                        StartedAt = startedAt,
                        CompletedAt = completedAt,
                        Status = "success_with_warning",
                        Warning = finalWarning,
                        BackupSize = backupSize,
                        ExportSize = exportSize,
                        BackupSha256 = backupSha256,
                        ExportSha256 = exportSha256
                    };

                try
                {
                    _logger.Info(
                        $"Updating metadata with final " +
                        $"cleanup warning.");

                    await _metadataService.SaveAsync(
                        backupDirectory,
                        metadata,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    finalWarning =
                        $"{finalWarning}{Environment.NewLine}" +
                        $"Failed to update metadata after " +
                        $"remote cleanup warning: {ex.Message}";

                    _logger.Error(
                        "Failed to update metadata after " +
                        "remote cleanup warning.",
                        ex);
                }
            }

            var result =
                new RouterBackupResult
                {
                    Router = router.Name,
                    RouterOsVersion = routerOsVersion,
                    Status = finalStatus,
                    Error = finalWarning,
                    BackupSize = backupSize,
                    ExportSize = exportSize,
                    BackupSha256 = backupSha256,
                    ExportSha256 = exportSha256,
                    StartedAt = startedAt,
                    CompletedAt = completedAt
                };

            if (finalStatus == BackupStatus.Success)
            {
                _logger.Info(
                    $"Backup completed successfully for " +
                    $"'{router.Name}' in " +
                    $"{stopwatch.Elapsed.TotalSeconds:F1}s.");
            }
            else
            {
                _logger.Warning(
                    $"Backup completed with warning for " +
                    $"'{router.Name}' in " +
                    $"{stopwatch.Elapsed.TotalSeconds:F1}s.");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.Error(
                $"Backup failed for router '{router.Name}'.",
                ex);

            return new RouterBackupResult
            {
                Router = router.Name,
                RouterOsVersion = routerOsVersion,
                Status = BackupStatus.Failed,
                Error = ex.Message,
                BackupSize = backupSize,
                ExportSize = exportSize,
                BackupSha256 = backupSha256,
                ExportSha256 = exportSha256,
                StartedAt = startedAt,
                CompletedAt = DateTime.UtcNow
            };
        }
        finally
        {
            await TryDisconnectAsync(
                cancellationToken);

            await TryDisposeAsync();
        }
    }

    
    private async Task<string?> TryDeleteRemoteFileAsync(
        string remoteFileName,
        CancellationToken cancellationToken)
    {
        try
        {
            await _routerOsClient.DeleteRemoteFileAsync(
                remoteFileName,
                cancellationToken);

            return null;
        }
        catch (Exception ex)
        {
            return
                $"Failed to delete remote file " +
                $"'{remoteFileName}': {ex.Message}";
        }
    }

    private async Task TryDisconnectAsync(
    CancellationToken cancellationToken)
{
    try
    {
        await _routerOsClient.DisconnectAsync(
            cancellationToken);
    }
    catch (Exception ex)
    {
        _logger.Warning(
            $"Disconnect failed: {ex.Message}");
    }
}

    private async Task TryDisposeAsync()
    {
        try
        {
            if (_routerOsClient is IAsyncDisposable disposable)
            {
                await disposable.DisposeAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(
                $"RouterOS client disposal failed: {ex.Message}");
        }
    }

    private async Task<long> WaitForRemoteFileAsync(
        string remoteFileName,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var deadline =
            DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            var size =
                await _routerOsClient.TryGetRemoteFileSizeAsync(
                    remoteFileName,
                    cancellationToken);

            if (size.HasValue && size.Value > 0)
            {
                return size.Value;
            }

            await Task.Delay(
                TimeSpan.FromMilliseconds(500),
                cancellationToken);
        }

        throw new TimeoutException(
            $"Remote file '{remoteFileName}' was not created " +
            $"within the configured timeout.");
    }
}