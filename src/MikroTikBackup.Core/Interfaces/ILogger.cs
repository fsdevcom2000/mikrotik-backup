namespace MikroTikBackup.Core.Interfaces;

public interface ILogger
{
    void Info(string message);

    void Warning(string message);

    void Error(string message);

    void Error(string message, Exception exception);
}