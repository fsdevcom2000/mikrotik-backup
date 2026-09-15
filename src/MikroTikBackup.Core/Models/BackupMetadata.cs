using System.Text.Json.Serialization;

namespace MikroTikBackup.Core.Models;

public sealed class BackupMetadata
{
    [JsonPropertyName("router")]
    public string Router { get; init; } = string.Empty;

    [JsonPropertyName("address")]
    public string Address { get; init; } = string.Empty;

    [JsonPropertyName("routeros_version")]
    public string? RouterOsVersion { get; init; }

    [JsonPropertyName("started_at")]
    public DateTime StartedAt { get; init; }

    [JsonPropertyName("completed_at")]
    public DateTime CompletedAt { get; init; }

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("warning")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Warning { get; init; }

    [JsonPropertyName("backup_size")]
    public long? BackupSize { get; init; }

    [JsonPropertyName("export_size")]
    public long? ExportSize { get; init; }

    [JsonPropertyName("backup_sha256")]
    public string? BackupSha256 { get; init; }

    [JsonPropertyName("export_sha256")]
    public string? ExportSha256 { get; init; }
}