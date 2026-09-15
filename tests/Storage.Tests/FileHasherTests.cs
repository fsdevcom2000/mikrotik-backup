using MikroTikBackup.Storage;

namespace Storage.Tests;

public sealed class FileHasherTests : IDisposable
{
    private readonly string _rootPath;

    public FileHasherTests()
    {
        _rootPath = Path.Combine(
            Path.GetTempPath(),
            "MikroTikBackupTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_rootPath);
    }

    [Fact]
    public async Task ComputeSha256Async_ReturnsExpectedHash()
    {
        var hasher = new FileHasher();

        var filePath =
            Path.Combine(
                _rootPath,
                "test.bin");

        await File.WriteAllBytesAsync(
            filePath,
            "hello world"u8.ToArray());

        var hash =
            await hasher.ComputeSha256Async(
                filePath,
                CancellationToken.None);

        Assert.Equal(
            "b94d27b9934d3e08a52e52d7da7dabfac484efe37a5380ee9088f7ace2efcde9",
            hash);
    }

    [Fact]
    public async Task ComputeSha256Async_ReturnsLowercaseHash()
    {
        var hasher = new FileHasher();

        var filePath =
            Path.Combine(
                _rootPath,
                "test.bin");

        await File.WriteAllBytesAsync(
            filePath,
            new byte[] { 1, 2, 3, 4, 5 });

        var hash =
            await hasher.ComputeSha256Async(
                filePath,
                CancellationToken.None);

        Assert.Equal(
            hash.ToLowerInvariant(),
            hash);
    }

    [Fact]
    public async Task ComputeSha256Async_ReturnsSameHashForSameContent()
    {
        var hasher = new FileHasher();

        var firstPath =
            Path.Combine(
                _rootPath,
                "first.bin");

        var secondPath =
            Path.Combine(
                _rootPath,
                "second.bin");

        var content =
            new byte[]
            {
                10, 20, 30, 40, 50,
                60, 70, 80, 90, 100
            };

        await File.WriteAllBytesAsync(
            firstPath,
            content);

        await File.WriteAllBytesAsync(
            secondPath,
            content);

        var firstHash =
            await hasher.ComputeSha256Async(
                firstPath,
                CancellationToken.None);

        var secondHash =
            await hasher.ComputeSha256Async(
                secondPath,
                CancellationToken.None);

        Assert.Equal(
            firstHash,
            secondHash);
    }

    [Fact]
    public async Task ComputeSha256Async_ReturnsDifferentHashForDifferentContent()
    {
        var hasher = new FileHasher();

        var firstPath =
            Path.Combine(
                _rootPath,
                "first.bin");

        var secondPath =
            Path.Combine(
                _rootPath,
                "second.bin");

        await File.WriteAllBytesAsync(
            firstPath,
            new byte[] { 1, 2, 3 });

        await File.WriteAllBytesAsync(
            secondPath,
            new byte[] { 1, 2, 4 });

        var firstHash =
            await hasher.ComputeSha256Async(
                firstPath,
                CancellationToken.None);

        var secondHash =
            await hasher.ComputeSha256Async(
                secondPath,
                CancellationToken.None);

        Assert.NotEqual(
            firstHash,
            secondHash);
    }

    [Fact]
    public async Task ComputeSha256Async_ReturnsHashForEmptyFile()
    {
        var hasher = new FileHasher();

        var filePath =
            Path.Combine(
                _rootPath,
                "empty.bin");

        await File.WriteAllBytesAsync(
            filePath,
            []);

        var hash =
            await hasher.ComputeSha256Async(
                filePath,
                CancellationToken.None);

        Assert.Equal(
            "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            hash);
    }

    [Fact]
    public async Task ComputeSha256Async_ThrowsWhenFileDoesNotExist()
    {
        var hasher = new FileHasher();

        var filePath =
            Path.Combine(
                _rootPath,
                "missing.bin");

        await Assert.ThrowsAsync<FileNotFoundException>(
            () => hasher.ComputeSha256Async(
                filePath,
                CancellationToken.None));
    }

    [Fact]
    public async Task ComputeSha256Async_ThrowsWhenCancellationRequested()
    {
        var hasher = new FileHasher();

        var filePath =
            Path.Combine(
                _rootPath,
                "test.bin");

        await File.WriteAllBytesAsync(
            filePath,
            new byte[] { 1, 2, 3 });

        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => hasher.ComputeSha256Async(
                filePath,
                cancellationTokenSource.Token));
    }

    [Fact]
    public async Task ComputeSha256Async_ReturnsCorrectHashForBinaryData()
    {
        var hasher = new FileHasher();

        var filePath =
            Path.Combine(
                _rootPath,
                "binary.bin");

        var content =
            new byte[]
            {
                0x00,
                0x01,
                0x02,
                0x7F,
                0x80,
                0xFE,
                0xFF
            };

        await File.WriteAllBytesAsync(
            filePath,
            content);

        var hash =
            await hasher.ComputeSha256Async(
                filePath,
                CancellationToken.None);

        Assert.Equal(
            "7bb6463b30f9e301fed333cdf8960ca9497b602ccd8eeb46ae42693fdea15a4d",
            hash);
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
