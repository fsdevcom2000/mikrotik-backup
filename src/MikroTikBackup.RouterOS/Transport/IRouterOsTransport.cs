namespace MikroTikBackup.RouterOS.Transport;

public interface IRouterOsTransport : IAsyncDisposable
{
    Task ConnectAsync(
        string host,
        int port,
        TimeSpan timeout,
        CancellationToken cancellationToken);

    Task SendAsync(
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken);

    Task<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken);

    bool IsConnected { get; }
}