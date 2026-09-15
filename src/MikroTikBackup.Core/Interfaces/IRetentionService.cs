namespace MikroTikBackup.Core.Interfaces;

public interface IRetentionService
{
    Task ExecuteAsync(
        string rootPath,
        int retentionDays,
        CancellationToken cancellationToken);
}