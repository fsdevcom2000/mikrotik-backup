using MikroTikBackup.Storage;

namespace Core.Tests;

public sealed class RetentionServiceTests : IDisposable
{
    private readonly string _rootPath;

    public RetentionServiceTests()
    {
        _rootPath =
            Path.Combine(
                Path.GetTempPath(),
                "MikroTikBackupRetentionTests",
                Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(
            _rootPath);
    }

    [Fact]
    public async Task ExecuteAsync_DeletesDirectoriesOlderThanRetention()
    {
        var oldDate =
            DateTime.Today.AddDays(-31);

        CreateBackupDirectory(
            oldDate,
            "router-01");

        var service =
            new RetentionService();

        await service.ExecuteAsync(
            _rootPath,
            30,
            CancellationToken.None);

        Assert.False(
            Directory.Exists(
                Path.Combine(
                    _rootPath,
                    oldDate.ToString("yyyy-MM-dd"))));
    }

    [Fact]
    public async Task ExecuteAsync_KeepsDirectoryAtRetentionBoundary()
    {
        var boundaryDate =
            DateTime.Today.AddDays(-30);

        CreateBackupDirectory(
            boundaryDate,
            "router-01");

        var service =
            new RetentionService();

        await service.ExecuteAsync(
            _rootPath,
            30,
            CancellationToken.None);

        Assert.True(
            Directory.Exists(
                Path.Combine(
                    _rootPath,
                    boundaryDate.ToString("yyyy-MM-dd"))));
    }

    [Fact]
    public async Task ExecuteAsync_KeepsRecentDirectories()
    {
        var recentDate =
            DateTime.Today.AddDays(-5);

        CreateBackupDirectory(
            recentDate,
            "router-01");

        var service =
            new RetentionService();

        await service.ExecuteAsync(
            _rootPath,
            30,
            CancellationToken.None);

        Assert.True(
            Directory.Exists(
                Path.Combine(
                    _rootPath,
                    recentDate.ToString("yyyy-MM-dd"))));
    }

    [Fact]
    public async Task ExecuteAsync_DeletesEntireOldDirectoryRecursively()
    {
        var oldDate =
            DateTime.Today.AddDays(-31);

        var directory =
            CreateBackupDirectory(
                oldDate,
                "router-01");

        var nestedDirectory =
            Path.Combine(
                directory,
                "nested");

        Directory.CreateDirectory(
            nestedDirectory);

        await File.WriteAllTextAsync(
            Path.Combine(
                directory,
                "backup.backup"),
            "backup");

        await File.WriteAllTextAsync(
            Path.Combine(
                nestedDirectory,
                "test.txt"),
            "test");

        var service =
            new RetentionService();

        await service.ExecuteAsync(
            _rootPath,
            30,
            CancellationToken.None);

        Assert.False(
            Directory.Exists(
                directory));
    }

    [Fact]
    public async Task ExecuteAsync_KeepsDirectoriesWithInvalidDateNames()
    {
        var directory =
            Path.Combine(
                _rootPath,
                "not-a-date");

        Directory.CreateDirectory(
            directory);

        await File.WriteAllTextAsync(
            Path.Combine(
                directory,
                "backup.backup"),
            "backup");

        var service =
            new RetentionService();

        await service.ExecuteAsync(
            _rootPath,
            30,
            CancellationToken.None);

        Assert.True(
            Directory.Exists(
                directory));
    }

    [Fact]
    public async Task ExecuteAsync_KeepsFilesInRootDirectory()
    {
        var filePath =
            Path.Combine(
                _rootPath,
                "some-file.txt");

        await File.WriteAllTextAsync(
            filePath,
            "test");

        var oldDate =
            DateTime.Today.AddDays(-31);

        CreateBackupDirectory(
            oldDate,
            "router-01");

        var service =
            new RetentionService();

        await service.ExecuteAsync(
            _rootPath,
            30,
            CancellationToken.None);

        Assert.True(
            File.Exists(
                filePath));
    }

    [Fact]
    public async Task ExecuteAsync_HandlesMissingRootDirectory()
    {
        var missingPath =
            Path.Combine(
                Path.GetTempPath(),
                "MikroTikBackupRetentionTests",
                Guid.NewGuid().ToString("N"));

        var service =
            new RetentionService();

        await service.ExecuteAsync(
            missingPath,
            30,
            CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteAsync_RejectsEmptyRootPath()
    {
        var service =
            new RetentionService();

        await Assert.ThrowsAsync<ArgumentException>(
            () =>
                service.ExecuteAsync(
                    string.Empty,
                    30,
                    CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_RejectsZeroRetentionDays()
    {
        var service =
            new RetentionService();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () =>
                service.ExecuteAsync(
                    _rootPath,
                    0,
                    CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_RejectsNegativeRetentionDays()
    {
        var service =
            new RetentionService();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () =>
                service.ExecuteAsync(
                    _rootPath,
                    -1,
                    CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_DeletesMultipleOldDirectories()
    {
        var oldDate1 =
            DateTime.Today.AddDays(-31);

        var oldDate2 =
            DateTime.Today.AddDays(-60);

        var oldDate3 =
            DateTime.Today.AddDays(-365);

        CreateBackupDirectory(
            oldDate1,
            "router-01");

        CreateBackupDirectory(
            oldDate2,
            "router-02");

        CreateBackupDirectory(
            oldDate3,
            "router-03");

        var service =
            new RetentionService();

        await service.ExecuteAsync(
            _rootPath,
            30,
            CancellationToken.None);

        Assert.False(
            Directory.Exists(
                Path.Combine(
                    _rootPath,
                    oldDate1.ToString("yyyy-MM-dd"))));

        Assert.False(
            Directory.Exists(
                Path.Combine(
                    _rootPath,
                    oldDate2.ToString("yyyy-MM-dd"))));

        Assert.False(
            Directory.Exists(
                Path.Combine(
                    _rootPath,
                    oldDate3.ToString("yyyy-MM-dd"))));
    }

    [Fact]
    public async Task ExecuteAsync_DeletesOldAndKeepsRecentDirectories()
    {
        var oldDate =
            DateTime.Today.AddDays(-31);

        var recentDate =
            DateTime.Today.AddDays(-10);

        CreateBackupDirectory(
            oldDate,
            "router-old");

        CreateBackupDirectory(
            recentDate,
            "router-new");

        var service =
            new RetentionService();

        await service.ExecuteAsync(
            _rootPath,
            30,
            CancellationToken.None);

        Assert.False(
            Directory.Exists(
                Path.Combine(
                    _rootPath,
                    oldDate.ToString("yyyy-MM-dd"))));

        Assert.True(
            Directory.Exists(
                Path.Combine(
                    _rootPath,
                    recentDate.ToString("yyyy-MM-dd"))));
    }

    [Fact]
    public async Task ExecuteAsync_StopsWhenCancellationIsRequested()
    {
        var oldDate =
            DateTime.Today.AddDays(-31);

        CreateBackupDirectory(
            oldDate,
            "router-01");

        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        var service =
            new RetentionService();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () =>
                service.ExecuteAsync(
                    _rootPath,
                    30,
                    cancellationTokenSource.Token));

        Assert.True(
            Directory.Exists(
                Path.Combine(
                    _rootPath,
                    oldDate.ToString("yyyy-MM-dd"))));
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotDeleteFutureDateDirectories()
    {
        var futureDate =
            DateTime.Today.AddDays(10);

        CreateBackupDirectory(
            futureDate,
            "router-01");

        var service =
            new RetentionService();

        await service.ExecuteAsync(
            _rootPath,
            30,
            CancellationToken.None);

        Assert.True(
            Directory.Exists(
                Path.Combine(
                    _rootPath,
                    futureDate.ToString("yyyy-MM-dd"))));
    }

    private string CreateBackupDirectory(
        DateTime date,
        string routerName)
    {
        var directory =
            Path.Combine(
                _rootPath,
                date.ToString("yyyy-MM-dd"),
                routerName);

        Directory.CreateDirectory(
            directory);

        return directory;
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