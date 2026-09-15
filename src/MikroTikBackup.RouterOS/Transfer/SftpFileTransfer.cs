// MikroTik Backup Manager
//
// Provides SFTP file transfer between MikroTik routers and local storage.
// Used to download verified backup and export files from the router.

using MikroTikBackup.Core.Interfaces;
using MikroTikBackup.Core.Models;
using Renci.SshNet;

namespace MikroTikBackup.RouterOS.Transfer;

public sealed class SftpFileTransfer : IFileTransfer
{
    public async Task DownloadAsync(
        RouterConfig router,
        Credential credential,
        string remoteFileName,
        string localFilePath,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var client = new SftpClient(
            router.Address,
            router.TransferPort,
            credential.Username,
            credential.Password);

        client.OperationTimeout = timeout;
        client.BufferSize = 32 * 1024;

        await client.ConnectAsync(cancellationToken);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var directory =
                Path.GetDirectoryName(localFilePath);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using var localFile =
                new FileStream(
                    localFilePath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 32 * 1024,
                    useAsync: true);

            await client.DownloadFileAsync(
                remoteFileName,
                localFile,
                cancellationToken);
        }
        finally
        {
            if (client.IsConnected)
            {
                client.Disconnect();
            }
        }
    }
}