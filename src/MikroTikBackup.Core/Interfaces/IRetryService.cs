namespace MikroTikBackup.Core.Interfaces;

public interface IRetryService
{
    Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        int attempts,
        TimeSpan delay,
        Func<Exception, bool> shouldRetry,
        CancellationToken cancellationToken);

    Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        int attempts,
        TimeSpan delay,
        Func<Exception, bool> shouldRetry,
        CancellationToken cancellationToken);
}