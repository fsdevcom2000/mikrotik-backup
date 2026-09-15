using MikroTikBackup.Core.Models;

namespace MikroTikBackup.Core.Interfaces;

public interface IMetadataService
{
    Task SaveAsync(
        string directory,
        BackupMetadata metadata,
        CancellationToken cancellationToken);
}