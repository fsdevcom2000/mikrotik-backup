using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;

namespace MikroTikBackup.RouterOS.Transport;

public sealed class RouterOsSslTransport : IRouterOsTransport
{
    private TcpClient? _client;
    private SslStream? _stream;

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

        var networkStream = _client.GetStream();

        _stream = new SslStream(
            networkStream,
            leaveInnerStreamOpen: false,
            (_, _, _, _) => true);

        await _stream.AuthenticateAsClientAsync(
            new SslClientAuthenticationOptions
            {
                TargetHost = host,
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                CertificateRevocationCheckMode =
                    System.Security.Cryptography.X509Certificates
                        .X509RevocationMode.NoCheck
            },
            cancellationToken);
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