using System.Text.Json;
using MikroTikBackup.Core.Models;
using MikroTikBackup.Storage;

namespace Storage.Tests;

public sealed class StatusServiceTests : IDisposable
{
    private readonly string _rootPath;

    public StatusServiceTests()
    {
        _rootPath =
            Path.Combine(
                Path.GetTempPath(),
                "MikroTikBackupTests",
                Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_rootPath);
    }

    [Fact]
    public async Task GetStatusAsync_WhenStorageDoesNotExist_ReturnsNoBackup()
    {
        var rootPath =
            Path.Combine(
                _rootPath,
                "missing");

        var router =
            CreateRouter();

        var service =
            new StatusService();

        var result =
            await service.GetStatusAsync(
                [router],
                rootPath,
                CancellationToken.None);

        var status = result.Single();

        Assert.Equal(router.Name, status.Router);
        Assert.Equal(router.Address, status.Address);
        Assert.Null(status.Status);
        Assert.Null(status.CompletedAt);
    }

    [Fact]
    public async Task GetStatusAsync_WhenMetadataDoesNotExist_ReturnsNoBackup()
    {
        var router =
            CreateRouter();

        Directory.CreateDirectory(
            Path.Combine(
                _rootPath,
                "2026-09-15",
                router.Name));

        var service =
            new StatusService();

        var result =
            await service.GetStatusAsync(
                [router],
                _rootPath,
                CancellationToken.None);

        Assert.Null(result.Single().Status);
    }

    [Fact]
    public async Task GetStatusAsync_ReadsLatestMetadata()
    {
        var router =
            CreateRouter();

        await WriteMetadataAsync(
            "2026-09-14",
            router.Name,
            new BackupMetadata
            {
                Router = router.Name,
                Address = router.Address,
                RouterOsVersion = "6.47 (stable)",
                StartedAt = DateTime.UtcNow.AddDays(-1),
                CompletedAt = DateTime.UtcNow.AddDays(-1),
                Status = "success",
                BackupSize = 100,
                ExportSize = 50
            });

        var completedAt =
            DateTime.UtcNow;

        await WriteMetadataAsync(
            "2026-09-15",
            router.Name,
            new BackupMetadata
            {
                Router = router.Name,
                Address = router.Address,
                RouterOsVersion = "7.18.2",
                StartedAt = completedAt.AddSeconds(-10),
                CompletedAt = completedAt,
                Status = "success",
                BackupSize = 200,
                ExportSize = 75
            });

        var service =
            new StatusService();

        var result =
            await service.GetStatusAsync(
                [router],
                _rootPath,
                CancellationToken.None);

        var status = result.Single();

        Assert.Equal(BackupStatus.Success, status.Status);
        Assert.Equal("7.18.2", status.RouterOsVersion);
        Assert.Equal(completedAt, status.CompletedAt);
        Assert.Equal(200, status.BackupSize);
        Assert.Equal(75, status.ExportSize);
        Assert.Null(status.Warning);
        Assert.Null(status.Error);
    }

    [Fact]
    public async Task GetStatusAsync_IgnoresNonDateDirectories()
    {
        var router =
            CreateRouter();

        var invalidDirectory =
            Path.Combine(
                _rootPath,
                "latest",
                router.Name);

        Directory.CreateDirectory(invalidDirectory);

        await File.WriteAllTextAsync(
            Path.Combine(
                invalidDirectory,
                "metadata.json"),
            "{}");

        var service =
            new StatusService();

        var result =
            await service.GetStatusAsync(
                [router],
                _rootPath,
                CancellationToken.None);

        Assert.Null(result.Single().Status);
    }

    [Fact]
    public async Task GetStatusAsync_ReturnsWarningStatus()
    {
        var router =
            CreateRouter();

        await WriteMetadataAsync(
            "2026-09-15",
            router.Name,
            new BackupMetadata
            {
                Router = router.Name,
                Address = router.Address,
                RouterOsVersion = "6.47 (stable)",
                StartedAt = DateTime.UtcNow.AddMinutes(-1),
                CompletedAt = DateTime.UtcNow,
                Status = "success_with_warning",
                Warning = "Remote cleanup failed.",
                BackupSize = 100,
                ExportSize = 50
            });

        var service =
            new StatusService();

        var result =
            await service.GetStatusAsync(
                [router],
                _rootPath,
                CancellationToken.None);

        var status = result.Single();

        Assert.Equal(
            BackupStatus.SuccessWithWarning,
            status.Status);

        Assert.Equal(
            "Remote cleanup failed.",
            status.Warning);
    }

