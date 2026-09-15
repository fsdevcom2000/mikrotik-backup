namespace MikroTikBackup.Core.Interfaces;

public interface ITelegramNotifier
{
    Task SendAsync(
        string message,
        CancellationToken cancellationToken);
}