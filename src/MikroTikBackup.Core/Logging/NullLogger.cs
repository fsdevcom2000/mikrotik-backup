using MikroTikBackup.Core.Interfaces;

namespace MikroTikBackup.Core.Logging;

public sealed class NullLogger : ILogger
{
    public void Info(string message)
    {
    }

    public void Warning(string message)
    {
    }

    public void Error(string message)
    {
    }

    public void Error(
        string message,
        Exception exception)
    {
    }
}