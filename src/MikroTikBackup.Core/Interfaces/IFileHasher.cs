namespace MikroTikBackup.Core.Interfaces;

public interface IFileHasher
{
    Task<string> ComputeSha256Async(
        string filePath,
        CancellationToken cancellationToken);
}