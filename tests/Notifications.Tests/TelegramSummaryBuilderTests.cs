using MikroTikBackup.Core.Models;
using MikroTikBackup.Notifications;

namespace Notifications.Tests;

public sealed class TelegramSummaryBuilderTests
{
    [Fact]
    public void Build_WithSuccessfulRouter_ReturnsExpectedSummary()
    {
        var runResult = new BackupRunResult();

        runResult.Results.Add(CreateResult(
            router: "nechaevka",
            status: BackupStatus.Success,
            backupSize: 95946,
            exportSize: 8989,
            duration: TimeSpan.FromSeconds(10.5)));

        var builder = new TelegramSummaryBuilder();

        var result = builder.Build(runResult);

        Assert.Contains(
            "MikroTik Backup Manager",
            result);

        Assert.Contains(
            "Backup completed",
            result);

        Assert.Contains(
            "Successful: 1",
            result);

        Assert.Contains(
            "Warnings: 0",
            result);

        Assert.Contains(
            "Failed: 0",
            result);

        Assert.Contains(
            "nechaevka",
            result);

        Assert.Contains(
            "OK - 93.7 KB backup, 8.8 KB export",
            result);

        Assert.Contains(
            "Duration: 10.5s",
            result);
    }

    [Fact]
    public void Build_WithWarningRouter_IncludesWarning()
    {
        var runResult = new BackupRunResult();

        runResult.Results.Add(CreateResult(
            router: "office-01",
            status: BackupStatus.SuccessWithWarning,
            backupSize: 183421,
            exportSize: 27482,
            duration: TimeSpan.FromSeconds(8.2),
            error: "Remote cleanup failed"));

        var builder = new TelegramSummaryBuilder();

        var result = builder.Build(runResult);

        Assert.Contains(
            "Successful: 0",
            result);

        Assert.Contains(
            "Warnings: 1",
            result);

        Assert.Contains(
            "Failed: 0",
            result);

        Assert.Contains(
            "office-01",
            result);

        Assert.Contains(
            "WARNING",
            result);

        Assert.Contains(
            "Remote cleanup failed",
            result);

        Assert.Contains(
            "Duration: 8.2s",
            result);
    }

    [Fact]
    public void Build_WithFailedRouter_IncludesFailure()
    {
        var runResult = new BackupRunResult();

        runResult.Results.Add(CreateResult(
            router: "remote-01",
            status: BackupStatus.Failed,
            duration: TimeSpan.FromSeconds(3.7),
            error: "Connection timeout"));

        var builder = new TelegramSummaryBuilder();

        var result = builder.Build(runResult);

        Assert.Contains(
            "Successful: 0",
            result);

        Assert.Contains(
            "Warnings: 0",
            result);

        Assert.Contains(
            "Failed: 1",
            result);

        Assert.Contains(
            "remote-01",
            result);

        Assert.Contains(
            "FAILED",
            result);

        Assert.Contains(
            "Connection timeout",
            result);

        Assert.Contains(
            "Duration: 3.7s",
            result);
    }

    [Fact]
    public void Build_WithMultipleRouters_ReturnsAllResults()
    {
        var runResult = new BackupRunResult();

        runResult.Results.Add(CreateResult(
            router: "nechaevka",
            status: BackupStatus.Success,
            backupSize: 95946,
            exportSize: 8989,
            duration: TimeSpan.FromSeconds(10.5)));

        runResult.Results.Add(CreateResult(
            router: "office-01",
            status: BackupStatus.SuccessWithWarning,
            backupSize: 183421,
            exportSize: 27482,
            duration: TimeSpan.FromSeconds(8.2),
            error: "Remote cleanup failed"));

        runResult.Results.Add(CreateResult(
            router: "remote-01",
            status: BackupStatus.Failed,
            duration: TimeSpan.FromSeconds(3.7),
            error: "Connection timeout"));

        var builder = new TelegramSummaryBuilder();

        var result = builder.Build(runResult);

        Assert.Contains(
            "Successful: 1",
            result);

        Assert.Contains(
            "Warnings: 1",
            result);

        Assert.Contains(
            "Failed: 1",
            result);

        Assert.Contains(
            "nechaevka",
            result);

        Assert.Contains(
            "office-01",
            result);

        Assert.Contains(
            "remote-01",
            result);

        Assert.Contains(
            "OK - 93.7 KB backup, 8.8 KB export",
            result);

        Assert.Contains(
            "WARNING",
            result);

        Assert.Contains(
            "Remote cleanup failed",
            result);

        Assert.Contains(
            "FAILED",
            result);

        Assert.Contains(
            "Connection timeout",
            result);
    }

    [Fact]
    public void Build_WithMegabyteBackup_FormatsSizeAsMegabytes()
    {
        var runResult = new BackupRunResult();

        runResult.Results.Add(CreateResult(
            router: "large-router",
            status: BackupStatus.Success,
            backupSize: 2 * 1024 * 1024,
            exportSize: 512 * 1024,
            duration: TimeSpan.FromSeconds(2)));

        var builder = new TelegramSummaryBuilder();

        var result = builder.Build(runResult);

        Assert.Contains(
            "OK - 2.0 MB backup, 512.0 KB export",
            result);
    }

    [Fact]
    public void Build_WithLongDuration_FormatsMinutesAndSeconds()
    {
        var runResult = new BackupRunResult();

        runResult.Results.Add(CreateResult(
            router: "slow-router",
            status: BackupStatus.Success,
            backupSize: 1024,
            exportSize: 1024,
            duration: TimeSpan.FromSeconds(125)));

        var builder = new TelegramSummaryBuilder();

        var result = builder.Build(runResult);

        Assert.Contains(
            "Duration: 2m 5s",
            result);
    }

    [Fact]
    public void Build_WithMissingSizes_UsesNoSizeForFailedResult()
    {
        var runResult = new BackupRunResult();

        runResult.Results.Add(CreateResult(
            router: "failed-router",
            status: BackupStatus.Failed,
            duration: TimeSpan.FromSeconds(1),
            error: "Backup failed"));

        var builder = new TelegramSummaryBuilder();

        var result = builder.Build(runResult);

        Assert.Contains(
            "FAILED",
            result);

        Assert.Contains(
            "Backup failed",
            result);

        Assert.DoesNotContain(
            "N/A backup",
            result);
    }

    [Fact]
    public void Build_WithNullResult_ThrowsArgumentNullException()
    {
        var builder = new TelegramSummaryBuilder();

        Assert.Throws<ArgumentNullException>(() =>
            builder.Build(null!));
    }

    [Fact]
    public void Build_WithEmptyRunResult_ReturnsSummaryWithZeroCounts()
    {
        var runResult = new BackupRunResult();

        var builder = new TelegramSummaryBuilder();

        var result = builder.Build(runResult);

        Assert.Equal(
            "MikroTik Backup Manager\r\n\r\n" +
            "Backup completed\r\n\r\n" +
            "Successful: 0\r\n" +
            "Warnings: 0\r\n" +
            "Failed: 0",
            result);
    }

    private static RouterBackupResult CreateResult(
        string router,
        BackupStatus status,
        TimeSpan duration,
        long? backupSize = null,
        long? exportSize = null,
        string? error = null)
    {
        var startedAt = new DateTime(
            2026,
            9,
            15,
            10,
            0,
            0,
            DateTimeKind.Utc);

        return new RouterBackupResult
        {
            Router = router,
            Status = status,
            BackupSize = backupSize,
            ExportSize = exportSize,
            Error = error,
            StartedAt = startedAt,
            CompletedAt = startedAt.Add(duration)
        };
    }
}