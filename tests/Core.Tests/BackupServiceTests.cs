using System.IO;
using MikroTikBackup.Core.Interfaces;
using MikroTikBackup.Core.Models;
using MikroTikBackup.Core.Services;

namespace Core.Tests;

public sealed class BackupServiceTests
{
    [Fact]
    public async Task BackupRouterAsync_SuccessfullyBacksUpRouter()
    {
        var routerClient =
            new FakeRouterOsClient();

        var fileTransfer =
            new FakeFileTransfer();

        var backupService =
            CreateBackupService(
                routerClient,
                fileTransfer);

        var result =
            await backupService.BackupRouterAsync(
                CreateRouter(),
                CreateCredential(),
                CreateConfig(),
                CancellationToken.None);

        Assert.Equal(
            BackupStatus.Success,
            result.Status);

        Assert.Null(result.Error);
        Assert.Equal("7.18.2", result.RouterOsVersion);

        Assert.Equal(
            12345,
            result.BackupSize);

        Assert.Equal(
            6789,
            result.ExportSize);

        Assert.Equal(
            "test-hash",
            result.BackupSha256);

        Assert.Equal(
            "test-hash",
            result.ExportSha256);

        Assert.Equal(
            2,
            fileTransfer.DownloadAttempts);

        Assert.Equal(
            2,
            routerClient.DeletedRemoteFiles.Count);
    }

    [Fact]
    public async Task BackupRouterAsync_ReturnsBackupSize()
    {
        var routerClient =
            new FakeRouterOsClient();

        var backupService =
            CreateBackupService(
                routerClient,
                new FakeFileTransfer());

        var result =
            await backupService.BackupRouterAsync(
                CreateRouter(),
                CreateCredential(),
                CreateConfig(),
                CancellationToken.None);

        Assert.Equal(
            12345,
            result.BackupSize);
    }

    [Fact]
    public async Task BackupRouterAsync_ReturnsExportSize()
    {
        var routerClient =
            new FakeRouterOsClient();

        var backupService =
            CreateBackupService(
                routerClient,
                new FakeFileTransfer());

        var result =
            await backupService.BackupRouterAsync(
                CreateRouter(),
                CreateCredential(),
                CreateConfig(),
                CancellationToken.None);

        Assert.Equal(
            6789,
            result.ExportSize);
    }

    [Fact]
    public async Task BackupRouterAsync_ReturnsBackupSha256()
    {
        var routerClient =
            new FakeRouterOsClient();

        var backupService =
            CreateBackupService(
                routerClient,
                new FakeFileTransfer());

        var result =
            await backupService.BackupRouterAsync(
                CreateRouter(),
                CreateCredential(),
                CreateConfig(),
                CancellationToken.None);

        Assert.Equal(
            "test-hash",
            result.BackupSha256);
    }

    [Fact]
    public async Task BackupRouterAsync_ReturnsExportSha256()
    {
        var routerClient =
            new FakeRouterOsClient();

        var backupService =
            CreateBackupService(
                routerClient,
                new FakeFileTransfer());

        var result =
            await backupService.BackupRouterAsync(
                CreateRouter(),
                CreateCredential(),
                CreateConfig(),
                CancellationToken.None);

        Assert.Equal(
            "test-hash",
            result.ExportSha256);
    }

    [Fact]
    public async Task BackupRouterAsync_ConnectsUsingConfiguredTimeout()
    {
        var routerClient =
            new FakeRouterOsClient();

        var backupService =
            CreateBackupService(
                routerClient,
                new FakeFileTransfer());

        var config =
            CreateConfig();

        config.Timeouts.ConnectionSeconds = 17;

        await backupService.BackupRouterAsync(
            CreateRouter(),
            CreateCredential(),
            config,
            CancellationToken.None);

        Assert.Equal(
            TimeSpan.FromSeconds(17),
            routerClient.ConnectionTimeout);
    }

