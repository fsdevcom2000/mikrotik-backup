namespace MikroTikBackup.Core.Models;

public enum BackupStatus
{
    Success,
    SuccessWithWarning,
    Failed
}

public sealed class RouterBackupResult
{
    public string Router { get; init; } = string.Empty;
    public string? RouterOsVersion { get; init; }
    public BackupStatus Status { get; init; }
    public string? Error { get; init; }

    public long? BackupSize { get; init; }
    public long? ExportSize { get; init; }

    public string? BackupSha256 { get; init; }
    public string? ExportSha256 { get; init; }

    public DateTime StartedAt { get; init; }
    public DateTime CompletedAt { get; init; }

    public TimeSpan Duration => CompletedAt - StartedAt;
}

public sealed class BackupRunResult
{
    public List<RouterBackupResult> Results { get; } = [];

    public int Total => Results.Count;

    public int SuccessCount =>
        Results.Count(x => x.Status == BackupStatus.Success);

    public int WarningCount =>
        Results.Count(x => x.Status == BackupStatus.SuccessWithWarning);

    public int FailedCount =>
        Results.Count(x => x.Status == BackupStatus.Failed);

    public bool IsSuccessful => FailedCount == 0;
}