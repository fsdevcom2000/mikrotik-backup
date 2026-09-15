using MikroTikBackup.Core.Models;

namespace MikroTikBackup.Core.Interfaces;

public interface IBackupService
{
    Task<RouterBackupResult> BackupRouterAsync(
        RouterConfig router,
        Credential credential,
        AppConfig config,
        CancellationToken cancellationToken);
}