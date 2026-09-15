namespace MikroTikBackup.Core.Interfaces;

public interface ICredentialStore
{
    Task<Credential> GetAsync(
        string reference,
        CancellationToken cancellationToken);
}

public sealed record Credential(
    string Username,
    string Password);