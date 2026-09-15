namespace MikroTikBackup.Core.Interfaces;

public interface ITransientErrorClassifier
{
    bool IsTransient(Exception exception);
}