    [Fact]
    public async Task BackupRouterAsync_RetriesTransientConnectionFailure()
    {
        var routerClient =
            new FakeRouterOsClient
            {
                ConnectFailuresBeforeSuccess = 1
            };

        var backupService =
            CreateBackupService(
                routerClient,
                new FakeFileTransfer());

        var result =
            await backupService.BackupRouterAsync(
                CreateRouter(),
                CreateCredential(),
                CreateConfig(),
                CancellationToken.None);

        Assert.Equal(
            BackupStatus.Success,
            result.Status);

        Assert.Equal(
            2,
            routerClient.ConnectAttempts);
    }

    [Fact]
    public async Task BackupRouterAsync_FailsAfterConnectionRetryLimit()
    {
        var routerClient =
            new FakeRouterOsClient
            {
                ConnectFailuresBeforeSuccess = 10
            };

        var backupService =
            CreateBackupService(
                routerClient,
                new FakeFileTransfer());

        var result =
            await backupService.BackupRouterAsync(
                CreateRouter(),
                CreateCredential(),
                CreateConfig(),
                CancellationToken.None);

        Assert.Equal(
            BackupStatus.Failed,
            result.Status);

        Assert.Equal(
            3,
            routerClient.ConnectAttempts);

        Assert.Empty(
            routerClient.DeletedRemoteFiles);
    }

    [Fact]
    public async Task BackupRouterAsync_RetriesTransientSftpFailure()
    {
        var routerClient =
            new FakeRouterOsClient();

        var fileTransfer =
            new FakeFileTransfer
            {
                FailuresBeforeSuccess = 1
            };

        var backupService =
            CreateBackupService(
                routerClient,
                fileTransfer);

        var result =
            await backupService.BackupRouterAsync(
                CreateRouter(),
                CreateCredential(),
                CreateConfig(),
                CancellationToken.None);

        Assert.Equal(
            BackupStatus.Success,
            result.Status);

        Assert.Equal(
            3,
            fileTransfer.DownloadAttempts);

        Assert.Equal(
            2,
            routerClient.DeletedRemoteFiles.Count);
    }

    [Fact]
    public async Task BackupRouterAsync_DoesNotRetryNonTransientSftpFailure()
    {
        var routerClient =
            new FakeRouterOsClient();

        var fileTransfer =
            new FakeFileTransfer
            {
                NonTransientFailure = true
            };

        var backupService =
            CreateBackupService(
                routerClient,
                fileTransfer);

        var result =
            await backupService.BackupRouterAsync(
                CreateRouter(),
                CreateCredential(),
                CreateConfig(),
                CancellationToken.None);

        Assert.Equal(
            BackupStatus.Failed,
            result.Status);

        Assert.Equal(
            1,
            fileTransfer.DownloadAttempts);

        Assert.Empty(
            routerClient.DeletedRemoteFiles);
    }

    [Fact]
    public async Task BackupRouterAsync_DeletesRemoteFilesAfterSuccessfulBackup()
    {
        var routerClient =
            new FakeRouterOsClient();

        var backupService =
            CreateBackupService(
                routerClient,
                new FakeFileTransfer());

        var result =
            await backupService.BackupRouterAsync(
                CreateRouter(),
                CreateCredential(),
                CreateConfig(),
                CancellationToken.None);

        Assert.Equal(
            BackupStatus.Success,
            result.Status);

        Assert.Equal(
            2,
            routerClient.DeletedRemoteFiles.Count);

        Assert.Contains(
            routerClient.DeletedRemoteFiles,
            x => x.EndsWith(".backup"));

        Assert.Contains(
            routerClient.DeletedRemoteFiles,
            x => x.EndsWith(".rsc"));
    }

    [Fact]
    public async Task BackupRouterAsync_DoesNotDeleteRemoteFilesWhenSftpDownloadFails()
    {
        var routerClient =
            new FakeRouterOsClient();

        var fileTransfer =
            new FakeFileTransfer
            {
                NonTransientFailure = true
            };

        var backupService =
            CreateBackupService(
                routerClient,
                fileTransfer);

        var result =
            await backupService.BackupRouterAsync(
                CreateRouter(),
                CreateCredential(),
                CreateConfig(),
                CancellationToken.None);

        Assert.Equal(
            BackupStatus.Failed,
            result.Status);

        Assert.Empty(
            routerClient.DeletedRemoteFiles);
    }

