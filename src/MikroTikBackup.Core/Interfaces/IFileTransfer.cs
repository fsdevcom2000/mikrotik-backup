using MikroTikBackup.Core.Models;

namespace MikroTikBackup.Core.Interfaces;

public interface IFileTransfer
{
    Task DownloadAsync(
        RouterConfig router,
        Credential credential,
        string remoteFileName,
        string localFilePath,
        TimeSpan timeout,
        CancellationToken cancellationToken);
}