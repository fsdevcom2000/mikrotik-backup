using MikroTikBackup.Core.Models;

namespace MikroTikBackup.Core.Interfaces;

public interface IStatusService
{
    Task<IReadOnlyList<BackupStatusInfo>> GetStatusAsync(
        IReadOnlyList<RouterConfig> routers,
        string rootPath,
        CancellationToken cancellationToken);
}