    [Fact]
    public async Task BackupRouterAsync_DoesNotDeleteRemoteFilesWhenVerificationFails()
    {
        var routerClient =
            new FakeRouterOsClient();

        var storage =
            new FakeBackupStorage
            {
                VerificationException =
                    new InvalidDataException(
                        "Simulated verification failure.")
            };

        var backupService =
            CreateBackupService(
                routerClient,
                new FakeFileTransfer(),
                storage);

        var result =
            await backupService.BackupRouterAsync(
                CreateRouter(),
                CreateCredential(),
                CreateConfig(),
                CancellationToken.None);

        Assert.Equal(
            BackupStatus.Failed,
            result.Status);

        Assert.Empty(
            routerClient.DeletedRemoteFiles);
    }

    [Fact]
    public async Task BackupRouterAsync_ReturnsWarningWhenRemoteCleanupFails()
    {
        var routerClient =
            new FakeRouterOsClient
            {
                DeleteException =
                    new IOException(
                        "Simulated remote cleanup failure.")
            };

        var backupService =
            CreateBackupService(
                routerClient,
                new FakeFileTransfer());

        var result =
            await backupService.BackupRouterAsync(
                CreateRouter(),
                CreateCredential(),
                CreateConfig(),
                CancellationToken.None);

        Assert.Equal(
            BackupStatus.SuccessWithWarning,
            result.Status);

        Assert.False(
            string.IsNullOrWhiteSpace(
                result.Error));
    }

    [Fact]
    public async Task BackupRouterAsync_SavesMetadataAfterLocalVerification()
    {
        var routerClient =
            new FakeRouterOsClient();

        var metadataService =
            new FakeMetadataService();

        var backupService =
            CreateBackupService(
                routerClient,
                new FakeFileTransfer(),
                metadataService: metadataService);

        var result =
            await backupService.BackupRouterAsync(
                CreateRouter(),
                CreateCredential(),
                CreateConfig(),
                CancellationToken.None);

        Assert.Equal(
            BackupStatus.Success,
            result.Status);

        Assert.Equal(
            1,
            metadataService.SaveAttempts);

        Assert.Single(
            metadataService.SavedMetadata);

        Assert.Equal(
            "success",
            metadataService.SavedMetadata[0].Status);
    }

    [Fact]
    public async Task BackupRouterAsync_DoesNotDeleteRemoteFilesWhenMetadataSaveFails()
    {
        var routerClient =
            new FakeRouterOsClient();

        var metadataService =
            new FakeMetadataService
            {
                SaveException =
                    new IOException(
                        "Simulated metadata storage failure.")
            };

        var backupService =
            CreateBackupService(
                routerClient,
                new FakeFileTransfer(),
                metadataService: metadataService);

        var result =
            await backupService.BackupRouterAsync(
                CreateRouter(),
                CreateCredential(),
                CreateConfig(),
                CancellationToken.None);

        Assert.Equal(
            BackupStatus.Failed,
            result.Status);

        Assert.Empty(
            routerClient.DeletedRemoteFiles);

        Assert.Equal(
            1,
            metadataService.SaveAttempts);
    }

    [Fact]
    public async Task BackupRouterAsync_UpdatesMetadataWhenCleanupFails()
    {
        var routerClient =
            new FakeRouterOsClient
            {
                DeleteException =
                    new IOException(
                        "Simulated remote cleanup failure.")
            };

        var metadataService =
            new FakeMetadataService();

        var backupService =
            CreateBackupService(
                routerClient,
                new FakeFileTransfer(),
                metadataService: metadataService);

        var result =
            await backupService.BackupRouterAsync(
                CreateRouter(),
                CreateCredential(),
                CreateConfig(),
                CancellationToken.None);

        Assert.Equal(
            BackupStatus.SuccessWithWarning,
            result.Status);

        Assert.Equal(
            2,
            metadataService.SaveAttempts);

        Assert.Equal(
            "success",
            metadataService.SavedMetadata[0].Status);

        Assert.Equal(
            "success_with_warning",
            metadataService.SavedMetadata[1].Status);

        Assert.False(
            string.IsNullOrWhiteSpace(
                metadataService.SavedMetadata[1].Warning));
    }

