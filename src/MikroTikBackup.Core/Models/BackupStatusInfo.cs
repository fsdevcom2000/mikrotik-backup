using MikroTikBackup.Core.Models;

namespace MikroTikBackup.Core.Models;

public sealed class BackupStatusInfo
{
    public string Router { get; init; } = string.Empty;

    public string Address { get; init; } = string.Empty;

    public BackupStatus? Status { get; init; }

    public string? RouterOsVersion { get; init; }

    public DateTime? CompletedAt { get; init; }

    public long? BackupSize { get; init; }

    public long? ExportSize { get; init; }

    public string? Warning { get; init; }

    public string? Error { get; init; }
}