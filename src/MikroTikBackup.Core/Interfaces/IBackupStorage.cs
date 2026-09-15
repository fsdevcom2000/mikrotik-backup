using MikroTikBackup.Core.Models;

namespace MikroTikBackup.Core.Interfaces;

public interface IBackupStorage
{
    string GetBackupDirectory(
        string rootPath,
        RouterConfig router,
        DateTime date);

    string GetBackupFilePath(
        string rootPath,
        RouterConfig router,
        DateTime date);

    string GetExportFilePath(
        string rootPath,
        RouterConfig router,
        DateTime date);

    Task VerifyFileAsync(
        string filePath,
        long expectedSize,
        CancellationToken cancellationToken);
}