    [Fact]
    public async Task BackupRouterAsync_SavesRouterNameToMetadata()
    {
        var routerClient =
            new FakeRouterOsClient();

        var metadataService =
            new FakeMetadataService();

        var backupService =
            CreateBackupService(
                routerClient,
                new FakeFileTransfer(),
                metadataService: metadataService);

        await backupService.BackupRouterAsync(
            CreateRouter(),
            CreateCredential(),
            CreateConfig(),
            CancellationToken.None);

        var metadata =
            Assert.Single(
                metadataService.SavedMetadata);

        Assert.Equal(
            "test-router",
            metadata.Router);
    }

    [Fact]
    public async Task BackupRouterAsync_SavesRouterAddressToMetadata()
    {
        var routerClient =
            new FakeRouterOsClient();

        var metadataService =
            new FakeMetadataService();

        var backupService =
            CreateBackupService(
                routerClient,
                new FakeFileTransfer(),
                metadataService: metadataService);

        await backupService.BackupRouterAsync(
            CreateRouter(),
            CreateCredential(),
            CreateConfig(),
            CancellationToken.None);

        var metadata =
            Assert.Single(
                metadataService.SavedMetadata);

        Assert.Equal(
            "192.168.88.1",
            metadata.Address);
    }

    [Fact]
    public async Task BackupRouterAsync_SavesRouterOsVersionToMetadata()
    {
        var routerClient =
            new FakeRouterOsClient();

        var metadataService =
            new FakeMetadataService();

        var backupService =
            CreateBackupService(
                routerClient,
                new FakeFileTransfer(),
                metadataService: metadataService);

        await backupService.BackupRouterAsync(
            CreateRouter(),
            CreateCredential(),
            CreateConfig(),
            CancellationToken.None);

        var metadata =
            Assert.Single(
                metadataService.SavedMetadata);

        Assert.Equal(
            "7.18.2",
            metadata.RouterOsVersion);
    }

    [Fact]
    public async Task BackupRouterAsync_SavesFileSizesToMetadata()
    {
        var routerClient =
            new FakeRouterOsClient();

        var metadataService =
            new FakeMetadataService();

        var backupService =
            CreateBackupService(
                routerClient,
                new FakeFileTransfer(),
                metadataService: metadataService);

        await backupService.BackupRouterAsync(
            CreateRouter(),
            CreateCredential(),
            CreateConfig(),
            CancellationToken.None);

        var metadata =
            Assert.Single(
                metadataService.SavedMetadata);

        Assert.Equal(
            12345,
            metadata.BackupSize);

        Assert.Equal(
            6789,
            metadata.ExportSize);
    }

    [Fact]
    public async Task BackupRouterAsync_SavesHashesToMetadata()
    {
        var routerClient =
            new FakeRouterOsClient();

        var metadataService =
            new FakeMetadataService();

        var backupService =
            CreateBackupService(
                routerClient,
                new FakeFileTransfer(),
                metadataService: metadataService);

        await backupService.BackupRouterAsync(
            CreateRouter(),
            CreateCredential(),
            CreateConfig(),
            CancellationToken.None);

        var metadata =
            Assert.Single(
                metadataService.SavedMetadata);

        Assert.Equal(
            "test-hash",
            metadata.BackupSha256);

        Assert.Equal(
            "test-hash",
            metadata.ExportSha256);
    }

    [Fact]
    public async Task BackupRouterAsync_SavesSuccessStatusWithoutWarning()
    {
        var routerClient =
            new FakeRouterOsClient();

        var metadataService =
            new FakeMetadataService();

        var backupService =
            CreateBackupService(
                routerClient,
                new FakeFileTransfer(),
                metadataService: metadataService);

        await backupService.BackupRouterAsync(
            CreateRouter(),
            CreateCredential(),
            CreateConfig(),
            CancellationToken.None);

        var metadata =
            Assert.Single(
                metadataService.SavedMetadata);

        Assert.Equal(
            "success",
            metadata.Status);

        Assert.Null(
            metadata.Warning);
    }

