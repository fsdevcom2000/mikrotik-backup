// MikroTik Backup Manager
//
// Coordinates backup operations for multiple routers.
// Collects individual results and produces the overall backup summary.

using MikroTikBackup.Core.Interfaces;
using MikroTikBackup.Core.Models;

namespace MikroTikBackup.Core.Services;

public sealed class BackupRunner : IBackupRunner
{
    private readonly IBackupServiceFactory _backupServiceFactory;
    private readonly ICredentialStore _credentialStore;

    public BackupRunner(
        IBackupServiceFactory backupServiceFactory,
        ICredentialStore credentialStore)
    {
        _backupServiceFactory = backupServiceFactory;
        _credentialStore = credentialStore;
    }

    public async Task<BackupRunResult> RunAsync(
        IReadOnlyList<RouterConfig> routers,
        AppConfig config,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(routers);
        ArgumentNullException.ThrowIfNull(config);

        var runResult = new BackupRunResult();

        foreach (var router in routers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var startedAt = DateTime.UtcNow;

            try
            {
                var credential =
                    await _credentialStore.GetAsync(
                        router.Credential,
                        cancellationToken);

                var backupService =
                    _backupServiceFactory.Create();

                var result =
                    await backupService.BackupRouterAsync(
                        router,
                        credential,
                        config,
                        cancellationToken);

                runResult.Results.Add(result);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                var completedAt = DateTime.UtcNow;

                runResult.Results.Add(
                    new RouterBackupResult
                    {
                        Router = router.Name,
                        Status = BackupStatus.Failed,
                        Error = ex.Message,
                        StartedAt = startedAt,
                        CompletedAt = completedAt
                    });
            }
        }

        return runResult;
    }
}