using MikroTikBackup.Core.Services;

namespace Core.Tests;

public sealed class RetryServiceTests
{
    [Fact]
    public async Task ExecuteAsync_SuccessfulOperation_RunsOnce()
    {
        var retryService = new RetryService();
        var attempts = 0;

        await retryService.ExecuteAsync(
            _ =>
            {
                attempts++;
                return Task.CompletedTask;
            },
            attempts: 3,
            delay: TimeSpan.Zero,
            shouldRetry: _ => true,
            CancellationToken.None);

        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task ExecuteAsync_TransientFailure_RetriesAndSucceeds()
    {
        var retryService = new RetryService();
        var attempts = 0;

        await retryService.ExecuteAsync(
            _ =>
            {
                attempts++;

                if (attempts < 3)
                {
                    throw new IOException("Temporary error.");
                }

                return Task.CompletedTask;
            },
            attempts: 3,
            delay: TimeSpan.Zero,
            shouldRetry: _ => true,
            CancellationToken.None);

        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task ExecuteAsync_RetryLimitReached_ThrowsLastException()
    {
        var retryService = new RetryService();
        var attempts = 0;

        var exception = await Assert.ThrowsAsync<IOException>(
            () => retryService.ExecuteAsync(
                _ =>
                {
                    attempts++;
                    throw new IOException("Temporary error.");
                },
                attempts: 3,
                delay: TimeSpan.Zero,
                shouldRetry: _ => true,
                CancellationToken.None));

        Assert.Equal(3, attempts);
        Assert.Equal("Temporary error.", exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_NonRetryableException_DoesNotRetry()
    {
        var retryService = new RetryService();
        var attempts = 0;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => retryService.ExecuteAsync(
                _ =>
                {
                    attempts++;
                    throw new InvalidOperationException("Permanent error.");
                },
                attempts: 3,
                delay: TimeSpan.Zero,
                shouldRetry: _ => false,
                CancellationToken.None));

        Assert.Equal(1, attempts);
        Assert.Equal("Permanent error.", exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_Cancellation_DoesNotRetry()
    {
        var retryService = new RetryService();
        var attempts = 0;

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var exception =
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => retryService.ExecuteAsync(
                    ct =>
                    {
                        attempts++;

                        cancellationTokenSource.Cancel();

                        ct.ThrowIfCancellationRequested();

                        return Task.CompletedTask;
                    },
                    attempts: 3,
                    delay: TimeSpan.Zero,
                    shouldRetry: _ => true,
                    cancellationTokenSource.Token));

        Assert.IsType<OperationCanceledException>(exception);
        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task ExecuteAsync_ZeroAttempts_ThrowsArgumentOutOfRangeException()
    {
        var retryService = new RetryService();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => retryService.ExecuteAsync(
                _ => Task.CompletedTask,
                attempts: 0,
                delay: TimeSpan.Zero,
                shouldRetry: _ => true,
                CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_NegativeDelay_ThrowsArgumentOutOfRangeException()
    {
        var retryService = new RetryService();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => retryService.ExecuteAsync(
                _ => Task.CompletedTask,
                attempts: 3,
                delay: TimeSpan.FromSeconds(-1),
                shouldRetry: _ => true,
                CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_Generic_ReturnsResult()
    {
        var retryService = new RetryService();

        var result = await retryService.ExecuteAsync(
            _ => Task.FromResult(42),
            attempts: 3,
            delay: TimeSpan.Zero,
            shouldRetry: _ => true,
            CancellationToken.None);

        Assert.Equal(42, result);
    }
}