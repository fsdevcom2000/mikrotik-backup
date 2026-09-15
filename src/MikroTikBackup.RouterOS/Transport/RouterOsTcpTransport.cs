using System.Net.Sockets;

namespace MikroTikBackup.RouterOS.Transport;

public sealed class RouterOsTcpTransport : IRouterOsTransport
{
    private TcpClient? _client;
    private NetworkStream? _stream;

    public bool IsConnected =>
        _client?.Connected == true && _stream != null;

    public async Task ConnectAsync(
        string host,
        int port,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        _client = new TcpClient();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken);

        timeoutCts.CancelAfter(timeout);

        await _client.ConnectAsync(host, port, timeoutCts.Token);

        _stream = _client.GetStream();
    }

    public async Task SendAsync(
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken)
    {
        if (_stream == null)
            throw new InvalidOperationException("Transport is not connected.");

        await _stream.WriteAsync(data, cancellationToken);
        await _stream.FlushAsync(cancellationToken);
    }

    public async Task<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken)
    {
        if (_stream == null)
            throw new InvalidOperationException("Transport is not connected.");

        return await _stream.ReadAsync(buffer, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_stream != null)
            await _stream.DisposeAsync();

        _client?.Dispose();

        _stream = null;
        _client = null;
    }
}