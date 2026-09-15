namespace MikroTikBackup.Core.Interfaces;

public interface IBackupServiceFactory
{
    IBackupService Create();
}