    [Fact]
    public async Task BackupRouterAsync_CleanupWarningContainsCleanupError()
    {
        var routerClient =
            new FakeRouterOsClient
            {
                DeleteException =
                    new IOException(
                        "Simulated remote cleanup failure.")
            };

        var metadataService =
            new FakeMetadataService();

        var backupService =
            CreateBackupService(
                routerClient,
                new FakeFileTransfer(),
                metadataService: metadataService);

        await backupService.BackupRouterAsync(
            CreateRouter(),
            CreateCredential(),
            CreateConfig(),
            CancellationToken.None);

        Assert.Equal(
            2,
            metadataService.SavedMetadata.Count);

        var warningMetadata =
            metadataService.SavedMetadata[1];

        Assert.Equal(
            "success_with_warning",
            warningMetadata.Status);

        Assert.Contains(
            "Simulated remote cleanup failure.",
            warningMetadata.Warning);
    }

    [Fact]
    public async Task BackupRouterAsync_RecordsStartedAndCompletedTimes()
    {
        var routerClient =
            new FakeRouterOsClient();

        var metadataService =
            new FakeMetadataService();

        var backupService =
            CreateBackupService(
                routerClient,
                new FakeFileTransfer(),
                metadataService: metadataService);

        var before =
            DateTime.UtcNow;

        var result =
            await backupService.BackupRouterAsync(
                CreateRouter(),
                CreateCredential(),
                CreateConfig(),
                CancellationToken.None);

        var after =
            DateTime.UtcNow;

        Assert.InRange(
            result.StartedAt,
            before,
            after);

        Assert.InRange(
            result.CompletedAt,
            result.StartedAt,
            after);

        var metadata =
            Assert.Single(
                metadataService.SavedMetadata);

        Assert.Equal(
            result.StartedAt,
            metadata.StartedAt);

        Assert.Equal(
            result.StartedAt,
            metadata.StartedAt);
    }

    [Fact]
    public async Task BackupRouterAsync_DisposesRouterOsClient()
    {
        var routerClient =
            new FakeRouterOsClient();

        var backupService =
            CreateBackupService(
                routerClient,
                new FakeFileTransfer());

        var result =
            await backupService.BackupRouterAsync(
                CreateRouter(),
                CreateCredential(),
                CreateConfig(),
                CancellationToken.None);

        Assert.Equal(
            BackupStatus.Success,
            result.Status);

        Assert.True(
            routerClient.DisposeCalled);
    }


    private static IBackupService CreateBackupService(
        FakeRouterOsClient routerClient,
        FakeFileTransfer fileTransfer,
        FakeBackupStorage? storage = null,
        FakeMetadataService? metadataService = null)
    {
        return new BackupService(
            routerClient,
            fileTransfer,
            storage ?? new FakeBackupStorage(),
            new FakeFileHasher(),
            metadataService ?? new FakeMetadataService(),
            new RetryService(),
            new TransientErrorClassifier());
    }

    private static RouterConfig CreateRouter()
    {
        return new RouterConfig
        {
            Name = "test-router",
            Address = "192.168.88.1",
            Protocol = "api",
            Port = 8728,
            TransferPort = 22,
            Credential = "test"
        };
    }

    private static Credential CreateCredential()
    {
        return new Credential(
            "admin",
            "password");
    }

    private static AppConfig CreateConfig()
    {
        return new AppConfig
        {
            Storage = new StorageConfig
            {
                Path =
                    Path.Combine(
                        Path.GetTempPath(),
                        "MikroTikBackupTests")
            },
            Backup = new BackupConfig
            {
                Binary = true,
                Export = true,
                Parallel = 1
            },
            Timeouts = new TimeoutConfig
            {
                ConnectionSeconds = 10,
                OperationSeconds = 1,
                TransferSeconds = 1
            },
            Retry = new RetryConfig
            {
                Attempts = 3,
                DelaySeconds = 0
            }
        };
    }

