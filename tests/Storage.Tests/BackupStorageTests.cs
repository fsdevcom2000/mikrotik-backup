using MikroTikBackup.Core.Models;
using MikroTikBackup.Storage;

namespace Storage.Tests;

public sealed class BackupStorageTests : IDisposable
{
    private readonly string _rootPath;

    public BackupStorageTests()
    {
        _rootPath = Path.Combine(
            Path.GetTempPath(),
            "MikroTikBackupTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_rootPath);
    }

    [Fact]
    public void GetBackupDirectory_ReturnsExpectedPath()
    {
        var storage = new BackupStorage();

        var router = new RouterConfig
        {
            Name = "office-01"
        };

        var date = new DateTime(
            2026,
            9,
            6);

        var result =
            storage.GetBackupDirectory(
                _rootPath,
                router,
                date);

        var expected =
            Path.Combine(
                _rootPath,
                "2026-09-06",
                "office-01");

        Assert.Equal(
            expected,
            result);
    }

    [Fact]
    public void GetBackupFilePath_ReturnsExpectedPath()
    {
        var storage = new BackupStorage();

        var router = new RouterConfig
        {
            Name = "office-01"
        };

        var date = new DateTime(
            2026,
            9,
            6);

        var result =
            storage.GetBackupFilePath(
                _rootPath,
                router,
                date);

        var expected =
            Path.Combine(
                _rootPath,
                "2026-09-06",
                "office-01",
                "office-01.backup");

        Assert.Equal(
            expected,
            result);
    }

    [Fact]
    public void GetExportFilePath_ReturnsExpectedPath()
    {
        var storage = new BackupStorage();

        var router = new RouterConfig
        {
            Name = "office-01"
        };

        var date = new DateTime(
            2026,
            9,
            6);

        var result =
            storage.GetExportFilePath(
                _rootPath,
                router,
                date);

        var expected =
            Path.Combine(
                _rootPath,
                "2026-09-06",
                "office-01",
                "office-01.rsc");

        Assert.Equal(
            expected,
            result);
    }

    [Fact]
    public async Task VerifyFileAsync_SucceedsForValidFile()
    {
        var storage = new BackupStorage();

        var filePath =
            Path.Combine(
                _rootPath,
                "test.backup");

        var content =
            new byte[] { 1, 2, 3, 4, 5 };

        await File.WriteAllBytesAsync(
            filePath,
            content);

        await storage.VerifyFileAsync(
            filePath,
            content.Length,
            CancellationToken.None);
    }

    [Fact]
    public async Task VerifyFileAsync_FailsWhenFileDoesNotExist()
    {
        var storage = new BackupStorage();

        var filePath =
            Path.Combine(
                _rootPath,
                "missing.backup");

        await Assert.ThrowsAsync<FileNotFoundException>(
            () => storage.VerifyFileAsync(
                filePath,
                10,
                CancellationToken.None));
    }

    [Fact]
    public async Task VerifyFileAsync_FailsWhenFileIsEmpty()
    {
        var storage = new BackupStorage();

        var filePath =
            Path.Combine(
                _rootPath,
                "empty.backup");

        await File.WriteAllBytesAsync(
            filePath,
            []);

        await Assert.ThrowsAsync<InvalidDataException>(
            () => storage.VerifyFileAsync(
                filePath,
                0,
                CancellationToken.None));
    }

    [Fact]
    public async Task VerifyFileAsync_FailsWhenSizeDoesNotMatch()
    {
        var storage = new BackupStorage();

        var filePath =
            Path.Combine(
                _rootPath,
                "test.backup");

        await File.WriteAllBytesAsync(
            filePath,
            new byte[] { 1, 2, 3, 4, 5 });

        var exception =
            await Assert.ThrowsAsync<InvalidDataException>(
                () => storage.VerifyFileAsync(
                    filePath,
                    10,
                    CancellationToken.None));

        Assert.Contains(
            "size mismatch",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "10",
            exception.Message);

        Assert.Contains(
            "5",
            exception.Message);
    }

    [Fact]
    public async Task VerifyFileAsync_FailsWhenExpectedSizeIsNegative()
    {
        var storage = new BackupStorage();

        var filePath =
            Path.Combine(
                _rootPath,
                "test.backup");

        await File.WriteAllBytesAsync(
            filePath,
            new byte[] { 1, 2, 3 });

        await Assert.ThrowsAsync<InvalidDataException>(
            () => storage.VerifyFileAsync(
                filePath,
                -1,
                CancellationToken.None));
    }

    [Fact]
    public async Task VerifyFileAsync_ThrowsWhenCancellationRequested()
    {
        var storage = new BackupStorage();

        var filePath =
            Path.Combine(
                _rootPath,
                "test.backup");

        await File.WriteAllBytesAsync(
            filePath,
            new byte[] { 1, 2, 3 });

        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => storage.VerifyFileAsync(
                filePath,
                3,
                cancellationTokenSource.Token));
    }

    [Fact]
    public async Task VerifyFileAsync_AcceptsCorrectNonZeroSize()
    {
        var storage = new BackupStorage();

        var filePath =
            Path.Combine(
                _rootPath,
                "test.rsc");

        var content =
            new byte[] { 10, 20, 30, 40 };

        await File.WriteAllBytesAsync(
            filePath,
            content);

        await storage.VerifyFileAsync(
            filePath,
            4,
            CancellationToken.None);
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
