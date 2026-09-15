using MikroTikBackup.Core.Models;

namespace MikroTikBackup.Core.Interfaces;

public interface IRouterOsClient
{
    Task ConnectAsync(
        RouterConfig router,
        Credential credential,
        TimeSpan connectionTimeout,
        CancellationToken cancellationToken);

    Task<string> GetRouterOsVersionAsync(
        CancellationToken cancellationToken);

    Task DisconnectAsync(
        CancellationToken cancellationToken);

    Task CreateBackupAsync(
        string remoteFileName,
        CancellationToken cancellationToken);

    Task CreateExportAsync(
        string remoteFileName,
        CancellationToken cancellationToken);

    Task<long> GetRemoteFileSizeAsync(
        string remoteFileName,
        CancellationToken cancellationToken);
        
    Task<long?> TryGetRemoteFileSizeAsync(
        string remoteFileName,
        CancellationToken cancellationToken);

    Task DeleteRemoteFileAsync(
        string remoteFileName,
        CancellationToken cancellationToken);
}