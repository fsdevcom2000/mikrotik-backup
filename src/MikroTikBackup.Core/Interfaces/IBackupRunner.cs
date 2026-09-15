using MikroTikBackup.Core.Models;

namespace MikroTikBackup.Core.Interfaces;

public interface IBackupRunner
{
    Task<BackupRunResult> RunAsync(
        IReadOnlyList<RouterConfig> routers,
        AppConfig config,
        CancellationToken cancellationToken);
}
