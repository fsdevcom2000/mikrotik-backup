using System.Text.Json;
using MikroTikBackup.Core.Models;
using MikroTikBackup.Storage;

namespace Storage.Tests;

public sealed class MetadataServiceTests : IDisposable
{
    private readonly string _rootPath;

    public MetadataServiceTests()
    {
        _rootPath = Path.Combine(
            Path.GetTempPath(),
            "MikroTikBackupTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_rootPath);
    }

    [Fact]
    public async Task SaveAsync_CreatesMetadataFile()
    {
        var service = new MetadataService();

        var directory =
            Path.Combine(
                _rootPath,
                "office-01");

        var metadata =
            CreateMetadata();

        await service.SaveAsync(
            directory,
            metadata,
            CancellationToken.None);

        var filePath =
            Path.Combine(
                directory,
                "metadata.json");

        Assert.True(
            File.Exists(filePath));
    }

    [Fact]
    public async Task SaveAsync_CreatesDirectoryWhenItDoesNotExist()
    {
        var service = new MetadataService();

        var directory =
            Path.Combine(
                _rootPath,
                "new-directory");

        Assert.False(
            Directory.Exists(directory));

        await service.SaveAsync(
            directory,
            CreateMetadata(),
            CancellationToken.None);

        Assert.True(
            Directory.Exists(directory));

        Assert.True(
            File.Exists(
                Path.Combine(
                    directory,
                    "metadata.json")));
    }

    [Fact]
    public async Task SaveAsync_WritesValidJson()
    {
        var service = new MetadataService();

        var directory =
            Path.Combine(
                _rootPath,
                "office-01");

        await service.SaveAsync(
            directory,
            CreateMetadata(),
            CancellationToken.None);

        var filePath =
            Path.Combine(
                directory,
                "metadata.json");

        var json =
            await File.ReadAllTextAsync(filePath);

        using var document =
            JsonDocument.Parse(json);

        Assert.Equal(
            JsonValueKind.Object,
            document.RootElement.ValueKind);
    }

    [Fact]
    public async Task SaveAsync_WritesExpectedPropertyNames()
    {
        var service = new MetadataService();

        var directory =
            Path.Combine(
                _rootPath,
                "office-01");

        await service.SaveAsync(
            directory,
            CreateMetadata(),
            CancellationToken.None);

        var filePath =
            Path.Combine(
                directory,
                "metadata.json");

        var json =
            await File.ReadAllTextAsync(filePath);

        using var document =
            JsonDocument.Parse(json);

        var properties =
            document.RootElement
                .EnumerateObject()
                .Select(x => x.Name)
                .ToHashSet();

        Assert.Contains("router", properties);
        Assert.Contains("address", properties);
        Assert.Contains("routeros_version", properties);
        Assert.Contains("started_at", properties);
        Assert.Contains("completed_at", properties);
        Assert.Contains("status", properties);
        Assert.Contains("backup_size", properties);
        Assert.Contains("export_size", properties);
        Assert.Contains("backup_sha256", properties);
        Assert.Contains("export_sha256", properties);
    }

    [Fact]
    public async Task SaveAsync_WritesExpectedValues()
    {
        var service = new MetadataService();

        var directory =
            Path.Combine(
                _rootPath,
                "office-01");

        var metadata =
            CreateMetadata();

        await service.SaveAsync(
            directory,
            metadata,
            CancellationToken.None);

        var filePath =
            Path.Combine(
                directory,
                "metadata.json");

        var json =
            await File.ReadAllTextAsync(filePath);

        using var document =
            JsonDocument.Parse(json);

        var root =
            document.RootElement;

        Assert.Equal(
            "office-01",
            root.GetProperty("router").GetString());

        Assert.Equal(
            "192.168.88.1",
            root.GetProperty("address").GetString());

        Assert.Equal(
            "7.18.2",
            root.GetProperty("routeros_version").GetString());

        Assert.Equal(
            "success",
            root.GetProperty("status").GetString());

        Assert.Equal(
            123456L,
            root.GetProperty("backup_size").GetInt64());

        Assert.Equal(
            45678L,
            root.GetProperty("export_size").GetInt64());

        Assert.Equal(
            "backup-hash",
            root.GetProperty("backup_sha256").GetString());

        Assert.Equal(
            "export-hash",
            root.GetProperty("export_sha256").GetString());
    }

    [Fact]
    public async Task SaveAsync_DoesNotWriteWarningWhenWarningIsNull()
    {
        var service = new MetadataService();

        var directory =
            Path.Combine(
                _rootPath,
                "office-01");

        var metadata =
            CreateMetadata();

        await service.SaveAsync(
            directory,
            metadata,
            CancellationToken.None);

        var filePath =
            Path.Combine(
                directory,
                "metadata.json");

        var json =
            await File.ReadAllTextAsync(filePath);

        using var document =
            JsonDocument.Parse(json);

        Assert.False(
            document.RootElement
                .TryGetProperty(
                    "warning",
                    out _));
    }

    [Fact]
    public async Task SaveAsync_WritesWarningWhenSpecified()
    {
        var service = new MetadataService();

        var directory =
            Path.Combine(
                _rootPath,
                "office-01");

        var metadata =
            new BackupMetadata
            {
                Router = "office-01",
                Address = "192.168.88.1",
                RouterOsVersion = "7.18.2",
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
                Status = "success_with_warning",
                Warning = "Failed to delete remote file.",
                BackupSize = 123456,
                ExportSize = 45678,
                BackupSha256 = "backup-hash",
                ExportSha256 = "export-hash"
            };

        await service.SaveAsync(
            directory,
            metadata,
            CancellationToken.None);

        var filePath =
            Path.Combine(
                directory,
                "metadata.json");

        var json =
            await File.ReadAllTextAsync(filePath);

        using var document =
            JsonDocument.Parse(json);

        Assert.Equal(
            "Failed to delete remote file.",
            document.RootElement
                .GetProperty("warning")
                .GetString());
    }

    [Fact]
    public async Task SaveAsync_OverwritesExistingMetadata()
    {
        var service = new MetadataService();

        var directory =
            Path.Combine(
                _rootPath,
                "office-01");

        await service.SaveAsync(
            directory,
            CreateMetadata(),
            CancellationToken.None);

        var updatedMetadata =
            new BackupMetadata
            {
                Router = "office-02",
                Address = "10.0.0.1",
                RouterOsVersion = "6.47",
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
                Status = "success_with_warning",
                Warning = "Test warning",
                BackupSize = 999,
                ExportSize = 888,
                BackupSha256 = "new-backup-hash",
                ExportSha256 = "new-export-hash"
            };

        await service.SaveAsync(
            directory,
            updatedMetadata,
            CancellationToken.None);

        var filePath =
            Path.Combine(
                directory,
                "metadata.json");

        var json =
            await File.ReadAllTextAsync(filePath);

        using var document =
            JsonDocument.Parse(json);

        Assert.Equal(
            "office-02",
            document.RootElement
                .GetProperty("router")
                .GetString());

        Assert.Equal(
            "success_with_warning",
            document.RootElement
                .GetProperty("status")
                .GetString());

        Assert.Equal(
            "Test warning",
            document.RootElement
                .GetProperty("warning")
                .GetString());

        Assert.Equal(
            999,
            document.RootElement
                .GetProperty("backup_size")
                .GetInt64());
    }

    [Fact]
    public async Task SaveAsync_ProducesIndentedJson()
    {
        var service = new MetadataService();

        var directory =
            Path.Combine(
                _rootPath,
                "office-01");

        await service.SaveAsync(
            directory,
            CreateMetadata(),
            CancellationToken.None);

        var filePath =
            Path.Combine(
                directory,
                "metadata.json");

        var json =
            await File.ReadAllTextAsync(filePath);

        Assert.Contains(
            Environment.NewLine,
            json);

        Assert.Contains(
            "  \"router\"",
            json);
    }


    [Fact]
    public async Task SaveAsync_ThrowsWhenCancellationRequested()
    {
        var service = new MetadataService();

        var directory =
            Path.Combine(
                _rootPath,
                "office-01");

        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.SaveAsync(
                directory,
                CreateMetadata(),
                cancellationTokenSource.Token));
    }





