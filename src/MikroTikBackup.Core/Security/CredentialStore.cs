// MikroTik Backup Manager
//
// Provides secure local storage for router credentials.
// Credentials are stored separately from the application configuration
// and are never included in backup metadata or logs.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MikroTikBackup.Core.Interfaces;

namespace MikroTikBackup.Core.Security;

public sealed class CredentialStore : ICredentialStore
{
    private readonly string _path;

    public CredentialStore(string path)
    {
        _path = path;
    }

    public async Task<Credential> GetAsync(
        string reference,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(_path))
        {
            throw new FileNotFoundException(
                $"Credentials file not found: {_path}");
        }

        var encrypted = await File.ReadAllBytesAsync(
            _path,
            cancellationToken);

        byte[] decrypted;

        try
        {
            decrypted = ProtectedData.Unprotect(
                encrypted,
                optionalEntropy: null,
                scope: DataProtectionScope.CurrentUser);
        }
        catch (CryptographicException ex)
        {
            throw new InvalidOperationException(
                "Unable to decrypt credentials file. " +
                "The file may have been created by another Windows user.",
                ex);
        }

        var json = Encoding.UTF8.GetString(decrypted);

        var credentials =
            JsonSerializer.Deserialize<Dictionary<string, CredentialEntry>>(
                json);

        if (credentials == null ||
            !credentials.TryGetValue(reference, out var entry))
        {
            throw new InvalidOperationException(
                $"Credential '{reference}' was not found.");
        }

        if (string.IsNullOrWhiteSpace(entry.Username) ||
            string.IsNullOrWhiteSpace(entry.Password))
        {
            throw new InvalidOperationException(
                $"Credential '{reference}' is incomplete.");
        }

        return new Credential(
            entry.Username,
            entry.Password);
    }

    private sealed class CredentialEntry
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}