    private sealed class FakeRouterOsClient :
        IRouterOsClient, IAsyncDisposable
    {
        public int ConnectAttempts { get; private set; }

        public int ConnectFailuresBeforeSuccess { get; set; }

        public TimeSpan? ConnectionTimeout { get; private set; }

        public List<string> DeletedRemoteFiles { get; } = [];

        public bool DisposeCalled { get; private set; }

        public Exception? DeleteException { get; set; }

        public Task ConnectAsync(
            RouterConfig router,
            Credential credential,
            TimeSpan connectionTimeout,
            CancellationToken cancellationToken)
        {
            ConnectAttempts++;

            ConnectionTimeout =
                connectionTimeout;

            if (ConnectAttempts <=
                ConnectFailuresBeforeSuccess)
            {
                throw new IOException(
                    "Simulated connection failure.");
            }

            return Task.CompletedTask;
        }

        public Task<string> GetRouterOsVersionAsync(
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                "7.18.2");
        }

        public Task DisconnectAsync(
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task CreateBackupAsync(
            string remoteFileName,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task CreateExportAsync(
            string remoteFileName,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<long> GetRemoteFileSizeAsync(
            string remoteFileName,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                remoteFileName.EndsWith(".backup")
                    ? 12345L
                    : 6789L);
        }

        public Task<long?> TryGetRemoteFileSizeAsync(
            string remoteFileName,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<long?>(
                remoteFileName.EndsWith(".backup")
                    ? 12345L
                    : 6789L);
        }

        public Task DeleteRemoteFileAsync(
            string remoteFileName,
            CancellationToken cancellationToken)
        {
            if (DeleteException != null)
            {
                throw DeleteException;
            }

            DeletedRemoteFiles.Add(
                remoteFileName);

            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            DisposeCalled = true;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakeFileTransfer : IFileTransfer
    {
        public int DownloadAttempts { get; private set; }

        public int FailuresBeforeSuccess { get; set; }

        public bool NonTransientFailure { get; set; }

        public async Task DownloadAsync(
            RouterConfig router,
            Credential credential,
            string remoteFileName,
            string localFilePath,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            DownloadAttempts++;

            if (NonTransientFailure)
            {
                throw new UnauthorizedAccessException(
                    "Simulated non-transient SFTP failure.");
            }

            if (DownloadAttempts <=
                FailuresBeforeSuccess)
            {
                throw new IOException(
                    "Simulated transient SFTP failure.");
            }

            var directory =
                Path.GetDirectoryName(localFilePath);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(
                    directory);
            }

            await File.WriteAllBytesAsync(
                localFilePath,
                new byte[
                    remoteFileName.EndsWith(".backup")
                        ? 12345
                        : 6789],
                cancellationToken);
        }
    }

    private sealed class FakeBackupStorage : IBackupStorage
    {
        public Exception? VerificationException { get; set; }

        public string GetBackupDirectory(
            string rootPath,
            RouterConfig router,
            DateTime date)
        {
            return Path.Combine(
                rootPath,
                date.ToString("yyyy-MM-dd"),
                router.Name);
        }

        public string GetBackupFilePath(
            string rootPath,
            RouterConfig router,
            DateTime date)
        {
            return Path.Combine(
                GetBackupDirectory(
                    rootPath,
                    router,
                    date),
                $"{router.Name}.backup");
        }

        public string GetExportFilePath(
            string rootPath,
            RouterConfig router,
            DateTime date)
        {
            return Path.Combine(
                GetBackupDirectory(
                    rootPath,
                    router,
                    date),
                $"{router.Name}.rsc");
        }

        public Task VerifyFileAsync(
            string filePath,
            long expectedSize,
            CancellationToken cancellationToken)
        {
            if (VerificationException != null)
            {
                throw VerificationException;
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FakeFileHasher : IFileHasher
    {
        public Task<string> ComputeSha256Async(
            string filePath,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                "test-hash");
        }
    }

    private sealed class FakeMetadataService : IMetadataService
    {
        public Exception? SaveException { get; set; }

        public int SaveAttempts { get; private set; }

        public List<BackupMetadata> SavedMetadata { get; } = [];

        public Task SaveAsync(
            string directory,
            BackupMetadata metadata,
            CancellationToken cancellationToken)
        {
            SaveAttempts++;

            if (SaveException != null)
            {
                throw SaveException;
            }

            SavedMetadata.Add(
                metadata);

            return Task.CompletedTask;
        }
    }
}