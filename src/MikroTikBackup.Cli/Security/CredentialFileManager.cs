// MikroTik Backup Manager
//
// Manages the local credentials.dat file used by the CLI.
// Handles loading, saving and protecting stored router credentials.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MikroTikBackup.Cli.Security;

public static class CredentialFileManager
{
    public static async Task AddAsync(
        string path,
        string reference,
        string username,
        string password,
        CancellationToken cancellationToken)
    {
        var credentials =
            await LoadAsync(path, cancellationToken);

        credentials[reference] = new CredentialEntry
        {
            Username = username,
            Password = password
        };

        var json = JsonSerializer.Serialize(
            credentials,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        var plaintext = Encoding.UTF8.GetBytes(json);

        var encrypted = ProtectedData.Protect(
            plaintext,
            optionalEntropy: null,
            scope: DataProtectionScope.CurrentUser);

        await File.WriteAllBytesAsync(
            path,
            encrypted,
            cancellationToken);
    }

    private static async Task<
        Dictionary<string, CredentialEntry>>
        LoadAsync(
            string path,
            CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
            return new();

        var encrypted = await File.ReadAllBytesAsync(
            path,
            cancellationToken);

        var plaintext = ProtectedData.Unprotect(
            encrypted,
            optionalEntropy: null,
            scope: DataProtectionScope.CurrentUser);

        var json = Encoding.UTF8.GetString(plaintext);

        return JsonSerializer.Deserialize<
                   Dictionary<string, CredentialEntry>>(json)
               ?? new();
    }

    private sealed class CredentialEntry
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}