    [Fact]
    public async Task SaveAsync_WritesNullOptionalValuesAsNull()
    {
        var service = new MetadataService();

        var directory =
            Path.Combine(
                _rootPath,
                "office-01");

        var metadata =
            new BackupMetadata
            {
                Router = "office-01",
                Address = "192.168.88.1",
                RouterOsVersion = null,
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
                Status = "success",
                Warning = null,
                BackupSize = null,
                ExportSize = null,
                BackupSha256 = null,
                ExportSha256 = null
            };

        await service.SaveAsync(
            directory,
            metadata,
            CancellationToken.None);

        var filePath =
            Path.Combine(
                directory,
                "metadata.json");

        var json =
            await File.ReadAllTextAsync(filePath);

        using var document =
            JsonDocument.Parse(json);

        Assert.True(
            document.RootElement
                .GetProperty("routeros_version")
                .ValueKind == JsonValueKind.Null);

        Assert.True(
            document.RootElement
                .GetProperty("backup_size")
                .ValueKind == JsonValueKind.Null);

        Assert.True(
            document.RootElement
                .GetProperty("export_size")
                .ValueKind == JsonValueKind.Null);

        Assert.True(
            document.RootElement
                .GetProperty("backup_sha256")
                .ValueKind == JsonValueKind.Null);

        Assert.True(
            document.RootElement
                .GetProperty("export_sha256")
                .ValueKind == JsonValueKind.Null);

        Assert.False(
            document.RootElement
                .TryGetProperty(
                    "warning",
                    out _));
    }

    private static BackupMetadata CreateMetadata()
    {
        var startedAt =
            new DateTime(
                2026,
                9,
                7,
                10,
                0,
                0,
                DateTimeKind.Utc);

        var completedAt =
            new DateTime(
                2026,
                9,
                7,
                10,
                0,
                10,
                DateTimeKind.Utc);

        return new BackupMetadata
        {
            Router = "office-01",
            Address = "192.168.88.1",
            RouterOsVersion = "7.18.2",
            StartedAt = startedAt,
            CompletedAt = completedAt,
            Status = "success",
            Warning = null,
            BackupSize = 123456,
            ExportSize = 45678,
            BackupSha256 = "backup-hash",
            ExportSha256 = "export-hash"
        };
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_rootPath))
            {
                Directory.Delete(
                    _rootPath,
                    recursive: true);
            }
        }
        catch
        {
        }
    }
}
