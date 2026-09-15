// MikroTik Backup Manager
//
// Provides the RouterOS API client used to connect to MikroTik routers,
// detect the RouterOS version, create backup/export files and manage
// temporary remote files.

using MikroTikBackup.Core.Interfaces;
using MikroTikBackup.Core.Models;
using MikroTikBackup.RouterOS.Exceptions;
using MikroTikBackup.RouterOS.Protocol;
using MikroTikBackup.RouterOS.Transport;

namespace MikroTikBackup.RouterOS;

public sealed class RouterOsClient : IRouterOsClient
{
    private IRouterOsTransport? _transport;

    public async Task ConnectAsync(
        RouterConfig router,
        Credential credential,
        TimeSpan connectionTimeout,
        CancellationToken cancellationToken)
    {
        if (_transport != null)
        {
            await DisconnectAsync(cancellationToken);
        }

        _transport = router.Protocol.ToLowerInvariant() switch
        {
            "api" => new RouterOsTcpTransport(),
            "api-ssl" => new RouterOsSslTransport(),
            _ => throw new RouterOsException(
                $"Unsupported RouterOS protocol: {router.Protocol}")
        };

        try
        {
            await _transport.ConnectAsync(
                router.Address,
                router.Port,
                connectionTimeout,
                cancellationToken);

            await LoginAsync(
                credential,
                cancellationToken);
        }
        catch
        {
            await _transport.DisposeAsync();
            _transport = null;

            throw;
        }
    }

    private async Task LoginAsync(
        Credential credential,
        CancellationToken cancellationToken)
    {
        try
        {
            await RunCommandAsync(
                [
                    "/login",
                    $"=name={credential.Username}",
                    $"=password={credential.Password}"
                ],
                cancellationToken);
    }
    catch (RouterOsTrapException ex)
    {
        throw new RouterOsException(
            $"RouterOS authentication failed: {ex.Message}",
            ex);
    }
}
    public async Task<string> GetRouterOsVersionAsync(
        CancellationToken cancellationToken)
    {
        var responses = await RunCommandAsync(
            ["/system/resource/print"],
            cancellationToken);

        var response = responses
            .FirstOrDefault(x =>
                x.Type == "!re");

        if (response == null)
        {
            throw new RouterOsException(
                "RouterOS did not return system information.");
        }

        var version = response.Get("version");

        if (string.IsNullOrWhiteSpace(version))
        {
            throw new RouterOsException(
                "RouterOS version was not returned.");
        }

        return version;
    }

    public async Task DisconnectAsync(
        CancellationToken cancellationToken)
    {
        if (_transport == null)
            return;

        try
        {
            if (_transport.IsConnected)
            {
                try
                {
                    await SendSentenceAsync(
                        ["/quit"],
                        cancellationToken);
                }
                catch
                {
                    // Connection may already be closed.
                }
            }
        }
        finally
        {
            await _transport.DisposeAsync();
            _transport = null;
        }
    }

    public async Task CreateBackupAsync(
        string remoteFileName,
        CancellationToken cancellationToken)
    {
        await RunCommandAsync(
            [
                "/system/backup/save",
                $"=name={remoteFileName}"
            ],
            cancellationToken);
    }

    public async Task CreateExportAsync(
        string remoteFileName,
        CancellationToken cancellationToken)
    {
        await RunCommandAsync(
            [
                "/export",
                $"=file={remoteFileName}"
            ],
            cancellationToken);
    }

    public async Task<long> GetRemoteFileSizeAsync(
        string remoteFileName,
        CancellationToken cancellationToken)
    {
        var responses = await RunCommandAsync(
            [
                "/file/print",
                $"?name={remoteFileName}"
            ],
            cancellationToken);

        var response = responses
            .FirstOrDefault(x => x.Type == "!re");

        if (response == null)
        {
            throw new RouterOsException(
                $"Remote file '{remoteFileName}' was not found.");
        }

        var size = response.Get("size");

        if (!long.TryParse(size, out var result))
        {
            throw new RouterOsException(
                $"Invalid size returned for remote file '{remoteFileName}'.");
        }

        return result;
    }

    public async Task<long?> TryGetRemoteFileSizeAsync(
        string remoteFileName,
        CancellationToken cancellationToken)
    {
        var responses = await RunCommandAsync(
            [
                "/file/print",
                $"?name={remoteFileName}"
            ],
            cancellationToken);

        var response = responses
            .FirstOrDefault(x => x.Type == "!re");

        if (response == null)
        {
            return null;
        }

        var size = response.Get("size");

        if (!long.TryParse(size, out var result))
        {
            throw new RouterOsException(
                $"Invalid size returned for remote file '{remoteFileName}'.");
        }

        return result;
    }

    public async Task DeleteRemoteFileAsync(
        string remoteFileName,
        CancellationToken cancellationToken)
    {
        await RunCommandAsync(
            [
                "/file/remove",
                $"=numbers={remoteFileName}"
            ],
            cancellationToken);
    }

    private async Task<List<RouterOsResponse>> RunCommandAsync(
        IReadOnlyList<string> words,
        CancellationToken cancellationToken)
    {
        await SendSentenceAsync(
            words,
            cancellationToken);

        var responses = new List<RouterOsResponse>();

        while (true)
        {
            var sentence =
                await ReadSentenceAsync(cancellationToken);

            responses.Add(sentence);

            switch (sentence.Type)
            {
                case "!done":
                    return responses;

                case "!trap":
                case "!fatal":
                {
                    var message =
                        sentence.Get("message")
                        ?? "Unknown RouterOS API error.";

                    var category =
                        sentence.Get("category");

                    throw new RouterOsTrapException(
                        message,
                        category);
                }
            }
        }
    }

    private async Task SendSentenceAsync(
        IReadOnlyList<string> words,
        CancellationToken cancellationToken)
    {
        if (_transport == null || !_transport.IsConnected)
        {
            throw new RouterOsException(
                "RouterOS client is not connected.");
        }

        var data = RouterOsProtocol.EncodeSentence(words);

        await _transport.SendAsync(
            data,
            cancellationToken);
    }

    private async Task<RouterOsResponse> ReadSentenceAsync(
        CancellationToken cancellationToken)
    {
        if (_transport == null || !_transport.IsConnected)
        {
            throw new RouterOsException(
                "RouterOS client is not connected.");
        }

        var words = new List<string>();

        while (true)
        {
            var word = await RouterOsProtocol.ReadWordAsync(
                _transport.ReadAsync,
                cancellationToken);

            if (word == null)
                break;

            words.Add(word);
        }

        return RouterOsSentence.Parse(words);
    }

    public async ValueTask DisposeAsync()
    {
        if (_transport != null)
        {
            await _transport.DisposeAsync();
            _transport = null;
        }
    }
}