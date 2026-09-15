using MikroTikBackup.Storage;

namespace Storage.Tests;

public sealed class FileLoggerTests
{
    [Fact]
    public void Info_WritesMessageToDailyLog()
    {
        var root =
            Path.Combine(
                Path.GetTempPath(),
                $"MikroTikBackupLogger-{Guid.NewGuid():N}");

        try
        {
            using (var logger = new FileLogger(root))
            {
                logger.Info("Test message");
            }

            var file =
                Path.Combine(
                    root,
                    $"{DateTime.Now:yyyy-MM-dd}.log");

            Assert.True(
                File.Exists(file));

            var content =
                File.ReadAllText(file);

            Assert.Contains(
                "[INFO] Test message",
                content);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(
                    root,
                    recursive: true);
            }
        }
    }

    [Fact]
    public void Warning_WritesWarningLevel()
    {
        var root =
            Path.Combine(
                Path.GetTempPath(),
                $"MikroTikBackupLogger-{Guid.NewGuid():N}");

        try
        {
            using (var logger = new FileLogger(root))
            {
                logger.Warning("Warning message");
            }

            var file =
                Path.Combine(
                    root,
                    $"{DateTime.Now:yyyy-MM-dd}.log");

            var content =
                File.ReadAllText(file);

            Assert.Contains(
                "[WARN] Warning message",
                content);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(
                    root,
                    recursive: true);
            }
        }
    }

    [Fact]
    public void Error_WithException_WritesExceptionMessage()
    {
        var root =
            Path.Combine(
                Path.GetTempPath(),
                $"MikroTikBackupLogger-{Guid.NewGuid():N}");

        try
        {
            using (var logger = new FileLogger(root))
            {
                logger.Error(
                    "Operation failed",
                    new InvalidOperationException(
                        "Test exception"));
            }

            var file =
                Path.Combine(
                    root,
                    $"{DateTime.Now:yyyy-MM-dd}.log");

            var content =
                File.ReadAllText(file);

            Assert.Contains(
                "[ERROR] Operation failed: Test exception",
                content);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(
                    root,
                    recursive: true);
            }
        }
    }

    [Fact]
    public void MultipleMessages_AreAppendedToSameFile()
    {
        var root =
            Path.Combine(
                Path.GetTempPath(),
                $"MikroTikBackupLogger-{Guid.NewGuid():N}");

        try
        {
            using (var logger = new FileLogger(root))
            {
                logger.Info("First");
                logger.Info("Second");
                logger.Warning("Third");
            }

            var file =
                Path.Combine(
                    root,
                    $"{DateTime.Now:yyyy-MM-dd}.log");

            var lines =
                File.ReadAllLines(file);

            Assert.Equal(
                3,
                lines.Length);

            Assert.Contains(
                "[INFO] First",
                lines[0]);

            Assert.Contains(
                "[INFO] Second",
                lines[1]);

            Assert.Contains(
                "[WARN] Third",
                lines[2]);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(
                    root,
                    recursive: true);
            }
        }
    }
}