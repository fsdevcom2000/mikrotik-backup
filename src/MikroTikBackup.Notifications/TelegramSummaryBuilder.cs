using System.Globalization;
using System.Text;
using MikroTikBackup.Core.Models;

namespace MikroTikBackup.Notifications;

public sealed class TelegramSummaryBuilder
{
    public string Build(BackupRunResult runResult)
    {
        ArgumentNullException.ThrowIfNull(runResult);

        var builder = new StringBuilder();

        builder.AppendLine("MikroTik Backup Manager");
        builder.AppendLine();
        builder.AppendLine("Backup completed");
        builder.AppendLine();
        builder.AppendLine(
            $"Successful: {runResult.SuccessCount}");
        builder.AppendLine(
            $"Warnings: {runResult.WarningCount}");
        builder.AppendLine(
            $"Failed: {runResult.FailedCount}");

        foreach (var result in runResult.Results)
        {
            builder.AppendLine();
            builder.AppendLine(result.Router);

            switch (result.Status)
            {
                case BackupStatus.Success:
                    builder.AppendLine(
                        $"OK - {FormatSize(result.BackupSize)} backup, " +
                        $"{FormatSize(result.ExportSize)} export");
                    break;

                case BackupStatus.SuccessWithWarning:
                    builder.AppendLine("WARNING");

                    if (!string.IsNullOrWhiteSpace(result.Error))
                    {
                        builder.AppendLine(result.Error);
                    }

                    break;

                case BackupStatus.Failed:
                    builder.AppendLine("FAILED");

                    if (!string.IsNullOrWhiteSpace(result.Error))
                    {
                        builder.AppendLine(result.Error);
                    }

                    break;
            }

            builder.AppendLine(
                $"Duration: {FormatDuration(result.Duration)}");
        }

        return builder.ToString().TrimEnd();
    }

    private static string FormatSize(long? bytes)
    {
        if (!bytes.HasValue)
            return "N/A";

        const double kilobyte = 1024.0;
        const double megabyte = kilobyte * 1024.0;

        if (bytes.Value >= megabyte)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0:F1} MB",
                bytes.Value / megabyte);
        }

        return string.Format(
            CultureInfo.InvariantCulture,
            "{0:F1} KB",
            bytes.Value / kilobyte);
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalMinutes >= 1)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}m {1}s",
                (int)duration.TotalMinutes,
                duration.Seconds);
        }

        return string.Format(
            CultureInfo.InvariantCulture,
            "{0:F1}s",
            duration.TotalSeconds);
    }
}