    [Fact]
    public async Task GetStatusAsync_ReturnsFailedForUnknownStatus()
    {
        var router =
            CreateRouter();

        await WriteMetadataAsync(
            "2026-09-15",
            router.Name,
            new BackupMetadata
            {
                Router = router.Name,
                Address = router.Address,
                RouterOsVersion = "6.47 (stable)",
                StartedAt = DateTime.UtcNow.AddMinutes(-1),
                CompletedAt = DateTime.UtcNow,
                Status = "something_new"
            });

        var service =
            new StatusService();

        var result =
            await service.GetStatusAsync(
                [router],
                _rootPath,
                CancellationToken.None);

        var status = result.Single();

        Assert.Equal(
            BackupStatus.Failed,
            status.Status);

        Assert.Contains(
            "Unknown backup status",
            status.Error);
    }

    [Fact]
    public async Task GetStatusAsync_ReturnsFailedForInvalidJson()
    {
        var router =
            CreateRouter();

        var directory =
            Path.Combine(
                _rootPath,
                "2026-09-15",
                router.Name);

        Directory.CreateDirectory(directory);

        await File.WriteAllTextAsync(
            Path.Combine(
                directory,
                "metadata.json"),
            "{ invalid json");

        var service =
            new StatusService();

        var result =
            await service.GetStatusAsync(
                [router],
                _rootPath,
                CancellationToken.None);

        var status = result.Single();

        Assert.Equal(
            BackupStatus.Failed,
            status.Status);

        Assert.Contains(
            "Invalid metadata.json",
            status.Error);
    }

    [Fact]
    public async Task GetStatusAsync_HandlesMultipleRouters()
    {
        var router1 =
            CreateRouter(
                "router-01",
                "192.168.88.1");

        var router2 =
            CreateRouter(
                "router-02",
                "192.168.88.2");

        await WriteMetadataAsync(
            "2026-09-15",
            router1.Name,
            new BackupMetadata
            {
                Router = router1.Name,
                Address = router1.Address,
                RouterOsVersion = "7.18.2",
                StartedAt = DateTime.UtcNow.AddMinutes(-1),
                CompletedAt = DateTime.UtcNow,
                Status = "success"
            });

        var service =
            new StatusService();

        var result =
            await service.GetStatusAsync(
                [router1, router2],
                _rootPath,
                CancellationToken.None);

        Assert.Equal(2, result.Count);

        Assert.Equal(
            BackupStatus.Success,
            result[0].Status);

        Assert.Null(
            result[1].Status);
    }

    [Fact]
    public async Task GetStatusAsync_ThrowsWhenRootPathIsEmpty()
    {
        var router =
            CreateRouter();

        var service =
            new StatusService();

        await Assert.ThrowsAsync<ArgumentException>(
            () =>
                service.GetStatusAsync(
                    [router],
                    string.Empty,
                    CancellationToken.None));
    }

    [Fact]
    public async Task GetStatusAsync_ThrowsWhenCancelled()
    {
        var router =
            CreateRouter();

        var service =
            new StatusService();

        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () =>
                service.GetStatusAsync(
                    [router],
                    _rootPath,
                    cancellationTokenSource.Token));
    }

    private async Task WriteMetadataAsync(
        string date,
        string routerName,
        BackupMetadata metadata)
    {
        var directory =
            Path.Combine(
                _rootPath,
                date,
                routerName);

        Directory.CreateDirectory(directory);

        var path =
            Path.Combine(
                directory,
                "metadata.json");

        await using var stream =
            new FileStream(
                path,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 16 * 1024,
                useAsync: true);

        await JsonSerializer.SerializeAsync(
            stream,
            metadata,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });
    }

    private static RouterConfig CreateRouter(
        string name = "nechaevka",
        string address = "192.168.88.1")
    {
        return new RouterConfig
        {
            Name = name,
            Address = address,
            Protocol = "api",
            Port = 8728,
            TransferPort = 22,
            Credential = name
        };
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(
                _rootPath,
                recursive: true);
        }
    }
}