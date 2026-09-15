using MikroTikBackup.Core.Services;

namespace Core.Tests;

public sealed class TransientErrorClassifierTests
{
    private readonly TransientErrorClassifier _classifier = new();

    [Theory]
    [InlineData(typeof(TimeoutException))]
    [InlineData(typeof(IOException))]
    [InlineData(typeof(System.Net.Sockets.SocketException))]
    [InlineData(typeof(OperationCanceledException))]
    public void IsTransient_TransientException_ReturnsTrue(
        Type exceptionType)
    {
        var exception =
            (Exception)Activator.CreateInstance(exceptionType)!;

        Assert.True(
            _classifier.IsTransient(exception));
    }

    [Theory]
    [InlineData(typeof(InvalidOperationException))]
    [InlineData(typeof(ArgumentException))]
    [InlineData(typeof(UnauthorizedAccessException))]
    public void IsTransient_PermanentException_ReturnsFalse(
        Type exceptionType)
    {
        var exception =
            (Exception)Activator.CreateInstance(exceptionType)!;

        Assert.False(
            _classifier.IsTransient(exception));
    }
}