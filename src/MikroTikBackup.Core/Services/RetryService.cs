using MikroTikBackup.Core.Interfaces;

namespace MikroTikBackup.Core.Services;

public sealed class RetryService : IRetryService
{
    public async Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        int attempts,
        TimeSpan delay,
        Func<Exception, bool> shouldRetry,
        CancellationToken cancellationToken)
    {
        await ExecuteAsync(
            async ct =>
            {
                await operation(ct);
                return true;
            },
            attempts,
            delay,
            shouldRetry,
            cancellationToken);
    }

    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        int attempts,
        TimeSpan delay,
        Func<Exception, bool> shouldRetry,
        CancellationToken cancellationToken)
    {
        if (attempts < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(attempts),
                "Attempts must be at least 1.");
        }

        if (delay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(delay),
                "Delay cannot be negative.");
        }

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await operation(cancellationToken);
            }
            catch (Exception ex) when (
                attempt < attempts &&
                !cancellationToken.IsCancellationRequested &&
                shouldRetry(ex))
            {
                await Task.Delay(
                    delay,
                    cancellationToken);
            }
        }

        throw new InvalidOperationException(
            "Retry operation completed without returning a result.");
    }
}