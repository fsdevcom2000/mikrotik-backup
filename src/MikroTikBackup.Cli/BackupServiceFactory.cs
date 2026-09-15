using MikroTikBackup.Core.Interfaces;
using MikroTikBackup.Core.Services;
using MikroTikBackup.RouterOS;
using MikroTikBackup.RouterOS.Transfer;
using MikroTikBackup.Storage;

namespace MikroTikBackup.Cli;

public sealed class BackupServiceFactory : IBackupServiceFactory
{
    private readonly ILogger _logger;

    public BackupServiceFactory(ILogger logger)
    {
        _logger = logger;
    }

    public IBackupService Create()
    {
        var client = new RouterOsClient();

        var storage =
            new BackupStorage();

        var fileTransfer =
            new SftpFileTransfer();

        var fileHasher =
            new FileHasher();

        var metadataService =
            new MetadataService();

        var retryService =
            new RetryService();

        var transientErrorClassifier =
            new TransientErrorClassifier();

        return new BackupService(
            client,
            fileTransfer,
            storage,
            fileHasher,
            metadataService,
            retryService,
            transientErrorClassifier,
            _logger);
    }
}